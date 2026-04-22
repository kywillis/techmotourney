using System.Net;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration
{
    public class WagerOrchestration : IWagerOrchestration
    {
        private const decimal MinStake = 1m;
        /// <summary>Max dollars at risk on spread / over-under (even money).</summary>
        private const decimal MaxRiskSpreadOrOverUnder = 40m;
        /// <summary>Max win (profit) on money line; max stake is derived from the line.</summary>
        private const decimal MaxWinMoneyLine = 40m;

        private readonly IPlayerDAO _playerDAO;
        private readonly IWagerDAO _wagerDAO;
        private readonly IWagerAuditDAO _wagerAuditDAO;
        private readonly IWagerSettingsDAO _wagerSettingsDAO;
        private readonly ITournamentsOrchestration _tournamentsOrchestration;
        private readonly IGameResultDAO _gameResultDAO;
        private readonly IGameOddsDAO _gameOddsDAO;
        private readonly IMapper _mapper;

        public WagerOrchestration(
            IPlayerDAO playerDAO,
            IWagerDAO wagerDAO,
            IWagerAuditDAO wagerAuditDAO,
            IWagerSettingsDAO wagerSettingsDAO,
            ITournamentsOrchestration tournamentsOrchestration,
            IGameResultDAO gameResultDAO,
            IGameOddsDAO gameOddsDAO,
            IMapper mapper)
        {
            _playerDAO = playerDAO;
            _wagerDAO = wagerDAO;
            _wagerAuditDAO = wagerAuditDAO;
            _wagerSettingsDAO = wagerSettingsDAO;
            _tournamentsOrchestration = tournamentsOrchestration;
            _gameResultDAO = gameResultDAO;
            _gameOddsDAO = gameOddsDAO;
            _mapper = mapper;
        }

        public async Task<Operation<decimal, ApiError>> GetBalanceAsync(int playerId)
        {
            var player = await _playerDAO.GetPlayerAsync(playerId);
            if (player == null)
                return new ApiError("Player not found", HttpStatusCode.NotFound);
            return player.Balance;
        }

        public async Task<Operation<List<WagerModel>, ApiError>> GetMyWagersAsync(
            int playerId,
            WagerStatus? statusFilter = null,
            int? tournamentId = null)
        {
            var rows = (await _wagerDAO.GetByPlayerIdWithMatchupAsync(playerId, tournamentId, statusFilter)).ToList();
            // Explicit map (avoid AutoMapper ambiguity with WagerDAOModel -> WagerModel dropping matchup fields).
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
                OddsOverUnder = r.OddsOverUnder
            }).ToList();
            await EnrichMyWagerNamesWhenMissingAsync(list);
            foreach (var w in list)
                ApplyMyWagerListDisplayFields(w);
            return list;
        }

        internal static void ApplyMyWagerListDisplayFields(WagerModel w)
        {
            w.PickDescription = BuildMyWagerPickDescription(w);
            w.PotentialPayout = Math.Round(ComputePotentialTotalReturnOnWin(w), 2, MidpointRounding.AwayFromZero);
        }

        private static string BuildMyWagerPickDescription(WagerModel w)
        {
            var p1 = string.IsNullOrWhiteSpace(w.Player1Name) ? "—" : w.Player1Name.Trim();
            var p2 = string.IsNullOrWhiteSpace(w.Player2Name) ? "—" : w.Player2Name.Trim();

            switch (w.MarketType)
            {
                case WagerMarketType.Spread:
                    var mag = Math.Abs(w.OddsSpread);
                    var fav = w.OddsFavoredPlayerId;
                    decimal lineP1 = mag;
                    decimal lineP2 = mag;
                    if (fav.HasValue)
                    {
                        if (fav.Value == w.MatchPlayer1Id)
                        {
                            lineP1 = -mag;
                            lineP2 = mag;
                        }
                        else if (fav.Value == w.MatchPlayer2Id)
                        {
                            lineP1 = mag;
                            lineP2 = -mag;
                        }
                    }

                    if (w.Side == WagerSide.Player1Spread)
                        return $"{p1} (spread {FormatSignedSpreadLine(lineP1)})";
                    if (w.Side == WagerSide.Player2Spread)
                        return $"{p2} (spread {FormatSignedSpreadLine(lineP2)})";
                    return w.Side.ToString();

                case WagerMarketType.MoneyLine:
                    if (w.Side == WagerSide.Player1ML)
                        return FormatPlayerMoneyLinePick(p1, w.OddsMoneyLinePlayer1);
                    if (w.Side == WagerSide.Player2ML)
                        return FormatPlayerMoneyLinePick(p2, w.OddsMoneyLinePlayer2);
                    return w.Side.ToString();

                case WagerMarketType.OverUnder:
                    var ou = w.OddsOverUnder;
                    if (w.Side == WagerSide.Over)
                        return ou.HasValue ? $"Over (total {ou.Value:0.##})" : "Over";
                    if (w.Side == WagerSide.Under)
                        return ou.HasValue ? $"Under (total {ou.Value:0.##})" : "Under";
                    return w.Side.ToString();

                default:
                    return string.Empty;
            }
        }

        private static string FormatPlayerMoneyLinePick(string playerName, decimal? americanOdds)
        {
            var oddsStr = FormatAmericanOdds(americanOdds);
            return string.IsNullOrEmpty(oddsStr)
                ? $"{playerName} (moneyline)"
                : $"{playerName} (moneyline {oddsStr})";
        }

        /// <summary>Stake plus profit if the bet wins (matches even-money spread/O-U and American ML).</summary>
        private static decimal ComputePotentialTotalReturnOnWin(WagerModel w)
        {
            var s = w.StakeAmount;
            if (s <= 0)
                return 0;

            switch (w.MarketType)
            {
                case WagerMarketType.Spread:
                case WagerMarketType.OverUnder:
                    return s * 2m;

                case WagerMarketType.MoneyLine:
                    var odds = w.Side == WagerSide.Player1ML
                        ? w.OddsMoneyLinePlayer1
                        : w.Side == WagerSide.Player2ML
                            ? w.OddsMoneyLinePlayer2
                            : null;
                    if (!odds.HasValue || odds.Value == 0)
                        return s * 2m;
                    return s + ProfitFromAmericanOddsStake(s, odds.Value);

                default:
                    return s;
            }
        }

        private static decimal ProfitFromAmericanOddsStake(decimal stake, decimal american)
        {
            if (american > 0)
                return stake * american / 100m;
            return stake * 100m / (-american);
        }

        private static string FormatSignedSpreadLine(decimal line)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return line > 0
                ? $"+{line.ToString("0.0", inv)}"
                : line.ToString("0.0", inv);
        }

        private static string FormatAmericanOdds(decimal? line)
        {
            if (!line.HasValue)
                return string.Empty;
            var v = line.Value;
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            return v > 0 ? $"+{v.ToString("0.0", inv)}" : v.ToString("0.0", inv);
        }

        /// <summary>Fills player names from game/players when SQL join did not map into the row.</summary>
        private async Task EnrichMyWagerNamesWhenMissingAsync(List<WagerModel> list)
        {
            var gameCache = new Dictionary<int, GameResultDAOModel?>();
            var nameCache = new Dictionary<int, string>();

            async Task<string> ResolveNameAsync(int playerId)
            {
                if (nameCache.TryGetValue(playerId, out var cached))
                    return cached;
                var p = await _playerDAO.GetPlayerAsync(playerId);
                var n = string.IsNullOrWhiteSpace(p?.FullName) ? $"Player {playerId}" : p!.FullName.Trim();
                nameCache[playerId] = n;
                return n;
            }

            foreach (var w in list)
            {
                if (!w.GameResultId.HasValue)
                    continue;

                var gid = w.GameResultId.Value;
                var needNames = string.IsNullOrWhiteSpace(w.Player1Name) || string.IsNullOrWhiteSpace(w.Player2Name);
                var needMatchIds = w.MatchPlayer1Id == 0 || w.MatchPlayer2Id == 0;
                if (!needNames && !needMatchIds)
                    continue;

                if (!gameCache.TryGetValue(gid, out var game))
                {
                    game = await _gameResultDAO.GetGameResultAsync(gid);
                    gameCache[gid] = game;
                }

                if (game == null)
                    continue;

                if (w.MatchPlayer1Id == 0)
                    w.MatchPlayer1Id = game.Player1Id;
                if (w.MatchPlayer2Id == 0)
                    w.MatchPlayer2Id = game.Player2Id;

                if (needNames)
                {
                    if (string.IsNullOrWhiteSpace(w.Player1Name))
                        w.Player1Name = await ResolveNameAsync(game.Player1Id);
                    if (string.IsNullOrWhiteSpace(w.Player2Name))
                        w.Player2Name = await ResolveNameAsync(game.Player2Id);
                }
            }
        }

        public async Task<Operation<List<WagerAuditEntryModel>, ApiError>> GetMyAuditAsync(int playerId, int? tournamentId = null)
        {
            var entries = await _wagerAuditDAO.GetByTargetPlayerIdAsync(playerId, tournamentId);
            var list = _mapper.Map<List<WagerAuditEntryModel>>(entries);
            return list;
        }

        public async Task<Operation<decimal?, ApiError>> GetFinalBalanceForTournamentAsync(int playerId, int tournamentId)
        {
            var entries = (await _wagerAuditDAO.GetByTargetPlayerIdAsync(playerId, tournamentId))
                .OrderByDescending(e => e.CreatedAt)
                .ToList();
            var lastWithBalance = entries.FirstOrDefault(e => e.BalanceAfter.HasValue);
            return lastWithBalance?.BalanceAfter;
        }

        public async Task<Operation<List<BettableGameModel>, ApiError>> GetGamesAvailableToBetAsync()
        {
            var activeOp = await _tournamentsOrchestration.GetActive();
            if (!activeOp.IsSuccess)
                return activeOp.Failure;
            var tournamentId = activeOp.Data.TournamentId;

            var games = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId, false))
                .Where(g => g.StatusId == (int)GameStatus.Waiting && g.GameStartedAt == null && !g.IsDeleted)
                .ToList();
            var allOdds = (await _gameOddsDAO.GetByTournamentIdAsync(tournamentId))
                .Where(o => o.GameResultId.HasValue)
                .ToList();
            var gameResultIdsWithOdds = allOdds.Select(o => o.GameResultId!.Value).ToHashSet();
            var bettableGames = games.Where(g => gameResultIdsWithOdds.Contains(g.GameResultId)).ToList();

            var settings = await _wagerSettingsDAO.GetAsync();
            var result = new List<BettableGameModel>();
            foreach (var g in bettableGames)
            {
                var odds = allOdds.FirstOrDefault(o => o.GameResultId == g.GameResultId);
                if (odds == null) continue;
                var model = await BuildBettableGameModelAsync(g, odds, settings);
                result.Add(model);
            }
            return result;
        }

        public async Task<Operation<WagerGamesBoardModel, ApiError>> GetGamesBoardAsync()
        {
            var activeOp = await _tournamentsOrchestration.GetActive();
            if (!activeOp.IsSuccess)
                return activeOp.Failure;
            var tournamentId = activeOp.Data.TournamentId;

            var games = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId, false))
                .Where(g => !g.IsDeleted)
                .ToList();
            var allOdds = (await _gameOddsDAO.GetByTournamentIdAsync(tournamentId))
                .Where(o => o.GameResultId.HasValue)
                .ToList();
            var oddsByGameId = allOdds.ToDictionary(o => o.GameResultId!.Value);

            var settings = await _wagerSettingsDAO.GetAsync();
            var open = new List<BettableGameModel>();
            var inProgress = new List<BettableGameModel>();
            var completed = new List<BettableGameModel>();

            foreach (var g in games)
            {
                if (!oddsByGameId.TryGetValue(g.GameResultId, out var odds))
                    continue;

                var model = await BuildBettableGameModelAsync(g, odds, settings);
                if (g.StatusId == (int)GameStatus.Completed)
                {
                    completed.Add(model);
                }
                else if (g.StatusId == (int)GameStatus.InProgress)
                {
                    inProgress.Add(model);
                }
                else if (g.StatusId == (int)GameStatus.Waiting)
                {
                    if (g.GameStartedAt == null)
                        open.Add(model);
                    else
                        inProgress.Add(model);
                }
            }

            open.Sort((a, b) => a.GameResultId.CompareTo(b.GameResultId));
            inProgress.Sort((a, b) => a.GameResultId.CompareTo(b.GameResultId));
            completed.Sort((a, b) => b.GameResultId.CompareTo(a.GameResultId));

            return new WagerGamesBoardModel
            {
                OpenForBetting = open,
                InProgress = inProgress,
                Completed = completed
            };
        }

        public async Task<Operation<BettableGameModel, ApiError>> GetGameDetailForWagerAsync(int gameResultId)
        {
            var game = await _gameResultDAO.GetGameResultAsync(gameResultId);
            if (game == null || game.IsDeleted)
                return new ApiError("Game not found", HttpStatusCode.NotFound);
            var odds = await _gameOddsDAO.GetByGameResultIdAsync(gameResultId);
            if (odds == null)
                return new ApiError("Odds not set for this game", HttpStatusCode.NotFound);
            var settings = await _wagerSettingsDAO.GetAsync();
            var model = await BuildBettableGameModelAsync(game, odds, settings);
            return model;
        }

        public async Task<Operation<BettableGameModel, ApiError>> GetGameDetailForAdminAsync(int gameResultId)
        {
            var game = await _gameResultDAO.GetGameResultAsync(gameResultId);
            if (game == null || game.IsDeleted)
                return new ApiError("Game not found", HttpStatusCode.NotFound);
            var odds = await _gameOddsDAO.GetByGameResultIdAsync(gameResultId);
            if (odds == null)
                return new ApiError("Odds not set for this game", HttpStatusCode.NotFound);
            var settings = await _wagerSettingsDAO.GetAsync();
            var model = await BuildBettableGameModelAsync(game, odds, settings);
            return model;
        }

        public async Task<Operation<PublicWageringSnapshotModel, ApiError>> GetPublicWageringSnapshotAsync(int gameResultId)
        {
            var game = await _gameResultDAO.GetGameResultAsync(gameResultId);
            if (game == null || game.IsDeleted)
                return new ApiError("Game not found", HttpStatusCode.NotFound);
            var odds = await _gameOddsDAO.GetByGameResultIdAsync(gameResultId);
            if (odds == null)
                return new ApiError("Odds not set for this game", HttpStatusCode.NotFound);

            var settings = await _wagerSettingsDAO.GetAsync();
            var p1 = await _playerDAO.GetPlayerAsync(game.Player1Id);
            var p2 = await _playerDAO.GetPlayerAsync(game.Player2Id);
            var pendingWagers = (await _wagerDAO.GetByGameResultIdAsync(gameResultId))
                .Where(w => w.Status == WagerStatus.Pending)
                .ToList();

            return BuildPublicWageringSnapshot(game, odds, p1, p2, pendingWagers, settings.MaxMarketImbalance);
        }

        public async Task<Operation<List<PublicWageringSnapshotModel>, ApiError>> GetPublicWageringSnapshotsByTournamentAsync(
            int tournamentId)
        {
            var settings = await _wagerSettingsDAO.GetAsync();
            var oddsList = (await _gameOddsDAO.GetByTournamentIdAsync(tournamentId)).ToList();
            var gamesById = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId, false))
                .Where(g => !g.IsDeleted)
                .ToDictionary(g => g.GameResultId);

            var pendingByGame = (await _wagerDAO.GetByTournamentIdAsync(tournamentId))
                .Where(w => w.Status == WagerStatus.Pending && w.GameResultId.HasValue)
                .GroupBy(w => w.GameResultId!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var playerCache = new Dictionary<int, PlayerDAOModel?>();
            var list = new List<PublicWageringSnapshotModel>();
            var seenGameIds = new HashSet<int>();

            foreach (var odds in oddsList)
            {
                if (!odds.GameResultId.HasValue)
                    continue;
                var gid = odds.GameResultId.Value;
                if (!seenGameIds.Add(gid))
                    continue;
                if (!gamesById.TryGetValue(gid, out var game))
                    continue;

                if (!playerCache.ContainsKey(game.Player1Id))
                    playerCache[game.Player1Id] = await _playerDAO.GetPlayerAsync(game.Player1Id);
                if (!playerCache.ContainsKey(game.Player2Id))
                    playerCache[game.Player2Id] = await _playerDAO.GetPlayerAsync(game.Player2Id);
                var p1 = playerCache[game.Player1Id];
                var p2 = playerCache[game.Player2Id];

                pendingByGame.TryGetValue(gid, out var pending);
                pending ??= new List<WagerDAOModel>();

                list.Add(BuildPublicWageringSnapshot(game, odds, p1, p2, pending, settings.MaxMarketImbalance));
            }

            return list;
        }

        private static PublicWageringSnapshotModel BuildPublicWageringSnapshot(
            GameResultDAOModel game,
            GameOddsDAOModel odds,
            PlayerDAOModel? p1,
            PlayerDAOModel? p2,
            List<WagerDAOModel> pendingWagers,
            decimal maxMarketImbalance)
        {
            return new PublicWageringSnapshotModel
            {
                GameResultId = game.GameResultId,
                Player1Id = game.Player1Id,
                Player2Id = game.Player2Id,
                Player1Name = string.IsNullOrWhiteSpace(p1?.FullName) ? $"Player {game.Player1Id}" : p1!.FullName.Trim(),
                Player2Name = string.IsNullOrWhiteSpace(p2?.FullName) ? $"Player {game.Player2Id}" : p2!.FullName.Trim(),
                Player1ProfilePic = p1?.ProfilePic ?? 0,
                Player2ProfilePic = p2?.ProfilePic ?? 0,
                Odds = new PublicWageringOddsModel
                {
                    Spread = odds.Spread,
                    FavoredPlayerId = odds.FavoredPlayerId,
                    OverUnder = odds.OverUnder,
                    MoneyLinePlayer1 = odds.MoneyLinePlayer1,
                    MoneyLinePlayer2 = odds.MoneyLinePlayer2,
                    Summary = odds.Summary?.Trim() ?? string.Empty
                },
                MarketDepth = BuildMarketDepth(pendingWagers, maxMarketImbalance)
            };
        }

        private static BettableGameMarketDepthModel BuildMarketDepth(
            IEnumerable<WagerDAOModel> pendingWagers,
            decimal maxMarketImbalance)
        {
            var d = new BettableGameMarketDepthModel { MaxMarketImbalance = maxMarketImbalance };
            foreach (var w in pendingWagers)
            {
                switch (w.MarketType)
                {
                    case WagerMarketType.Spread:
                        if (w.Side == WagerSide.Player1Spread) d.SpreadPlayer1 += w.StakeAmount;
                        else if (w.Side == WagerSide.Player2Spread) d.SpreadPlayer2 += w.StakeAmount;
                        break;
                    case WagerMarketType.OverUnder:
                        if (w.Side == WagerSide.Over) d.Over += w.StakeAmount;
                        else if (w.Side == WagerSide.Under) d.Under += w.StakeAmount;
                        break;
                    case WagerMarketType.MoneyLine:
                        if (w.Side == WagerSide.Player1ML) d.MoneyLinePlayer1 += w.StakeAmount;
                        else if (w.Side == WagerSide.Player2ML) d.MoneyLinePlayer2 += w.StakeAmount;
                        break;
                }
            }
            return d;
        }

        private static decimal GetMaxStakeForMarket(WagerMarketType market, WagerSide side, GameOddsDAOModel odds)
        {
            return market switch
            {
                WagerMarketType.Spread or WagerMarketType.OverUnder => MaxRiskSpreadOrOverUnder,
                WagerMarketType.MoneyLine => MaxStakeForMoneyLineSide(side, odds),
                _ => MaxRiskSpreadOrOverUnder
            };
        }

        private static decimal MaxStakeForMoneyLineSide(WagerSide side, GameOddsDAOModel odds)
        {
            decimal? line = side switch
            {
                WagerSide.Player1ML => odds.MoneyLinePlayer1,
                WagerSide.Player2ML => odds.MoneyLinePlayer2,
                _ => null
            };
            if (!line.HasValue || line.Value == 0)
                return MaxRiskSpreadOrOverUnder;
            var l = line.Value;
            if (l > 0)
                return Math.Floor(MaxWinMoneyLine * 100m / l);
            return Math.Floor(MaxWinMoneyLine * Math.Abs(l) / 100m);
        }

        private async Task<BettableGameModel> BuildBettableGameModelAsync(
            GameResultDAOModel game,
            GameOddsDAOModel odds,
            WagerSettingsDAOModel settings)
        {
            var p1 = await _playerDAO.GetPlayerAsync(game.Player1Id);
            var p2 = await _playerDAO.GetPlayerAsync(game.Player2Id);
            var model = new BettableGameModel
            {
                GameResultId = game.GameResultId,
                TournamentId = game.TournamentId,
                Player1Id = game.Player1Id,
                Player2Id = game.Player2Id,
                Player1Name = p1?.FullName ?? $"Player {game.Player1Id}",
                Player2Name = p2?.FullName ?? $"Player {game.Player2Id}",
                Player1ProfilePic = p1?.ProfilePic ?? 0,
                Player2ProfilePic = p2?.ProfilePic ?? 0,
                GameStartedAt = game.GameStartedAt,
                Odds = new BettableGameOddsModel
                {
                    Spread = odds.Spread,
                    FavoredPlayerId = odds.FavoredPlayerId,
                    OverUnder = odds.OverUnder,
                    MoneyLinePlayer1 = odds.MoneyLinePlayer1,
                    MoneyLinePlayer2 = odds.MoneyLinePlayer2,
                    Summary = odds.Summary?.Trim() ?? string.Empty
                },
                GameStatus = ((GameStatus)game.StatusId).ToString(),
                IsOpenForBetting = game.StatusId == (int)GameStatus.Waiting && !game.GameStartedAt.HasValue
            };
            if (game.StatusId == (int)GameStatus.Completed)
            {
                model.Player1Score = game.Player1Score;
                model.Player2Score = game.Player2Score;
            }
            var pendingWagers = (await _wagerDAO.GetByGameResultIdAsync(game.GameResultId))
                .Where(w => w.Status == WagerStatus.Pending)
                .ToList();
            model.MarketDepth = BuildMarketDepth(pendingWagers, settings.MaxMarketImbalance);
            if (settings.ShowActionOnGames)
            {
                var actionList = new List<WagerActionItemModel>();
                foreach (var w in pendingWagers)
                {
                    var player = await _playerDAO.GetPlayerAsync(w.PlayerId);
                    actionList.Add(new WagerActionItemModel
                    {
                        PlayerName = player?.FullName ?? $"Player {w.PlayerId}",
                        Side = w.Side,
                        StakeAmount = w.StakeAmount
                    });
                }
                model.Action = actionList;
            }
            return model;
        }

        public async Task<Operation<TournamentSummaryModel, ApiError>> GetTournamentSummaryForUserAsync(int playerId, int tournamentId)
        {
            var entries = (await _wagerAuditDAO.GetByTargetPlayerIdAsync(playerId, tournamentId)).ToList();
            var wins = entries.Count(e => e.Action == WagerAuditAction.SettleWagerWin);
            var losses = entries.Count(e => e.Action == WagerAuditAction.SettleWagerLose);
            var lastWithBalance = entries.OrderByDescending(e => e.CreatedAt).FirstOrDefault(e => e.BalanceAfter.HasValue);
            var netAmount = lastWithBalance?.BalanceAfter ?? 0;
            var tournamentOp = await _tournamentsOrchestration.GetById(tournamentId);
            var name = tournamentOp.IsSuccess ? tournamentOp.Data.Name : "";
            return new TournamentSummaryModel
            {
                TournamentId = tournamentId,
                TournamentName = name,
                Wins = wins,
                Losses = losses,
                NetAmount = netAmount
            };
        }

        public async Task<Operation<WagerModel, ApiError>> PlaceWagerAsync(int playerId, PlaceWagerRequestModel request)
        {
            var game = await _gameResultDAO.GetGameResultAsync(request.GameResultId);
            if (game == null || game.IsDeleted)
                return new ApiError("Game not found", HttpStatusCode.NotFound);
            if (game.StatusId != (int)GameStatus.Waiting || game.GameStartedAt.HasValue)
                return new ApiError("Game is not open for betting", HttpStatusCode.BadRequest);
            if (game.Player1Id == playerId || game.Player2Id == playerId)
                return new ApiError("You cannot wager on a game you are playing in", HttpStatusCode.BadRequest);

            var odds = await _gameOddsDAO.GetByGameResultIdAsync(request.GameResultId);
            if (odds == null)
                return new ApiError("Odds not set for this game", HttpStatusCode.BadRequest);

            var maxStakeForBet = GetMaxStakeForMarket(request.MarketType, request.Side, odds);
            if (maxStakeForBet < MinStake)
                return new ApiError("House limits do not allow a wager on this selection.", HttpStatusCode.BadRequest);
            if (request.StakeAmount < MinStake || request.StakeAmount > maxStakeForBet)
                return new ApiError($"Stake must be between ${MinStake} and ${maxStakeForBet:0}", HttpStatusCode.BadRequest);

            var player = await _playerDAO.GetPlayerAsync(playerId);
            if (player == null)
                return new ApiError("Player not found", HttpStatusCode.NotFound);
            if (player.Balance < request.StakeAmount)
                return new ApiError("Insufficient balance", HttpStatusCode.BadRequest);

            var settings = await _wagerSettingsDAO.GetAsync();
            var existingWagers = (await _wagerDAO.GetByGameResultIdAsync(request.GameResultId))
                .Where(w => w.Status == WagerStatus.Pending && w.MarketType == request.MarketType)
                .ToList();
            var sideTotals = existingWagers
                .GroupBy(w => w.Side)
                .ToDictionary(g => g.Key, g => g.Sum(w => w.StakeAmount));
            decimal totalMySide = sideTotals.GetValueOrDefault(request.Side, 0) + request.StakeAmount;
            var otherSides = request.MarketType switch
            {
                WagerMarketType.Spread => new[] { WagerSide.Player1Spread, WagerSide.Player2Spread },
                WagerMarketType.OverUnder => new[] { WagerSide.Over, WagerSide.Under },
                WagerMarketType.MoneyLine => new[] { WagerSide.Player1ML, WagerSide.Player2ML },
                _ => Array.Empty<WagerSide>()
            };
            decimal totalOther = otherSides
                .Where(s => s != request.Side)
                .Sum(s => sideTotals.GetValueOrDefault(s, 0));
            if (Math.Abs(totalMySide - totalOther) > settings.MaxMarketImbalance)
                return new ApiError($"This side is full until there's more action on the other side (max imbalance ${settings.MaxMarketImbalance})", HttpStatusCode.BadRequest);

            var wager = new WagerDAOModel
            {
                PlayerId = playerId,
                GameResultId = request.GameResultId,
                TournamentId = game.TournamentId,
                MarketType = request.MarketType,
                Side = request.Side,
                StakeAmount = request.StakeAmount,
                Status = WagerStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            wager = await _wagerDAO.CreateAsync(wager);
            var newBalance = player.Balance - request.StakeAmount;
            await _playerDAO.UpdatePlayerBalanceAsync(playerId, newBalance);
            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TournamentId = game.TournamentId,
                TargetPlayerId = playerId,
                ActorPlayerId = playerId,
                Action = WagerAuditAction.PlaceWager,
                WagerId = wager.WagerId,
                GameResultId = request.GameResultId,
                Amount = request.StakeAmount,
                BalanceBefore = player.Balance,
                BalanceAfter = newBalance,
                CreatedAt = DateTime.UtcNow
            });
            return _mapper.Map<WagerModel>(wager);
        }
    }
}
