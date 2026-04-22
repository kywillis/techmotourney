using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.DataAccess.Interfaces;
using AutoMapper;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.ResultPattern;
using System.Net;
using Microsoft.IdentityModel.Tokens;
using TecmoTourney.DataAccess;
using System.Numerics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using TecmoTourney.Bracket;

namespace TecmoTourney.Orchestration 
{
    public class TournamentsOrchestration : ITournamentsOrchestration
    {
        private const int PRELIM_GAMES = 2;
        private const int POINTS_FOR_WIN = 20;
        private readonly ITournamentsDAO _tournamentsDAO;
        private readonly IPlayerTournamentDAO _playerTournamentDAO;
        private readonly IPlayerDAO _playerDAO;
        private readonly IMapper _mapper;
        private readonly IGameResultDAO _gameResultDAO;
        private readonly IGameOddsDAO _gameOddsDAO;
        private readonly IGameOddsGenerationService _gameOddsGenerationService;
        private readonly IWagerDetachmentService _wagerDetachmentService;
        private readonly ITournamentBracketReconciliationService _tournamentBracketReconciliationService;

        private List<BracketMatchup> _existingPointSpeads = new List<BracketMatchup>() { };

        public TournamentsOrchestration(
            ITournamentsDAO tournamentsDAO,
            IPlayerTournamentDAO playerTournamentDAO,
            IGameResultDAO gameResultDAO,
            IPlayerDAO playerDAO,
            IMapper mapper,
            IGameOddsDAO gameOddsDAO,
            IGameOddsGenerationService gameOddsGenerationService,
            IWagerDetachmentService wagerDetachmentService,
            ITournamentBracketReconciliationService tournamentBracketReconciliationService)
        {
            _gameResultDAO = gameResultDAO;
            _playerTournamentDAO = playerTournamentDAO;
            _tournamentsDAO = tournamentsDAO;
            _playerDAO = playerDAO;
            _mapper = mapper;
            _gameOddsDAO = gameOddsDAO;
            _gameOddsGenerationService = gameOddsGenerationService;
            _wagerDetachmentService = wagerDetachmentService;
            _tournamentBracketReconciliationService = tournamentBracketReconciliationService;
        }

        public async Task<Operation<List<TournamentModel>, ApiError>> ListAllAsync() 
        {
            try
            {
                var tournaments = await _tournamentsDAO.ListAllAsync();
                return _mapper.Map<List<TournamentDAOModel>, List<TournamentModel>>(tournaments.ToList());
            }
            catch (Exception e)
            {
                return new ApiError($"error getting all tournaments: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<TournamentModel, ApiError>> AddTournamentAsync(UpdateTournamentRequestModel tournament)
        {
            try
            {
                tournament.StatusId = (int)TournamentStatus.Waiting;
                var newTournament = await _tournamentsDAO.AddTournamentAsync(tournament);
                foreach (var playerId in tournament.PlayerIds)
                {
                    await _playerTournamentDAO.CreatePlayerTournamentAsync(new PlayerTournamentDAOModel()
                    {
                        PlayerId = playerId,
                        TournamentId = newTournament.TournamentId,
                        DateAdded = DateTime.UtcNow,
                        DateModified = DateTime.UtcNow,
                    });
                }
                return _mapper.Map<TournamentModel>(newTournament);
            }
            catch (Exception e)
            {
                return new ApiError($"error getting all tournaments: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Returns the current playable tournament: not Deleted or Completed, status Waiting / Preliminaries / Tournament.
        /// If several match (data issue), prefers latest <see cref="TournamentDAOModel.StartDate"/> then highest <see cref="TournamentDAOModel.TournamentId"/>.
        /// </summary>
        public async Task<Operation<TournamentModel, ApiError>> GetActive()
        {
            try
            {
                var all = await _tournamentsDAO.ListAllAsync();
                var active = all
                    .Where(t =>
                        t.StatusId != (int)TournamentStatus.Deleted
                        && t.StatusId != (int)TournamentStatus.Completed
                        && (t.StatusId == (int)TournamentStatus.Waiting
                            || t.StatusId == (int)TournamentStatus.Preliminaries
                            || t.StatusId == (int)TournamentStatus.Tournament))
                    .OrderByDescending(t => t.StartDate ?? DateTime.MinValue)
                    .ThenByDescending(t => t.TournamentId)
                    .FirstOrDefault();
                if (active == null)
                    return new ApiError("no in progress tournament found", HttpStatusCode.BadRequest);

                return _mapper.Map<TournamentModel>(active);
            }
            catch (Exception e)
            {
                return new ApiError($"error getting getting active tournament {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<TournamentModel, ApiError>> GetById(int tournamentId)
        {
            try
            {
                var tournament = await _tournamentsDAO.GetById(tournamentId);
                return _mapper.Map<TournamentModel>(tournament);
            }
            catch (Exception e)
            {
                return new ApiError($"error getting getting tournament {tournamentId}: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<TournamentModel, ApiError>> UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament)
        {
            try
            {
                var existingTournament = await _tournamentsDAO.GetById(tournamentId);
                if(existingTournament == null)
                    return new ApiError($"no tournament with id {tournamentId} found", HttpStatusCode.BadRequest);

                var existingPlayers = await _playerTournamentDAO.GetByTournamentIdAsync(tournamentId);
                foreach (var player in existingPlayers)
                {
                    if (!tournament.PlayerIds.Contains(player.PlayerId))
                        await _playerTournamentDAO.DeleteByPlayerAndTournamentIdAsync(player.PlayerId, tournamentId);
                }

                foreach (var playerId in tournament.PlayerIds)
                {
                    if (!existingPlayers.Any(ep => ep.PlayerId == playerId))
                        await _playerTournamentDAO.CreatePlayerTournamentAsync(new PlayerTournamentDAOModel()
                        {
                            PlayerId = playerId,
                            TournamentId = tournamentId,
                            DateAdded = DateTime.UtcNow,
                            DateModified = DateTime.UtcNow,
                        });
                }

                //these cannot be updated through this method
                tournament.StatusId = existingTournament.StatusId;
                if(tournament.StartDate != existingTournament.StartDate)
                    tournament.StartDate = existingTournament.StartDate;
                if(tournament.EndDate != existingTournament.EndDate)
                    tournament.EndDate = existingTournament.EndDate;

                var updatedTournament = await _tournamentsDAO.UpdateTournamentAsync(tournamentId, tournament);
                return _mapper.Map<TournamentModel>(updatedTournament);
            }
            catch (Exception e)
            {
                return new ApiError($"error updating tournament: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }
        public async Task<Operation<TournamentModel, ApiError>> UpdateBracketDataAsync(int tournamentId, string bracketData)
        {
            try
            {
                var temp = generateMissingPointSpeads(bracketData);
                var existingTournament = await _tournamentsDAO.GetById(tournamentId);
                if (existingTournament == null)
                    return new ApiError($"no tournament with id {tournamentId} found", HttpStatusCode.BadRequest);

                await _tournamentsDAO.UpdateTournamentBracketDataAsync(tournamentId, bracketData);

                var savedTournament = await _tournamentsDAO.GetById(tournamentId);
                return _mapper.Map<TournamentModel>(savedTournament);
            }
            catch (Exception e)
            {
                return new ApiError($"error updating tournament data: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<bool, ApiError>> DeleteTournamentAsync(int tournamentId)
        {
            try
            {
                await _tournamentsDAO.DeleteTournamentAsync(tournamentId);
                return true;
            }
            catch (Exception e)
            {
                return new ApiError($"error deleting tournament: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<ChangeTournamentStatusResponseModel, ApiError>> ChangeStatusAsync(ChangeTournamentStatusRequest request)
        {
            try
            {
                var tournament = await _tournamentsDAO.GetById(request.TournamentId);
                if (tournament == null)
                    return new ApiError($"could not find tournament with id {request.TournamentId}", HttpStatusCode.BadRequest);

                if (request.Status == TournamentStatus.Preliminaries)
                {
                    return await startPrelims(tournament);
                }
                else if(request.Status == TournamentStatus.Tournament)
                {
                    return await startTournament(request, tournament);
                }

                throw new NotImplementedException();
            }
            catch (Exception e)
            {
                return new ApiError($"error chaing tournament status: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        private async Task<Operation<ChangeTournamentStatusResponseModel, ApiError>> startTournament(ChangeTournamentStatusRequest request, TournamentDAOModel tournament)
        {
            try
            {
                var tournamentId = tournament.TournamentId;

                var standingsOp = await GetStandingsAsync(tournamentId, TournamentStatus.Preliminaries);
                if (!standingsOp.IsSuccess)
                    return standingsOp.Failure!;
                var standingsList = standingsOp.Data ?? [];
                var n = standingsList.Count;
                if (n < 4 || n > 32)
                    return new ApiError("Tournament must have between 4 and 32 players (after prelims) to start the bracket.", HttpStatusCode.BadRequest);

                await ClearBracketPhaseAsync(tournamentId);

                var orderedBySeed = standingsList.OrderBy(s => s.Seed).ToList();
                var matchupRanks = DoubleEliminationBracket.GetFirstRoundWinnersBracketMatchupRanks(n);
                var savedGames = new List<GameResultDAOModel>();
                var baseTime = DateTime.UtcNow;
                var ordinal = 0;
                foreach (var (rankA, rankB) in matchupRanks)
                {
                    var sa = orderedBySeed[rankA];
                    var sb = orderedBySeed[rankB];
                    ordinal++;
                    var game = new GameResultDAOModel
                    {
                        TournamentId = tournamentId,
                        Player1Id = sa.PlayerId,
                        Player2Id = sb.PlayerId,
                        StatusId = (int)GameStatus.Waiting,
                        GameTypeId = (int)GameType.Tournament,
                        IsDeleted = false,
                        DateAdded = baseTime.AddTicks(ordinal * 10L),
                    };
                    savedGames.Add(await _gameResultDAO.CreateGameResultAsync(game));
                }

                var oddsStatus = await _gameOddsGenerationService.EnsureOddsForNewGameResultsAsync(savedGames);
                await _tournamentsDAO.UpdateTournamentStatusAsync(tournamentId, (int)TournamentStatus.Tournament);

                var reconcileOp = await _tournamentBracketReconciliationService.ReconcileAsync(tournamentId, standingsList);
                if (!reconcileOp.IsSuccess)
                    return reconcileOp.Failure!;
                var recOdds = reconcileOp.Data!.OddsGeneration;
                var mergedOdds = new OddsGenerationStatusModel
                {
                    Attempted = oddsStatus.Attempted || recOdds.Attempted,
                    Success = (!oddsStatus.Attempted || oddsStatus.Success) && (!recOdds.Attempted || recOdds.Success),
                    Message = MergeBracketStartOddsMessages(oddsStatus, recOdds),
                };

                var savedTournament = await _tournamentsDAO.GetById(tournamentId);
                return new ChangeTournamentStatusResponseModel
                {
                    Tournament = _mapper.Map<TournamentModel>(savedTournament),
                    OddsGeneration = mergedOdds,
                };
            }
            catch (Exception e)
            {
                return new ApiError($"error updating tournament to running: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        private static string? MergeBracketStartOddsMessages(OddsGenerationStatusModel first, OddsGenerationStatusModel second)
        {
            if ((!first.Attempted || first.Success) && (!second.Attempted || second.Success))
                return null;
            var parts = new List<string>();
            if (first.Attempted && !first.Success && !string.IsNullOrWhiteSpace(first.Message))
                parts.Add(first.Message);
            if (second.Attempted && !second.Success && !string.IsNullOrWhiteSpace(second.Message))
                parts.Add(second.Message);
            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        /// <summary>
        /// Detach wagers (pending refunded, settled kept with GameResultId cleared), delete odds for bracket games, soft-delete those game rows, clear legacy bracket JSON, set Preliminaries.
        /// </summary>
        private async Task ClearBracketPhaseAsync(int tournamentId)
        {
            var bracketGames = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId, false))
                .Where(g => g.GameTypeId == (int)GameType.Tournament)
                .ToList();

            if (bracketGames.Count > 0)
            {
                var ids = bracketGames.Select(g => g.GameResultId).ToList();
                await _wagerDetachmentService.DetachWagersForGameResultsAsync(ids, actorPlayerId: null);
                await _gameOddsDAO.DeleteByGameResultIdsAsync(ids);
                foreach (var game in bracketGames)
                {
                    game.IsDeleted = true;
                    await _gameResultDAO.UpdateGameResultAsync(game.GameResultId, game);
                }
            }

            await _tournamentsDAO.UpdateTournamentStatusAsync(tournamentId, (int)TournamentStatus.Preliminaries);
            await _tournamentsDAO.UpdateTournamentBracketDataAsync(tournamentId, string.Empty);
        }

        public async Task<Operation<bool, ApiError>> ResetTournamentPhaseAsync(int tournamentId, ResetTournamentRequestModel request)
        {
            try
            {
                if (tournamentId != request.TournamentId)
                    return new ApiError("tournament ids in request do not match", HttpStatusCode.BadRequest);

                var tournament = await _tournamentsDAO.GetById(tournamentId);
                if (tournament == null)
                    return new ApiError($"no tournament found with id {tournamentId}", HttpStatusCode.BadRequest);

                await ClearBracketPhaseAsync(tournamentId);
                return true;
            }
            catch (Exception e)
            {
                return new ApiError($"error resetting tournament phase: {e.Message}", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// returns the standings of the tournament for the preliminary games
        /// </summary>
        /// <param name="tournamentId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<Operation<List<TournamentStandingModel>, ApiError>> GetStandingsAsync(int tournamentId, TournamentStatus status)
        {
            try
            {
                List<TournamentStandingModel> standings = new List<TournamentStandingModel>();
                var players = await _playerDAO.ListPlayersAsync(tournamentId);
                var prelimGames = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId)).Where(g => g.GameTypeId == (int)GameType.Preliminary).ToList();
                var seed = 1;

                foreach (var player in players) 
                {
                    // Exclude games that don't count for this player's seeding (e.g. their 3rd game when odd N)
                    var gamesThatCountForPlayer = prelimGames.Where(g => GameUtils.PlayerInGame(g, player.PlayerId) != null && g.SeedingExemptPlayerId != player.PlayerId).ToList();
                    standings.Add(new TournamentStandingModel() { 
                        GamesPlayed = gamesThatCountForPlayer.Count(g => g.StatusId == (int)status),
                        PlayerId = player.PlayerId,
                        PlayerName = player.FullName,
                        PreliminariesScore = calculatePlayerPreliminaryScore(gamesThatCountForPlayer, player.PlayerId),
                        TournamentFinishPosition = 0,
                        TotalPassingYards = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.PassingYards),
                        TotalRushingYards = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.RushingYards),
                        TotalPassingYardsAllowed = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.PassingYardsAllowed),
                        TotalRushingYardsAllowed = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.RushingYardsAllowed),
                        TotalPointsFor = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.PointsScoreFor),
                        TotalPointsAgainst = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.PointsScoreAgainst),
                        Wins = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.Wins),
                        Loses = GameUtils.GetPlayerStat(gamesThatCountForPlayer, player.PlayerId, GameStat.Losses),
                        TournamentId = tournamentId,
                        Seed = 0
                    });
                }

                List<PrelimTieBreaker> tiebreakers = Enum.GetValues(typeof(PrelimTieBreaker))
                                         .Cast<PrelimTieBreaker>()
                                         .ToList();

                standings.Sort(new TournamentStandingComparer(tiebreakers));

                int position = 1;
                for (int i = 0; i < standings.Count; i++)
                {
                    var currentStanding = standings[i];
                    currentStanding.PreliminaryPosition = position;
                    position++;

                    if (i > 0)
                    {
                        var previousStanding = standings[i - 1];
                        PrelimTieBreaker? tieBreakerUsed = null;

                        if (standings[i].GamesPlayed > 0 && currentStanding.PreliminariesScore == previousStanding.PreliminariesScore)
                        {
                            tieBreakerUsed = getTieBreakerUsed(previousStanding, currentStanding, tiebreakers);
                            previousStanding.PreliminariesTieBreakerUsed = tieBreakerUsed;
                        }
                    }
                }
                
                for (int i = 1; i <= standings.Count; i++)
                {
                    standings[i-1].Seed = i;
                }
                return standings.ToList();
            }
            catch (Exception e)
            {
                return new ApiError($"error getting tournament standings: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        private int calculatePlayerPreliminaryScore(IEnumerable<GameResultDAOModel> games, int playerId)
        {
            int score = 0;
            int totalPointsFor = 0;
            int totalPointsAgainst = 0;
            foreach (var game in games)
            {
                var pointsFor = GameUtils.GetPlayerStat(game, playerId, GameStat.PointsScoreFor);
                var pointsAgainst = GameUtils.GetPlayerStat(game, playerId, GameStat.PointsScoreAgainst);

                totalPointsFor += pointsFor;
                totalPointsAgainst += pointsAgainst;
                if (pointsFor > pointsAgainst)
                {
                    score += POINTS_FOR_WIN;
                }
            }
            score += (totalPointsFor - totalPointsAgainst);
            return score;
        }

        private PrelimTieBreaker? getTieBreakerUsed(TournamentStandingModel higherStanding, TournamentStandingModel lowerStanding, List<PrelimTieBreaker> tiebreakers)
        {
            foreach (var tieBreaker in tiebreakers)
            {
                int result = 0;
                switch (tieBreaker)
                {
                    case PrelimTieBreaker.PointsScored:
                        result = higherStanding.TotalPointsFor.CompareTo(lowerStanding.TotalPointsFor);
                        if (result != 0)
                            return tieBreaker;
                        break;
                    case PrelimTieBreaker.PointsAllowed:
                        result = lowerStanding.TotalPointsAgainst.CompareTo(higherStanding.TotalPointsAgainst); // Fewer is better
                        if (result != 0)
                            return tieBreaker;
                        break;
                    case PrelimTieBreaker.PassingYards:
                        result = higherStanding.TotalPassingYards.CompareTo(lowerStanding.TotalPassingYards);
                        if (result != 0)
                            return tieBreaker;
                        break;
                    case PrelimTieBreaker.RushingYards:
                        result = higherStanding.TotalRushingYards.CompareTo(lowerStanding.TotalRushingYards);
                        if (result != 0)
                            return tieBreaker;
                        break;
                    case PrelimTieBreaker.PassingYardsAllowed:
                        result = lowerStanding.TotalPassingYardsAllowed.CompareTo(higherStanding.TotalPassingYardsAllowed); // Fewer is better
                        if (result != 0)
                            return tieBreaker;
                        break;
                    case PrelimTieBreaker.RushingYardsAllowed:
                        result = lowerStanding.TotalRushingYardsAllowed.CompareTo(higherStanding.TotalRushingYardsAllowed); // Fewer is better
                        if (result != 0)
                            return tieBreaker;
                        break;
                    case PrelimTieBreaker.CoinFlip:
                        return PrelimTieBreaker.CoinFlip;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(tieBreaker), "Unknown tie-breaker");
                }
            }

            return null; // No tie-breaker could break the tie
        }

        private int getGameCount(int playerId, List<GameResultDAOModel> games)
        {
            return games.Count(g => g.Player2Id == playerId || g.Player1Id == playerId);
        }
        private int? getOpponent(int playerId, List<GameResultDAOModel> games, List<PlayerTournamentDAOModel> shuffledPlayers)
        {
            int? opponentId = null;
            foreach (var player in shuffledPlayers)
            {
                if(getGameCount(player.PlayerId, games) >= PRELIM_GAMES) //if this player already has two games
                    continue;

                if(games.Any(g => GameUtils.PlayerInGame(g, playerId).HasValue && GameUtils.PlayerInGame(g, player.PlayerId).HasValue))//if these two players already have a game together
                    continue;

                opponentId = player.PlayerId;//this player does not have too many games and there is no game scheduled between these two
                break;
            }

            return opponentId;
        }

        /// <summary>
        /// generates prelims games and assigns them to the players in the tournament
        /// </summary>
        /// <param name="tournament"></param>
        /// <returns></returns>
        private async Task<Operation<ChangeTournamentStatusResponseModel, ApiError>> startPrelims(TournamentDAOModel tournament)
        {
            try
            {
                await _tournamentsDAO.UpdateTournamentStatusAsync(tournament.TournamentId, (int)TournamentStatus.Preliminaries);

                List<GameResultDAOModel> games = new List<GameResultDAOModel>();
                var allPlayers = await _playerTournamentDAO.GetByTournamentIdAsync(tournament.TournamentId);

                foreach (var player in allPlayers)
                {
                    for (int i = 0; i < PRELIM_GAMES; i++)
                    {
                        if (getGameCount(player.PlayerId, games) >= PRELIM_GAMES) //skip when this player already has two games
                            continue;

                        var shuffledPlayerList = allPlayers //create random ordering of players without the current player
                                                    .Where(p => p.PlayerId != player.PlayerId)
                                                    .OrderBy(x => Guid.NewGuid()).ToList();
                        var opponentId = getOpponent(player.PlayerId, games, shuffledPlayerList);
                        var isSeedingExemptGame = opponentId == null; // odd N: opponent already has 2 games; this game won't count for their seeding
                        if (opponentId == null) // odd N — give current player an opponent whose 3rd game this is; prefer someone we haven't already played
                        {
                            var opponentNotYetPlayed = shuffledPlayerList
                                .FirstOrDefault(p => !games.Any(g => GameUtils.PlayerInGame(g, player.PlayerId).HasValue && GameUtils.PlayerInGame(g, p.PlayerId).HasValue));
                            opponentId = opponentNotYetPlayed?.PlayerId ?? shuffledPlayerList.First().PlayerId;
                        }

                        games.Add(new GameResultDAOModel()
                        {
                            TournamentId = tournament.TournamentId,
                            Player1Id = player.PlayerId,
                            Player2Id = opponentId.Value,
                            StatusId = (int)GameStatus.Waiting,
                            GameTypeId = (int)GameType.Preliminary,
                            IsDeleted = false,
                            SeedingExemptPlayerId = isSeedingExemptGame ? opponentId : null,
                        });
                    }
                }

                var savedGames = new List<GameResultDAOModel>();
                foreach (var game in games)
                {
                    savedGames.Add(await _gameResultDAO.CreateGameResultAsync(game));
                }

                var oddsStatus = await _gameOddsGenerationService.EnsureOddsForNewGameResultsAsync(savedGames);

                var savedTournament = await _tournamentsDAO.GetById(tournament.TournamentId);
                return new ChangeTournamentStatusResponseModel
                {
                    Tournament = _mapper.Map<TournamentModel>(savedTournament),
                    OddsGeneration = oddsStatus
                };
            }
            catch (Exception e)
            {
                return new ApiError($"error setting up tournament preliminaries: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }

        private IEnumerable<BracketMatchup> generateMissingPointSpeads(string rawBracketData)
        {
            List<BracketMatchup> pointSpreads = new List<BracketMatchup>();
            var bracketData = JsonConvert.DeserializeObject<TournamentBracketModel>(rawBracketData);
            return pointSpreads;
        }

        public async Task<Operation<bool, ApiError>> Reset(int tournamentId, ResetTournamentRequestModel request)
        {
            try
            {
                if (tournamentId != request.TournamentId)
                    return new ApiError("tournament ids in request do not match", HttpStatusCode.BadRequest);

                var tournament = await _tournamentsDAO.GetById(tournamentId);
                if (tournament == null)
                    return new ApiError($"no tournament found with id {tournamentId}", HttpStatusCode.BadRequest);

                await _tournamentsDAO.UpdateTournamentStatusAsync(tournamentId, (int)TournamentStatus.Waiting);
                await _tournamentsDAO.UpdateTournamentBracketDataAsync(tournamentId, string.Empty);

                var games = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId)).ToList();
                var gameIds = games.Select(g => g.GameResultId).ToList();
                await _wagerDetachmentService.DetachWagersForGameResultsAsync(gameIds, actorPlayerId: null);

                foreach (var game in games)
                {
                    game.IsDeleted = true;
                    await _gameResultDAO.UpdateGameResultAsync(game.GameResultId, game);
                }

                await _gameOddsDAO.DeleteByTournamentIdAsync(tournamentId);
                return true;
            }
            catch (Exception e)
            {
                return new ApiError( $"error resetting tournament: {e.Message}", HttpStatusCode.InternalServerError);;
            }
        }

        public async Task<Operation<RecalculateBracketResponseModel, ApiError>> RecalculateBracketAsync(int tournamentId)
        {
            var standingsOp = await GetStandingsAsync(tournamentId, TournamentStatus.Preliminaries);
            if (!standingsOp.IsSuccess)
                return standingsOp.Failure!;
            return await _tournamentBracketReconciliationService.ReconcileAsync(tournamentId, standingsOp.Data!);
        }
    }

    public class BracketMatchup
    {
        public int Player1ID { get; set; }
        public int Player2ID { get; set; }
        public int TournamentId { get; set; }
        public int BracketLocation { get; set; }
        public bool InProgress { get; set; }
    }
}
