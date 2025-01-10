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

        public TournamentsOrchestration(ITournamentsDAO tournamentsDAO, IPlayerTournamentDAO playerTournamentDAO, IGameResultDAO gameResultDAO, IPlayerDAO playerDAO, IMapper mapper)
        {
            _gameResultDAO = gameResultDAO;
            _playerTournamentDAO = playerTournamentDAO;
            _tournamentsDAO = tournamentsDAO;
            _playerDAO = playerDAO;
            _mapper = mapper;
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
                        await _playerTournamentDAO.DeleteByPlayerAndTournamentIdAsync(tournamentId, player.PlayerId);
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

        public async Task<Operation<TournamentModel, ApiError>> ChangeStatusAsync(ChangeTournamentStatusRequest request)
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

        private async Task<Operation<TournamentModel, ApiError>> startTournament(ChangeTournamentStatusRequest request, TournamentDAOModel tournament)
        {
            try
            {
                var teams = await _playerTournamentDAO.GetByTournamentIdAsync(tournament.TournamentId);
                await _tournamentsDAO.UpdateTournamentStatusAsync(tournament.TournamentId, (int)TournamentStatus.Tournament);
                TournamentBracketModel bracketModel = new TournamentBracketModel();
                bracketModel.PopulateBracket(teams.Count());
                var standings = (await GetStandingsAsync(tournament.TournamentId, TournamentStatus.Preliminaries)).Data!;

                foreach (var bracketMatchups in bracketModel.Teams)
                {
                    var team1 = bracketMatchups[0]!;
                    var standing = standings.First(s => s.Seed == team1.Seed);
                    team1.PlayerId = standing.PlayerId;
                    team1.Player = standing.PlayerName;

                    if (bracketMatchups.Count > 1)
                    {
                        var team2 = bracketMatchups[1]!;
                        standing = standings.First(s => s.Seed == team2.Seed);
                        team2.PlayerId = standing.PlayerId;
                        team2.Player = standing.PlayerName;
                        var newGame = new GameResultDAOModel() { 
                                        GameTypeId = (int)GameType.Tournament,
                                        Player1Id = team1.PlayerId,
                                        Player2Id = team2.PlayerId,
                                        StatusId = (int)GameStatus.Waiting,
                                        TournamentId = tournament.TournamentId
                                    };
                        newGame = await _gameResultDAO.CreateGameResultAsync(newGame);
                        team2.GameId = team1.GameId = newGame.GameResultId;
                    }
                }
                
                await _tournamentsDAO.UpdateTournamentBracketDataAsync(tournament.TournamentId, JsonConvert.SerializeObject(bracketModel));
                var savedTournament = await _tournamentsDAO.GetById(tournament.TournamentId);

                return _mapper.Map<TournamentModel>(savedTournament);
            }
            catch (Exception e)
            {
                return new ApiError($"error updating tournament to running: {e.ToString()}", HttpStatusCode.InternalServerError);
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
                var prelimGames = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId)).Where(g => g.GameTypeId == (int)GameType.Preliminary);
                var seed = 1;

                foreach (var player in players) 
                {
                    standings.Add(new TournamentStandingModel() { 
                        GamesPlayed = prelimGames.Count(g => GameUtils.PlayerInGame(g, player.PlayerId) != null && g.StatusId == (int)status),
                        PlayerId = player.PlayerId,
                        PlayerName = player.FullName,
                        PreliminariesScore = calculatePlayerPreliminaryScore(prelimGames, player.PlayerId),
                        TournamentFinishPosition = 0,
                        TotalPassingYards = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.PassingYards),
                        TotalRushingYards = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.RushingYards),
                        TotalPassingYardsAllowed = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.PassingYardsAllowed),
                        TotalRushingYardsAllowed = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.RushingYardsAllowed),
                        TotalPointsFor = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.PointsScoreFor),
                        TotalPointsAgainst = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.PointsScoreAgainst),
                        Wins = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.Wins),
                        Loses = GameUtils.GetPlayerStat(prelimGames, player.PlayerId, GameStat.Losses),
                        TournamentId = tournamentId,
                        Seed = seed++
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
        private async Task<Operation<TournamentModel, ApiError>> startPrelims(TournamentDAOModel tournament)
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
                        if (getGameCount(player.PlayerId, games) > 2) //check if this player is already in two games
                            continue;

                        var shuffledPlayerList = allPlayers //create random ordering of players without the current player
                                                    .Where(p => p.PlayerId != player.PlayerId)
                                                    .OrderBy(x => Guid.NewGuid()).ToList();
                        var opponentId = getOpponent(player.PlayerId, games, shuffledPlayerList);
                        if (opponentId == null) //there are no players left
                            opponentId = shuffledPlayerList.First().PlayerId;

                        games.Add(new GameResultDAOModel()
                        {
                            TournamentId = tournament.TournamentId,
                            Player1Id = player.PlayerId,
                            Player2Id = opponentId.Value,
                            StatusId = (int)GameStatus.Waiting,
                            GameTypeId = (int)GameType.Preliminary,
                            IsDeleted = false,
                        });
                    }
                }

                foreach (var game in games)
                {
                    await _gameResultDAO.CreateGameResultAsync(game);
                }

                var savedTournament = await _tournamentsDAO.GetById(tournament.TournamentId);
                return _mapper.Map<TournamentModel>(savedTournament);
            }
            catch (Exception e)
            {
                return new ApiError($"error setting up tournament preliminaries: {e.ToString()}", HttpStatusCode.InternalServerError);
            }
        }
    }
}
