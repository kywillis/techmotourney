// TecmoTourney, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// TecmoTourney.Orchestration.GameResultOrchestration
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;
using System.Text.Json;

namespace TecmoTourney.Orchestration
{
public class GameResultOrchestration : IGameResultOrchestration
{
    private const int _winnersGroup = 0;

    private const int _losersGroup = 1;

    private const string _win = "win";

    private const string _loss = "loss";

    private readonly IGameResultDAO _gameResultDAO;

    private readonly ITournamentsDAO _tournamentsDAO;

    private readonly ITournamentBracketUpdateDAO _tournamentBracketUpdateDAO;

    private readonly IPlayerDAO _playerDAO;

    private readonly IGameTeamDAO _gameTeamDAO;

    private readonly IGameOddsDAO _gameOddsDAO;

    private readonly IGameOddsGenerationService _gameOddsGenerationService;

    private readonly IWagerDetachmentService _wagerDetachmentService;

    private readonly ITournamentsOrchestration _tournamentsOrchestration;

    private readonly ITournamentBracketReconciliationService _tournamentBracketReconciliationService;

    private readonly IMapper _mapper;

    private readonly IGameResultSaveAuditDAO _gameResultSaveAuditDAO;

    private readonly IWagerSettlementService _wagerSettlementService;

    private static IEnumerable<GameTeamDAOModel> _gameTeams = new List<GameTeamDAOModel>();

    public GameResultOrchestration(
        IGameResultDAO gameResultDAO,
        ITournamentsDAO tournamentsDAO,
        IPlayerDAO playerDAO,
        IGameTeamDAO gameTeamDAO,
        IMapper mapper,
        ITournamentBracketUpdateDAO tournamentBracketUpdateDAO,
        IGameOddsDAO gameOddsDAO,
        IGameOddsGenerationService gameOddsGenerationService,
        IWagerDetachmentService wagerDetachmentService,
        ITournamentsOrchestration tournamentsOrchestration,
        ITournamentBracketReconciliationService tournamentBracketReconciliationService,
        IGameResultSaveAuditDAO gameResultSaveAuditDAO,
        IWagerSettlementService wagerSettlementService)
    {
        _tournamentsDAO = tournamentsDAO;
        _gameTeamDAO = gameTeamDAO;
        _gameResultDAO = gameResultDAO;
        _playerDAO = playerDAO;
        _mapper = mapper;
        _tournamentBracketUpdateDAO = tournamentBracketUpdateDAO;
        _gameOddsDAO = gameOddsDAO;
        _gameOddsGenerationService = gameOddsGenerationService;
        _wagerDetachmentService = wagerDetachmentService;
        _tournamentsOrchestration = tournamentsOrchestration;
        _tournamentBracketReconciliationService = tournamentBracketReconciliationService;
        _gameResultSaveAuditDAO = gameResultSaveAuditDAO;
        _wagerSettlementService = wagerSettlementService;
    }

    public async Task<Operation<bool, ApiError>> AcknowledgeBracketUpdate(int tournamentBracketUpdateId)
    {
        try
        {
            TournamentBracketUpdateDAOModel update = await _tournamentBracketUpdateDAO.GetByUpdateIdAsync(tournamentBracketUpdateId);
            if (update == null)
            {
                return new ApiError($"tournamentBracketUpdateId: {tournamentBracketUpdateId} not found", HttpStatusCode.BadRequest);
            }
            update.StatusID = 2;
            await _tournamentBracketUpdateDAO.Save(update);
            return true;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<bool, ApiError>> DeleteGameResultAsync(int id)
    {
        try
        {
            await _wagerDetachmentService.DetachWagersForGameResultAsync(id, actorPlayerId: null);
            await _gameOddsDAO.DeleteByGameResultIdAsync(id);
            await _gameResultDAO.DeleteGameResultAsync(id);
            return true;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<GameResultModel, ApiError>> GetById(int gameResultId)
    {
        GameResultDAOModel result = await _gameResultDAO.GetGameResultAsync(gameResultId);
        if (result == null)
        {
            return new ApiError("no game result found", HttpStatusCode.NotFound);
        }
        GameResultModel game = _mapper.Map<GameResultModel>(result);
        await populatePlayerNames(game);
        await populateTeamNames(game);
        return game;
    }

    public async Task<Operation<List<TournamentBracketUpdateModel>, ApiError>> GetGameUpdates(int tournamentId)
    {
        try
        {
            List<TournamentBracketUpdateModel> updatedGames = new List<TournamentBracketUpdateModel>();
            List<GameResultModel> gameResults = new List<GameResultModel>();
            IEnumerable<TournamentBracketUpdateDAOModel> updates = await _tournamentBracketUpdateDAO.GetByTournamentIdAsync(tournamentId, 1);
            if (updates.Any())
            {
                IEnumerable<GameResultDAOModel> allGameResults = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId)).Where((GameResultDAOModel g) => g.GameTypeId == 1);
                foreach (TournamentBracketUpdateDAOModel update in updates)
                {
                    IMapper mapper = _mapper;
                    GameResultModel gameResult = mapper.Map<GameResultModel>(await _gameResultDAO.GetGameResultAsync(update.GameResultId));
                    if (gameResult == null)
                    {
                        update.StatusID = 2;
                        await _tournamentBracketUpdateDAO.Save(update);
                        return new ApiError("found update with no matching game result", HttpStatusCode.BadRequest);
                    }
                    List<GameResultDAOModel> matchUps = (from g in allGameResults
                                                         where (g.Player1Id == gameResult.Player1.PlayerId && g.Player2Id == gameResult.Player2.PlayerId) || (g.Player1Id == gameResult.Player2.PlayerId && g.Player2Id == gameResult.Player1.PlayerId)
                                                         orderby g.GameResultId
                                                         select g).ToList();
                    gameResult.MatchUpIndex = matchUps.ToList().FindIndex((GameResultDAOModel g) => g.GameResultId == update.GameResultId);
                    gameResults.Add(gameResult);
                    updatedGames.Add(new TournamentBracketUpdateModel
                    {
                        GameResult = gameResult,
                        TournamentBracketUpdateId = update.TournamentBracketUpdateId
                    });
                }
                await populatePlayerNames(gameResults);
            }
            return updatedGames;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByPlayerAsync(int playerId)
    {
        try
        {
            IEnumerable<GameResultDAOModel> gameResultDAOModels = await _gameResultDAO.ListResultsByPlayerAsync(playerId);
            List<GameResultModel> games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
            await populatePlayerNames(games);
            await populateTeamNames(games);
            return games;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByTournamentAsync(int tournamentId, bool includeDeledted = false)
    {
        try
        {
            IEnumerable<GameResultDAOModel> gameResultDAOModels = await _gameResultDAO.ListResultsByTournamentAsync(tournamentId, includeDeledted);
            List<GameResultModel> games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
            await populatePlayerNames(games);
            await populateTeamNames(games);
            return games;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByTournamentAsync(int tournamentId, int playerId)
    {
        try
        {
            IEnumerable<GameResultDAOModel> gameResultDAOModels = await _gameResultDAO.ListResultsByTournamentAsync(tournamentId);
            List<GameResultModel> games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels.Where((GameResultDAOModel g) => g.Player1Id == playerId || g.Player2Id == playerId));
            await populatePlayerNames(games);
            await populateTeamNames(games);
            return games;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByBracketGameIDsAsync(IEnumerable<int> bracketGameIds)
    {
        try
        {
            IEnumerable<GameResultDAOModel> gameResultDAOModels = await _gameResultDAO.ListResultsByBracketGameIDsAsync(bracketGameIds);
            List<GameResultModel> games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
            await populatePlayerNames(games);
            await populateTeamNames(games);
            return games;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<SaveGameResultResponseModel, ApiError>> SaveGameResultAsync(SaveGameResultRequestModel gameResult)
    {
        try
        {
            await ApplyTieStatAccumulationIfRequestedAsync(gameResult);

            List<string> errors = await ValidateGameResultAsync(null, gameResult);
            if (errors.Any())
            {
                return new ApiError(string.Join("; ", errors), HttpStatusCode.BadRequest);
            }
            GameResultDAOModel gameResultDAOModel = _mapper.Map<GameResultDAOModel>(gameResult);
            GameResultDAOModel savedGameResultDAOModel;
            var isNewGame = !gameResult.GameResultId.HasValue || gameResult.GameResultId < 1;
            if (isNewGame)
            {
                savedGameResultDAOModel = await _gameResultDAO.CreateGameResultAsync(gameResultDAOModel);
            }
            else
            {
                await _gameResultDAO.UpdateGameResultAsync(gameResultDAOModel.GameResultId, gameResultDAOModel);
                savedGameResultDAOModel = await _gameResultDAO.GetGameResultAsync(gameResultDAOModel.GameResultId);
            }
            //await _tournamentBracketUpdateDAO.Save(new TournamentBracketUpdateDAOModel
            //{
            //    GameResultId = savedGameResultDAOModel.GameResultId,
            //    StatusID = 1,
            //    TournamentId = savedGameResultDAOModel.TournamentId
            //});
            GameResultModel game = _mapper.Map<GameResultModel>(savedGameResultDAOModel);
            if (gameResult.GameType == GameType.Tournament)
            {
                await updateTournament(game);
            }
            await populatePlayerNames(game);
            await populateTeamNames(game);

            OddsGenerationStatusModel oddsStatus;
            if (isNewGame)
            {
                oddsStatus = await _gameOddsGenerationService.EnsureOddsForNewGameResultsAsync(
                    new List<GameResultDAOModel> { savedGameResultDAOModel });
            }
            else
            {
                oddsStatus = new OddsGenerationStatusModel { Attempted = false, Success = true };
            }

            RecalculateBracketResponseModel? bracketRec = null;
            if (gameResult.GameType == GameType.Tournament &&
                savedGameResultDAOModel.StatusId == (int)GameStatus.Completed)
            {
                var standingsOp = await _tournamentsOrchestration.GetStandingsAsync(
                    savedGameResultDAOModel.TournamentId,
                    TournamentStatus.Preliminaries);
                if (standingsOp.IsSuccess && standingsOp.Data != null)
                {
                    var recOp = await _tournamentBracketReconciliationService.ReconcileAsync(
                        savedGameResultDAOModel.TournamentId,
                        standingsOp.Data);
                    if (recOp.IsSuccess && recOp.Data != null)
                        bracketRec = recOp.Data;
                }
            }

            if (bracketRec?.OddsGeneration != null && bracketRec.OddsGeneration.Attempted)
                oddsStatus = bracketRec.OddsGeneration;

            await _wagerSettlementService.SettleWagersAfterGameSaveAsync(savedGameResultDAOModel);

            await TryInsertSaveAuditAsync(gameResult, savedGameResultDAOModel.GameResultId);

            return new SaveGameResultResponseModel
            {
                GameResult = game,
                OddsGeneration = oddsStatus,
                BracketReconciliation = bracketRec
            };
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    private async Task ApplyTieStatAccumulationIfRequestedAsync(SaveGameResultRequestModel gameResult)
    {
        if (!gameResult.AccumulateStatsFromTieLeg || !gameResult.GameResultId.HasValue || gameResult.GameResultId.Value < 1)
            return;

        var existing = await _gameResultDAO.GetGameResultAsync(gameResult.GameResultId.Value);
        if (existing == null)
            return;

        gameResult.Player1.PassingYards += existing.Player1PassingYards;
        gameResult.Player2.PassingYards += existing.Player2PassingYards;
        gameResult.Player1.RushingYards += existing.Player1RushingYards;
        gameResult.Player2.RushingYards += existing.Player2RushingYards;
    }

    private static string? Truncate(string? value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var t = value.Trim();
        return t.Length <= maxLen ? t : t.Substring(0, maxLen);
    }

    private async Task TryInsertSaveAuditAsync(SaveGameResultRequestModel gameResult, int gameResultId)
    {
        try
        {
            var isTie = gameResult.Status == GameStatus.Completed
                && gameResult.Player1.PlayerId > 0
                && gameResult.Player2.PlayerId > 0
                && gameResult.Player1.Score == gameResult.Player2.Score;

            var json = JsonSerializer.Serialize(gameResult, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await _gameResultSaveAuditDAO.InsertAsync(new GameResultSaveAuditDAOModel
            {
                GameResultId = gameResultId,
                SaveSource = Truncate(gameResult.SaveSource, 128),
                ClientCorrelationId = Truncate(gameResult.ClientCorrelationId, 64),
                IsTieGame = isTie,
                AccumulatedStats = gameResult.AccumulateStatsFromTieLeg,
                RequestJson = json.Length > 400000 ? json.Substring(0, 400000) : json,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        catch
        {
            // Audit must not block save
        }
    }

    public async Task<Operation<List<GameResultModel>, ApiError>> SearchAsync(int? tournamentId, int? player1Id, int? player2Id, BracketLocation? bracketLocation = null)
    {
        try
        {
            IEnumerable<GameResultDAOModel> gameResultDAOModels = await _gameResultDAO.SearchAsync(tournamentId, player1Id, player2Id);
            if (bracketLocation.HasValue && bracketLocation != BracketLocation.Preliminary)
            {
                gameResultDAOModels = gameResultDAOModels.OrderBy((GameResultDAOModel g) => g.GameResultId);
                gameResultDAOModels = ((bracketLocation != BracketLocation.Losers || gameResultDAOModels.Count() <= 1) ? gameResultDAOModels.Take(1) : gameResultDAOModels.Skip(1));
            }
            List<GameResultModel> games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
            await populatePlayerNames(games);
            await populateTeamNames(games);
            return games;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    private async Task updateTournament(GameResultModel gameResult)
    {
        TournamentDAOModel tournamentDAO = await _tournamentsDAO.GetById(gameResult.TournamentId);
        await _tournamentsDAO.UpdateTournamentBracketDataAsync(tournamentDAO.TournamentId, tournamentDAO.BracketData);
    }

    private async Task populatePlayerNames(GameResultModel game)
    {
        await populatePlayerNames(new List<GameResultModel> { game });
    }

    private async Task populatePlayerNames(List<GameResultModel> games)
    {
        IEnumerable<PlayerDAOModel> players = await _playerDAO.ListPlayersAsync();
        foreach (GameResultModel game in games)
        {
            PlayerDAOModel player = players.FirstOrDefault((PlayerDAOModel p) => p.PlayerId == game.Player1.PlayerId);
            if (player != null)
            {
                game.Player1.PlayerName = player.FullName;
                game.Player1.ProfilePic = player.ProfilePic >= 1 ? player.ProfilePic : 1;
            }
            player = players.FirstOrDefault((PlayerDAOModel p) => p.PlayerId == game.Player2.PlayerId);
            if (player != null)
            {
                game.Player2.PlayerName = player.FullName;
                game.Player2.ProfilePic = player.ProfilePic >= 1 ? player.ProfilePic : 1;
            }
        }
    }

    private async Task populateTeamNames(GameResultModel game)
    {
        await populateTeamNames(new List<GameResultModel> { game });
    }

    private async Task populateTeamNames(List<GameResultModel> games)
    {
        if (_gameTeams.Count() == 0)
        {
            _gameTeams = await _gameTeamDAO.GetAll();
        }
        foreach (GameResultModel game in games)
        {
            GameTeamDAOModel team1 = _gameTeams.FirstOrDefault((GameTeamDAOModel p) => p.GameTeamId == game.Player1.GameTeamId);
            GameTeamDAOModel team2 = _gameTeams.FirstOrDefault((GameTeamDAOModel p) => p.GameTeamId == game.Player2.GameTeamId);
            if (team1 != null && team2 != null)
            {
                game.Player1.TeamName = team1.TeamName;
                game.Player2.TeamName = team2.TeamName;
            }
        }
    }

    private async Task<List<string>> ValidateGameResultAsync(int? gameResultId, SaveGameResultRequestModel gameResult)
    {
        List<string> errors = new List<string>();
        if (gameResultId.HasValue && await _gameResultDAO.GetGameResultAsync(gameResultId.Value) == null)
        {
            errors.Add("Game result not found");
        }
        if (await _tournamentsDAO.GetById(gameResult.TournamentId) == null)
        {
            errors.Add("Tournament not found");
        }
        if (await _playerDAO.GetPlayerAsync(gameResult.Player1.PlayerId) == null)
        {
            errors.Add("Player 1 not found");
        }
        if (await _playerDAO.GetPlayerAsync(gameResult.Player2.PlayerId) == null)
        {
            errors.Add("Player 2 not found");
        }
        int? t1 = gameResult.Player1.GameTeamId;
        int? t2 = gameResult.Player2.GameTeamId;
        if (t1.HasValue && t2.HasValue && t1.Value == t2.Value && t1.Value > 0)
        {
            errors.Add("Each player must use a different team.");
        }
        if (gameResult.Status == GameStatus.Completed
            && gameResult.Player1.PlayerId > 0
            && gameResult.Player2.PlayerId > 0
            && gameResult.Player1.Score == gameResult.Player2.Score
            && !gameResult.AllowTieScore)
        {
            errors.Add("A completed game cannot end in a tie; scores must differ.");
        }
        return errors;
    }

    public async Task<Operation<List<GameOddsModel>, ApiError>> GetPointSpreadsAsync(int tournamentId)
    {
        try
        {
            IEnumerable<GameOddsDAOModel> gameOdds = await _gameOddsDAO.GetByTournamentIdAsync(tournamentId);
            return _mapper.Map<List<GameOddsModel>>(gameOdds);
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

}
}
