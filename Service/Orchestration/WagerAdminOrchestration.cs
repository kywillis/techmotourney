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
        private readonly IWagerOrchestration _wagerOrchestration;
        private readonly ITournamentsDAO _tournamentsDAO;
        private readonly IMapper _mapper;
        private readonly ApplicationConfig _appConfig;

        public WagerAdminOrchestration(
            IPendingActivationDAO pendingActivationDAO,
            IPlayerDAO playerDAO,
            IWagerAuditDAO wagerAuditDAO,
            IWagerSettingsDAO wagerSettingsDAO,
            IWagerDAO wagerDAO,
            IGameOddsDAO gameOddsDAO,
            IGameResultOrchestration gameResultOrchestration,
            IWagerOrchestration wagerOrchestration,
            ITournamentsDAO tournamentsDAO,
            IMapper mapper,
            ApplicationConfig appConfig)
        {
            _pendingActivationDAO = pendingActivationDAO;
            _playerDAO = playerDAO;
            _wagerAuditDAO = wagerAuditDAO;
            _wagerSettingsDAO = wagerSettingsDAO;
            _wagerDAO = wagerDAO;
            _gameOddsDAO = gameOddsDAO;
            _gameResultOrchestration = gameResultOrchestration;
            _wagerOrchestration = wagerOrchestration;
            _tournamentsDAO = tournamentsDAO;
            _mapper = mapper;
            _appConfig = appConfig;
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
                WagerOrchestration.ApplyMyWagerListDisplayFields(w, _appConfig.WageringVigPercent);
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

        public async Task<Operation<List<WagerAuditEntryModel>, ApiError>> GetPlayerAuditAsync(int playerId, int? tournamentId = null)
        {
            if (playerId < 1)
                return new ApiError("playerId is required", HttpStatusCode.BadRequest);
            var player = await _playerDAO.GetPlayerAsync(playerId);
            if (player == null)
                return new ApiError("Player not found", HttpStatusCode.NotFound);
            var entries = await _wagerAuditDAO.GetByTargetPlayerIdAsync(playerId, tournamentId);
            var list = _mapper.Map<List<WagerAuditEntryModel>>(entries).ToList();
            return list;
        }

        public Task<Operation<TournamentSummaryModel, ApiError>> GetPlayerTournamentSummaryAsync(int playerId, int tournamentId) =>
            _wagerOrchestration.GetTournamentSummaryForUserAsync(playerId, tournamentId);

        public async Task<Operation<WagerTournamentSnapshotModel, ApiError>> GetWagerTournamentSnapshotAsync(int tournamentId)
        {
            if (tournamentId < 1)
                return new ApiError("tournamentId is required", HttpStatusCode.BadRequest);

            var tDao = await _tournamentsDAO.GetById(tournamentId);
            if (tDao == null)
                return new ApiError("Tournament not found", HttpStatusCode.NotFound);

            var settledHouseNet = await _wagerDAO.GetSettledWagerNetForTournamentAsync(tournamentId);
            var (pendingTotal, pendingCount) = await _wagerDAO.GetTournamentPendingStakeSummaryAsync(tournamentId);

            var pnlRows = await _wagerDAO.GetPlayerSettledPnlByTournamentAsync(tournamentId);
            var pendingByPlayer = (await _wagerDAO.GetPendingStakeByPlayerForTournamentAsync(tournamentId)).ToDictionary(x => x.PlayerId);
            var pendingByGame = (await _wagerDAO.GetPendingStakeByGameForTournamentAsync(tournamentId)).ToDictionary(x => x.GameResultId);

            var playerIds = pnlRows.Select(p => p.PlayerId)
                .Union(pendingByPlayer.Keys)
                .Distinct()
                .ToList();

            var playerRows = new List<WagerSnapshotPlayerRowModel>();
            foreach (var pid in playerIds.OrderBy(id => id))
            {
                var p = await _playerDAO.GetPlayerAsync(pid);
                var name = p == null || string.IsNullOrWhiteSpace(p.FullName) ? $"Player {pid}" : p.FullName.Trim();
                pendingByPlayer.TryGetValue(pid, out var pend);
                var pnl = pnlRows.FirstOrDefault(r => r.PlayerId == pid)?.SettledPnl ?? 0m;
                playerRows.Add(new WagerSnapshotPlayerRowModel
                {
                    PlayerId = pid,
                    DisplayName = name,
                    SettledPlayerPnl = pnl,
                    PendingStake = pend?.StakeTotal ?? 0m,
                    PendingWagerCount = pend?.WagerCount ?? 0
                });
            }

            playerRows = playerRows.OrderBy(r => r.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

            var gamesOp = await _gameResultOrchestration.ListResultsByTournamentAsync(tournamentId, false);
            if (!gamesOp.IsSuccess)
                return gamesOp.Failure!;

            var tourneyGames = gamesOp.Data ?? new List<GameResultModel>();
            var idOrder = tourneyGames.Select(g => g.GameResultId).ToList();
            var distinctGameIds = (await _wagerDAO.GetDistinctGameResultIdsWithWagersForTournamentAsync(tournamentId)).ToList();
            distinctGameIds.Sort((a, b) =>
            {
                var ia = idOrder.IndexOf(a);
                var ib = idOrder.IndexOf(b);
                if (ia < 0 && ib < 0)
                    return a.CompareTo(b);
                if (ia < 0)
                    return 1;
                if (ib < 0)
                    return -1;
                return ia.CompareTo(ib);
            });

            var gameRows = new List<WagerSnapshotGameRowModel>();
            foreach (var gid in distinctGameIds)
            {
                var net = await _wagerDAO.GetSettledWagerNetForGameResultAsync(gid);
                pendingByGame.TryGetValue(gid, out var pg);
                var gr = tourneyGames.FirstOrDefault(x => x.GameResultId == gid);
                var label = gr == null
                    ? $"Game {gid}"
                    : BuildSnapshotGameLabel(gr);
                gameRows.Add(new WagerSnapshotGameRowModel
                {
                    GameResultId = gid,
                    Label = label,
                    SettledHouseNet = net,
                    PendingStake = pg?.StakeTotal ?? 0m,
                    PendingWagerCount = pg?.WagerCount ?? 0
                });
            }

            return new WagerTournamentSnapshotModel
            {
                TournamentId = tournamentId,
                TournamentName = tDao.Name?.Trim() ?? string.Empty,
                SettledHouseNet = settledHouseNet,
                PendingStakeTotal = pendingTotal,
                PendingWagerCount = pendingCount,
                Players = playerRows,
                Games = gameRows
            };
        }

        private static string BuildSnapshotGameLabel(GameResultModel g)
        {
            var p1 = g.Player1;
            var p2 = g.Player2;
            var n1 = string.IsNullOrWhiteSpace(p1?.PlayerName) ? $"P{p1?.PlayerId}" : p1!.PlayerName.Trim();
            var n2 = string.IsNullOrWhiteSpace(p2?.PlayerName) ? $"P{p2?.PlayerId}" : p2!.PlayerName.Trim();
            if (!string.IsNullOrWhiteSpace(p1?.TeamName) && !string.IsNullOrWhiteSpace(p2?.TeamName))
                return FormattableString.Invariant($"({p1!.TeamName!.Trim()}) {n1} vs {n2} ({p2!.TeamName!.Trim()})");
            if (!string.IsNullOrWhiteSpace(p1?.TeamName))
                return FormattableString.Invariant($"({p1!.TeamName!.Trim()}) {n1} vs {n2}");
            if (!string.IsNullOrWhiteSpace(p2?.TeamName))
                return FormattableString.Invariant($"{n1} vs {n2} ({p2!.TeamName!.Trim()})");
            return FormattableString.Invariant($"{n1} vs {n2}");
        }

        public async Task<Operation<List<WagerModel>, ApiError>> GetWagersForPlayerTournamentAdminAsync(int tournamentId, int playerId)
        {
            if (tournamentId < 1 || playerId < 1)
                return new ApiError("tournamentId and playerId are required", HttpStatusCode.BadRequest);

            var tDao = await _tournamentsDAO.GetById(tournamentId);
            if (tDao == null)
                return new ApiError("Tournament not found", HttpStatusCode.NotFound);

            var found = (await _wagerDAO.GetByTournamentIdAsync(tournamentId)).Any(w => w.PlayerId == playerId);
            if (!found)
                return new List<WagerModel>();

            var rows = (await _wagerDAO.GetByPlayerIdWithMatchupAsync(playerId, tournamentId, null)).ToList();
            var list = rows.Select(MapWagerWithMatchupToModel).ToList();
            foreach (var w in list)
                WagerOrchestration.ApplyMyWagerListDisplayFields(w, _appConfig.WageringVigPercent);
            return list;
        }

        public async Task<Operation<List<WagerModel>, ApiError>> GetWagersForGameAdminAsync(int gameResultId, int? expectedTournamentId = null)
        {
            if (gameResultId < 1)
                return new ApiError("gameResultId is required", HttpStatusCode.BadRequest);

            var rows = (await _wagerDAO.GetWagersWithMatchupByGameResultIdAsync(gameResultId)).ToList();
            if (rows.Count < 1)
                return new List<WagerModel>();

            if (expectedTournamentId.HasValue && expectedTournamentId >= 1 &&
                rows[0].TournamentId != expectedTournamentId.Value)
                return new ApiError("Game is not in that tournament", HttpStatusCode.BadRequest);

            var list = rows.Select(MapWagerWithMatchupToModel).ToList();
            foreach (var w in list)
                WagerOrchestration.ApplyMyWagerListDisplayFields(w, _appConfig.WageringVigPercent);
            return list;
        }

        private WagerModel MapWagerWithMatchupToModel(WagerWithMatchupDAOModel r) =>
            new()
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
                BettorFullName = null
            };
    }
}
