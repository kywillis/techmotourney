using System.Linq;
using System.Net;
using AutoMapper;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration
{
    public class WagerAdminOrchestration : IWagerAdminOrchestration
    {
        private readonly IPendingActivationDAO _pendingActivationDAO;
        private readonly IPlayerDAO _playerDAO;
        private readonly IWagerAuditDAO _wagerAuditDAO;
        private readonly IWagerSettingsDAO _wagerSettingsDAO;
        private readonly IWagerDAO _wagerDAO;
        private readonly IGameOddsDAO _gameOddsDAO;
        private readonly IGameResultOrchestration _gameResultOrchestration;
        private readonly ITournamentsDAO _tournamentsDAO;
        private readonly IMapper _mapper;

        public WagerAdminOrchestration(
            IPendingActivationDAO pendingActivationDAO,
            IPlayerDAO playerDAO,
            IWagerAuditDAO wagerAuditDAO,
            IWagerSettingsDAO wagerSettingsDAO,
            IWagerDAO wagerDAO,
            IGameOddsDAO gameOddsDAO,
            IGameResultOrchestration gameResultOrchestration,
            ITournamentsDAO tournamentsDAO,
            IMapper mapper)
        {
            _pendingActivationDAO = pendingActivationDAO;
            _playerDAO = playerDAO;
            _wagerAuditDAO = wagerAuditDAO;
            _wagerSettingsDAO = wagerSettingsDAO;
            _wagerDAO = wagerDAO;
            _gameOddsDAO = gameOddsDAO;
            _gameResultOrchestration = gameResultOrchestration;
            _tournamentsDAO = tournamentsDAO;
            _mapper = mapper;
        }

        public async Task<Operation<List<PendingActivationModel>, ApiError>> GetPendingActivationsAsync(bool includeActivated = false)
        {
            var list = await _pendingActivationDAO.ListAsync(includeActivated);
            return _mapper.Map<List<PendingActivationModel>>(list).ToList();
        }

        public async Task<Operation<PendingActivationModel, ApiError>> GetPendingActivationByIdAsync(int pendingActivationId)
        {
            var pending = await _pendingActivationDAO.GetByIdAsync(pendingActivationId);
            if (pending == null)
                return new ApiError("Pending activation not found", HttpStatusCode.NotFound);
            return _mapper.Map<PendingActivationModel>(pending);
        }

        public async Task<Operation<PlayerModel, ApiError>> ActivatePendingAsync(int pendingActivationId, int adminPlayerId, string fullName, string emailAddress, int profilePic)
        {
            var pending = await _pendingActivationDAO.GetByIdAsync(pendingActivationId);
            if (pending == null)
                return new ApiError("Pending activation not found", HttpStatusCode.NotFound);
            if (pending.Status != PendingActivationStatus.Pending)
                return new ApiError("Pending activation is already activated", HttpStatusCode.BadRequest);

            var googleTaken = await _playerDAO.GetPlayerByGoogleSubjectIdAsync(pending.GoogleSubjectId);
            if (googleTaken != null)
                return new ApiError("This Google account is already linked to a player. Link that signup to the existing account instead of creating a new one.", HttpStatusCode.BadRequest);

            var player = new PlayerDAOModel
            {
                FullName = fullName,
                EmailAddress = emailAddress,
                ProfilePic = profilePic,
                GoogleSubjectId = pending.GoogleSubjectId,
                IsAdmin = false,
                Balance = 0,
                IsActive = true
            };
            var created = await _playerDAO.AddPlayerAsync(player);
            await _playerDAO.SetPlayerGoogleSubjectIdAsync(created.PlayerId, pending.GoogleSubjectId);

            pending.Status = PendingActivationStatus.Activated;
            pending.ActivatedAt = DateTime.UtcNow;
            pending.ActivatedByPlayerId = adminPlayerId;
            pending.FullName = fullName;
            pending.Email = emailAddress;
            pending.RequestedProfilePic = profilePic;
            await _pendingActivationDAO.UpdateAsync(pending);

            return _mapper.Map<PlayerModel>(created);
        }

        public async Task<Operation<List<AdminPlayerLinkListItemModel>, ApiError>> ListPlayersEligibleForGoogleLinkAsync()
        {
            var rows = await _playerDAO.ListPlayersEligibleForGoogleLinkAsync();
            var list = rows
                .Select(p => new AdminPlayerLinkListItemModel
                {
                    PlayerId = p.PlayerId,
                    FullName = p.FullName,
                    EmailAddress = p.EmailAddress
                })
                .ToList();
            return list;
        }

        public async Task<Operation<PlayerModel, ApiError>> LinkPendingToExistingPlayerAsync(int pendingActivationId, int adminPlayerId, int targetPlayerId)
        {
            var pending = await _pendingActivationDAO.GetByIdAsync(pendingActivationId);
            if (pending == null)
                return new ApiError("Pending activation not found", HttpStatusCode.NotFound);
            if (pending.Status != PendingActivationStatus.Pending)
                return new ApiError("Pending activation is already activated", HttpStatusCode.BadRequest);

            var existingForGoogle = await _playerDAO.GetPlayerByGoogleSubjectIdAsync(pending.GoogleSubjectId);
            if (existingForGoogle != null)
                return new ApiError("This Google account is already linked to a player.", HttpStatusCode.BadRequest);

            var target = await _playerDAO.GetPlayerAsync(targetPlayerId);
            if (target == null)
                return new ApiError("Player not found", HttpStatusCode.NotFound);
            if (!string.IsNullOrWhiteSpace(target.GoogleSubjectId))
                return new ApiError("That player already has a Google account linked. Pick a different player.", HttpStatusCode.BadRequest);

            var emailFromGoogleSignup = pending.Email ?? string.Empty;
            var linked = await _playerDAO.TryLinkGoogleAndEmailAsync(targetPlayerId, pending.GoogleSubjectId, emailFromGoogleSignup);
            if (!linked)
                return new ApiError("Could not link Google to that player (they may already be linked or removed).", HttpStatusCode.BadRequest);

            pending.Status = PendingActivationStatus.Activated;
            pending.ActivatedAt = DateTime.UtcNow;
            pending.ActivatedByPlayerId = adminPlayerId;
            pending.FullName = target.FullName;
            pending.Email = emailFromGoogleSignup;
            pending.RequestedProfilePic = target.ProfilePic;
            await _pendingActivationDAO.UpdateAsync(pending);

            var updated = await _playerDAO.GetPlayerAsync(targetPlayerId);
            return _mapper.Map<PlayerModel>(updated!);
        }

        public async Task<Operation<bool, ApiError>> UpdatePlayerBalanceAsync(int adminPlayerId, WagerBalanceRequestModel request)
        {
            var player = await _playerDAO.GetPlayerAsync(request.PlayerId);
            if (player == null)
                return new ApiError("Player not found", HttpStatusCode.NotFound);

            decimal newBalance;
            WagerAuditAction action;
            if (request.Action == WagerBalanceAction.SetToZero)
            {
                newBalance = 0;
                action = WagerAuditAction.BalanceSetToZero;
            }
            else if (request.Action == WagerBalanceAction.Add && request.Amount.HasValue)
            {
                newBalance = player.Balance + request.Amount.Value;
                action = WagerAuditAction.BalanceAdd;
            }
            else if (request.Action == WagerBalanceAction.Set && request.Amount.HasValue)
            {
                newBalance = request.Amount.Value;
                action = WagerAuditAction.BalanceSet;
            }
            else
                return new ApiError("Invalid balance action or missing amount", HttpStatusCode.BadRequest);

            var before = player.Balance;
            await _playerDAO.UpdatePlayerBalanceAsync(request.PlayerId, newBalance);
            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TargetPlayerId = request.PlayerId,
                ActorPlayerId = adminPlayerId,
                Action = action,
                Amount = request.Amount ?? 0,
                BalanceBefore = before,
                BalanceAfter = newBalance,
                CreatedAt = DateTime.UtcNow
            });
            return true;
        }

        public async Task<Operation<WagerSettingsModel, ApiError>> GetWagerSettingsAsync()
        {
            var settings = await _wagerSettingsDAO.GetAsync();
            return _mapper.Map<WagerSettingsModel>(settings);
        }

        public async Task<Operation<WagerSettingsModel, ApiError>> UpdateWagerSettingsAsync(WagerSettingsModel settings)
        {
            var dao = _mapper.Map<WagerSettingsDAOModel>(settings);
            await _wagerSettingsDAO.UpdateAsync(dao);
            return settings;
        }

        public async Task<Operation<List<WagerAuditEntryModel>, ApiError>> GetAllAuditAsync(int? tournamentId = null)
        {
            var entries = await _wagerAuditDAO.GetAllAsync(tournamentId);
            return _mapper.Map<List<WagerAuditEntryModel>>(entries).ToList();
        }

        public async Task<Operation<List<WagerModel>, ApiError>> GetPendingWagersForTournamentAsync(int tournamentId)
        {
            var rows = (await _wagerDAO.GetPendingByTournamentWithMatchupAsync(tournamentId)).ToList();
            var list = rows.Select(r => new WagerModel
            {
                WagerId = r.WagerId,
                PlayerId = r.PlayerId,
                GameResultId = r.GameResultId,
                TournamentId = r.TournamentId,
                MarketType = r.MarketType,
                Side = r.Side,
                StakeAmount = r.StakeAmount,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                CancelledAt = r.CancelledAt,
                SettledAt = r.SettledAt,
                Player1Name = string.IsNullOrWhiteSpace(r.Player1Name) ? string.Empty : r.Player1Name.Trim(),
                Player2Name = string.IsNullOrWhiteSpace(r.Player2Name) ? string.Empty : r.Player2Name.Trim(),
                MatchPlayer1Id = r.MatchPlayer1Id,
                MatchPlayer2Id = r.MatchPlayer2Id,
                OddsSpread = r.OddsSpread,
                OddsFavoredPlayerId = r.OddsFavoredPlayerId,
                OddsMoneyLinePlayer1 = r.OddsMoneyLinePlayer1,
                OddsMoneyLinePlayer2 = r.OddsMoneyLinePlayer2,
                OddsOverUnder = r.OddsOverUnder,
                BettorFullName = string.IsNullOrWhiteSpace(r.BettorFullName) ? null : r.BettorFullName.Trim()
            }).ToList();
            foreach (var w in list)
                WagerOrchestration.ApplyMyWagerListDisplayFields(w);
            return list;
        }

        public async Task<Operation<bool, ApiError>> AdminCancelWagerAsync(int adminPlayerId, int wagerId)
        {
            var wager = await _wagerDAO.GetByIdAsync(wagerId);
            if (wager == null)
                return new ApiError("Wager not found", HttpStatusCode.NotFound);
            if (wager.Status != WagerStatus.Pending)
                return new ApiError("Only pending wagers can be cancelled", HttpStatusCode.BadRequest);

            var player = await _playerDAO.GetPlayerAsync(wager.PlayerId);
            if (player == null)
                return new ApiError("Player not found", HttpStatusCode.NotFound);

            var cancelledAt = DateTime.UtcNow;
            await _wagerDAO.UpdateStatusAsync(wagerId, WagerStatus.Cancelled, cancelledAt: cancelledAt);
            var newBalance = player.Balance + wager.StakeAmount;
            await _playerDAO.UpdatePlayerBalanceAsync(wager.PlayerId, newBalance);
            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TournamentId = wager.TournamentId,
                TargetPlayerId = wager.PlayerId,
                ActorPlayerId = adminPlayerId,
                Action = WagerAuditAction.AdminCancelWager,
                WagerId = wagerId,
                GameResultId = wager.GameResultId,
                Amount = wager.StakeAmount,
                BalanceBefore = player.Balance,
                BalanceAfter = newBalance,
                CreatedAt = cancelledAt
            });
            return true;
        }

        public async Task<Operation<bool, ApiError>> UpdateGameOddsByGameResultIdAsync(int gameResultId, AdminUpdateGameOddsRequestModel request)
        {
            var existing = await _gameOddsDAO.GetByGameResultIdAsync(gameResultId);
            if (existing == null)
                return new ApiError("Odds not found for this game", HttpStatusCode.NotFound);

            var rows = await _gameOddsDAO.UpdateByGameResultIdAsync(
                gameResultId,
                request.Spread,
                request.FavoredPlayerId,
                request.MoneyLinePlayer1,
                request.MoneyLinePlayer2,
                request.OverUnder);
            if (rows < 1)
                return new ApiError("Odds not found for this game", HttpStatusCode.NotFound);
            return true;
        }

        public Task<Operation<SaveGameResultResponseModel, ApiError>> SaveGameResultAdminAsync(SaveGameResultRequestModel gameResult) =>
            _gameResultOrchestration.SaveGameResultAsync(gameResult);

        public async Task<Operation<List<AdminPlayerBalanceListItemModel>, ApiError>> ListPlayersForBalanceAdminAsync()
        {
            var allPlayers = (await _playerDAO.ListPlayersAsync(null, false)).ToList();
            var rows = allPlayers
                .GroupBy(p => p.PlayerId)
                .Select(g => g.First())
                .Select(p => new AdminPlayerBalanceListItemModel
                {
                    PlayerId = p.PlayerId,
                    FullName = string.IsNullOrWhiteSpace(p.FullName) ? $"Player {p.PlayerId}" : p.FullName.Trim(),
                    Balance = p.Balance
                })
                .OrderBy(x => x.FullName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return rows;
        }
    }
}
