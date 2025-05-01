// TecmoTourney, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// TecmoTourney.Orchestration.GameResultOrchestration
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using OpenAI.Chat;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

public class GameResultOrchestration : IGameResultOrchestration
{
    private class PointSpreadResponse
    {
        public string Player1Name { get; set; } = string.Empty;

        public string Player2Name { get; set; } = string.Empty;

        public int Spread { get; set; }

        public string Summary { get; set; } = string.Empty;

        public string FavoredPlayerName { get; set; } = string.Empty;
    }

    private const int _winnersGroup = 0;

    private const int _losersGroup = 1;

    private const string _win = "win";

    private const string _loss = "loss";

    private readonly IGameResultDAO _gameResultDAO;

    private readonly ITournamentsDAO _tournamentsDAO;

    private readonly ITournamentBracketUpdateDAO _tournamentBracketUpdateDAO;

    private readonly IPlayerDAO _playerDAO;

    private readonly IGameTeamDAO _gameTeamDAO;

    private readonly IPointSpreadDAO _pointSpreadDAO;

    private readonly IMapper _mapper;

    private static IEnumerable<GameTeamDAOModel> _gameTeams = new List<GameTeamDAOModel>();

    private readonly IHostEnvironment _environment;

    private readonly IConfiguration _configuration;

    public GameResultOrchestration(IGameResultDAO gameResultDAO, ITournamentsDAO tournamentsDAO, IPlayerDAO playerDAO, IGameTeamDAO gameTeamDAO, IMapper mapper, ITournamentBracketUpdateDAO tournamentBracketUpdateDAO, IPointSpreadDAO pointSpreadDAO, IHostEnvironment environment, IConfiguration configuration)
    {
        _tournamentsDAO = tournamentsDAO;
        _gameTeamDAO = gameTeamDAO;
        _gameResultDAO = gameResultDAO;
        _playerDAO = playerDAO;
        _mapper = mapper;
        _tournamentBracketUpdateDAO = tournamentBracketUpdateDAO;
        _pointSpreadDAO = pointSpreadDAO;
        _environment = environment;
        _configuration = configuration;
        string key = _configuration["ApplicationConfig:gptKey"];
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

    public async Task<Operation<GameResultModel, ApiError>> SaveGameResultAsync(SaveGameResultRequestModel gameResult)
    {
        try
        {
            List<string> errors = await ValidateGameResultAsync(null, gameResult);
            if (errors.Any())
            {
                return new ApiError(string.Join("; ", errors), HttpStatusCode.BadRequest);
            }
            GameResultDAOModel gameResultDAOModel = _mapper.Map<GameResultDAOModel>(gameResult);
            GameResultDAOModel savedGameResultDAOModel;
            if (!gameResult.GameResultId.HasValue || gameResult.GameResultId < 1)
            {
                savedGameResultDAOModel = await _gameResultDAO.CreateGameResultAsync(gameResultDAOModel);
            }
            else
            {
                await _gameResultDAO.UpdateGameResultAsync(gameResultDAOModel.GameResultId, gameResultDAOModel);
                savedGameResultDAOModel = await _gameResultDAO.GetGameResultAsync(gameResultDAOModel.GameResultId);
            }
            await _tournamentBracketUpdateDAO.Save(new TournamentBracketUpdateDAOModel
            {
                GameResultId = savedGameResultDAOModel.GameResultId,
                StatusID = 1,
                TournamentId = savedGameResultDAOModel.TournamentId
            });
            GameResultModel game = _mapper.Map<GameResultModel>(savedGameResultDAOModel);
            if (gameResult.GameType == GameType.Tournament)
            {
                await updateTournament(game);
            }
            await populatePlayerNames(game);
            await populateTeamNames(game);
            return game;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<List<GameResultModel>, ApiError>> SearchAsync(int? tournamentId, int? player1Id, int? player2Id, BracketLocation? bracketLocation = null)
    {
        try
        {
            IEnumerable<GameResultDAOModel> gameResultDAOModels = await _gameResultDAO.SearchAsync(tournamentId, player1Id, player2Id);
            if (bracketLocation.HasValue)
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
            }
            player = players.FirstOrDefault((PlayerDAOModel p) => p.PlayerId == game.Player2.PlayerId);
            if (player != null)
            {
                game.Player2.PlayerName = player.FullName;
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
        return errors;
    }

    public async Task<Operation<List<PointSpreadModel>, ApiError>> CreatePointSpreadsAsync(int tournamentId, IEnumerable<PointSpreadRequestModel> pointSpreads)
    {
        try
        {
            List<PointSpreadModel> results = new List<PointSpreadModel>();
            if (!pointSpreads.Any())
            {
                return results;
            }
            IEnumerable<PointSpreadDAOModel> allPointSpreads = await _pointSpreadDAO.GetByTournamentIdAsync(tournamentId);
            List<PointSpreadDAOModel> newPointSpreads = new List<PointSpreadDAOModel>();
            foreach (PointSpreadRequestModel pointSpread in pointSpreads)
            {
                if (!allPointSpreads.Any((PointSpreadDAOModel ps) => ((ps.Player1ID == pointSpread.Player1ID && ps.Player2ID == pointSpread.Player2ID) || (ps.Player1ID == pointSpread.Player2ID && ps.Player2ID == pointSpread.Player1ID)) && ps.BracketTypeId == (int)pointSpread.BracketType))
                {
                    PointSpreadDAOModel pointSpreadDAOModel = _mapper.Map<PointSpreadDAOModel>(pointSpread);
                    pointSpreadDAOModel.FavoredPlayerId = null;
                    newPointSpreads.Add(pointSpreadDAOModel);
                }
            }
            if (newPointSpreads.Any())
            {
                await generatePointSpread(newPointSpreads);
                foreach (PointSpreadDAOModel pointSpreadDAOModel2 in newPointSpreads)
                {
                    if (pointSpreadDAOModel2.Spread > 0)
                    {
                        pointSpreadDAOModel2.Spread *= -1;
                    }
                    await _pointSpreadDAO.CreatePointSpreadsAsync(pointSpreadDAOModel2);
                }
            }
            return results;
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    public async Task<Operation<List<PointSpreadModel>, ApiError>> GetPointSpreadsAsync(int tournamentId)
    {
        try
        {
            IEnumerable<PointSpreadDAOModel> pointSpreads = await _pointSpreadDAO.GetByTournamentIdAsync(tournamentId);
            return _mapper.Map<List<PointSpreadModel>>(pointSpreads);
        }
        catch (Exception ex)
        {
            Exception e = ex;
            return new ApiError(e.Message, HttpStatusCode.InternalServerError);
        }
    }

    private async Task<string> buildBaseAIText()
    {
        IEnumerable<GameResultDAOModel> allGames = await _gameResultDAO.SearchAsync(null, null, null);
        IEnumerable<PlayerDAOModel> allPlayers = await _playerDAO.ListPlayersAsync(null, includeDeleted: true);
        IEnumerable<GameTeamDAOModel> allTeams = await _gameTeamDAO.GetAll();
        StringBuilder text = new StringBuilder("Player1, Player2, Player1 Team, Player 2 Team, Player 1 Score, Player 2 Score, Player 1 Rushing Yards, Player 2 Rushing Yards, Player1 Passing Yards, Player2 Passing Yards, Game Date, GameType\r\n");
        foreach (GameResultDAOModel game in allGames)
        {
            PlayerDAOModel player1 = allPlayers.FirstOrDefault((PlayerDAOModel p) => p.PlayerId == game.Player1Id, new PlayerDAOModel());
            PlayerDAOModel player2 = allPlayers.FirstOrDefault((PlayerDAOModel p) => p.PlayerId == game.Player2Id, new PlayerDAOModel());
            GameTeamDAOModel team1 = allTeams.FirstOrDefault((GameTeamDAOModel t) => t.GameTeamId == game.Player1GameTeamID, new GameTeamDAOModel());
            GameTeamDAOModel team2 = allTeams.FirstOrDefault((GameTeamDAOModel t) => t.GameTeamId == game.Player2GameTeamID, new GameTeamDAOModel());
            StringBuilder stringBuilder = text;
            StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(24, 12, stringBuilder);
            handler.AppendFormatted(player1.FullName);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(player2.FullName);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(team1.TeamName);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(team2.TeamName);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(game.Player1Score);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(game.Player2Score);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(game.Player1RushingYards);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(game.Player2RushingYards);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(game.Player1PassingYards);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(game.Player2PassingYards);
            handler.AppendLiteral(", ");
            handler.AppendFormatted(game.DateAdded);
            handler.AppendLiteral(", ");
            handler.AppendFormatted((GameType)game.GameTypeId);
            handler.AppendLiteral("\r\n");
            stringBuilder.Append(ref handler);
        }
        string filePath = Path.Combine(_environment.ContentRootPath, "gptFiles", "pointspread.instructions.txt");
        return (await File.ReadAllTextAsync(filePath)).Replace("{{gamedata}}", text.ToString());
    }

    private async Task generatePointSpread(IEnumerable<PointSpreadDAOModel> pointSpreads)
    {
        try
        {
            IEnumerable<PlayerDAOModel> allPlayers = await _playerDAO.ListPlayersAsync(null, includeDeleted: true);
            string matchupList = string.Empty;
            string aiInstructions = await buildBaseAIText();
            foreach (PointSpreadDAOModel pointSpread in pointSpreads)
            {
                matchupList = matchupList + allPlayers.First((PlayerDAOModel p) => p.PlayerId == pointSpread.Player1ID).FullName + " vs " + allPlayers.First((PlayerDAOModel p) => p.PlayerId == pointSpread.Player2ID).FullName + "\r\n";
            }
            aiInstructions = aiInstructions.Replace("{{matchups}}", matchupList);
            string key = _configuration["ApplicationConfig:gptKey"];
            ChatClient client = new ChatClient("gpt-4o", key);
            List<ChatMessage> messages = new List<ChatMessage>(1)
            {
                new UserChatMessage(ChatMessageContentPart.CreateTextPart(aiInstructions))
            };
            ChatCompletion completion = client.CompleteChat(messages);
            Enumerable.Empty<PointSpreadResponse>();
            IEnumerable<PointSpreadResponse> respsonses;
            try
            {
                respsonses = JsonConvert.DeserializeObject<IEnumerable<PointSpreadResponse>>(completion.Content[0].Text);
            }
            catch (Exception)
            {
                throw;
            }
            foreach (PointSpreadDAOModel pointSpread2 in pointSpreads)
            {
                string player1Name = allPlayers.First((PlayerDAOModel p) => p.PlayerId == pointSpread2.Player1ID).FullName;
                string player2Name = allPlayers.First((PlayerDAOModel p) => p.PlayerId == pointSpread2.Player2ID).FullName;
                PointSpreadResponse response = respsonses.FirstOrDefault((PointSpreadResponse r) => r.Player2Name == player2Name && r.Player1Name == player1Name);
                PlayerDAOModel favoredPlayer = allPlayers.FirstOrDefault((PlayerDAOModel p) => p.FullName == response.FavoredPlayerName);
                if (response != null)
                {
                    pointSpread2.FavoredPlayerId = favoredPlayer?.PlayerId;
                    pointSpread2.Spread = response.Spread;
                    pointSpread2.Summary = response.Summary;
                }
            }
        }
        catch (Exception ex2)
        {
            Exception e = ex2;
            Console.WriteLine(e);
        }
    }
}
