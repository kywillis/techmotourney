using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.DataAccess.Interfaces;
using AutoMapper;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.ResultPattern;
using System.Net;
using Microsoft.Data.SqlClient;
using TecmoTourney.DataAccess;

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
        private readonly IMapper _mapper;

        public GameResultOrchestration(IGameResultDAO gameResultDAO, ITournamentsDAO tournamentsDAO, IPlayerDAO playerDAO, IMapper mapper, ITournamentBracketUpdateDAO tournamentBracketUpdateDAO)
        {
            _tournamentsDAO = tournamentsDAO;
            _gameResultDAO = gameResultDAO;
            _playerDAO = playerDAO;
            _mapper = mapper;
            _tournamentBracketUpdateDAO = tournamentBracketUpdateDAO;
        }

        public async Task<Operation<GameResultModel, ApiError>> SaveGameResultAsync(SaveGameResultRequestModel gameResult)
        {
            try
            {
                var errors = await ValidateGameResultAsync(null, gameResult);

                if (errors.Any())
                    return new ApiError(string.Join("; ", errors), HttpStatusCode.BadRequest);

                var gameResultDAOModel = _mapper.Map<GameResultDAOModel>(gameResult);
                GameResultDAOModel savedGameResultDAOModel;
                if(gameResult.GameResultId == null)
                    savedGameResultDAOModel = await _gameResultDAO.CreateGameResultAsync(gameResultDAOModel);
                else
                {
                    await _gameResultDAO.UpdateGameResultAsync(gameResultDAOModel.GameResultId, gameResultDAOModel);
                    savedGameResultDAOModel = (await _gameResultDAO.GetGameResultAsync(gameResultDAOModel.GameResultId))!;
                }
                var game = _mapper.Map<GameResultModel>(savedGameResultDAOModel);
                if(gameResult.GameType == GameType.Tournament)
                    await updateTournament(game);

                await populatePlayerNames(game);
                return game;
            }
            catch (Exception e)
            {
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
            catch (Exception e)
            {
                return new ApiError(e.Message, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<GameResultModel, ApiError>> GetById(int gameResultId)
        {
            var result = await _gameResultDAO.GetGameResultAsync(gameResultId);
            if(result == null)
            {
                return new ApiError("no game result found", HttpStatusCode.NotFound);
            }

            var game = _mapper.Map<GameResultModel>(result);
            await populatePlayerNames(game);
            return game;
        }

        public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByPlayerAsync(int playerId)
        {
            try
            {
                var gameResultDAOModels = await _gameResultDAO.ListResultsByPlayerAsync(playerId);
                var games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
                await populatePlayerNames(games);
                return games;
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByTournamentAsync(int tournamentId, bool includeDeledted = false)
        {
            try
            {
                var gameResultDAOModels = await _gameResultDAO.ListResultsByTournamentAsync(tournamentId, includeDeledted);
                var games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
                await populatePlayerNames(games);
                return games;
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByTournamentAsync(int tournamentId, int playerId)
        {
            try
            {
                var gameResultDAOModels = await _gameResultDAO.ListResultsByTournamentAsync(tournamentId);
                var games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels.Where(g => g.Player1Id == playerId || g.Player2Id == playerId));
                await populatePlayerNames(games);
                return games;
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<List<GameResultModel>, ApiError>> ListResultsByBracketGameIDsAsync(IEnumerable<int> bracketGameIds)
        {
            try
            {
                var gameResultDAOModels = await _gameResultDAO.ListResultsByBracketGameIDsAsync(bracketGameIds);
                var games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
                await populatePlayerNames(games);
                return games;
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<List<GameResultModel>, ApiError>> SearchAsync(int tournamentId, int? player1Id, int? player2Id)
        {
            try
            {
                var gameResultDAOModels = await _gameResultDAO.SearchAsync(tournamentId,player1Id, player2Id);
                var games = _mapper.Map<List<GameResultModel>>(gameResultDAOModels);
                await populatePlayerNames(games);
                return games;
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, HttpStatusCode.InternalServerError);
            }
        }
       
        private async Task updateTournament(GameResultModel gameResult)
        {
            var tournamentDAO = await _tournamentsDAO.GetById(gameResult.TournamentId);
            var tournament = _mapper.Map<TournamentModel>(tournamentDAO);
            var bracket = tournament.TournamentBracket;
            
            await _tournamentsDAO.UpdateTournamentBracketDataAsync(tournamentDAO.TournamentId, tournamentDAO.BracketData);
        }

        private async Task populatePlayerNames(GameResultModel game)
        { 
            await populatePlayerNames(new List<GameResultModel>() { game});
        }

        private async Task populatePlayerNames(List<GameResultModel> games)
        {
            var players = await _playerDAO.ListPlayersAsync();
            foreach (var game in games)
            {
                game.Player1.PlayerName = players.First(p => p.PlayerId == game.Player1.PlayerId).FullName;
                game.Player2.PlayerName = players.First(p => p.PlayerId == game.Player2.PlayerId).FullName;
            }
        }

        private async Task<List<string>> ValidateGameResultAsync(int? gameResultId, SaveGameResultRequestModel gameResult)
        {
            var errors = new List<string>();

            if (gameResultId.HasValue)
            {
                var existingGameResult = await _gameResultDAO.GetGameResultAsync(gameResultId.Value);
                if (existingGameResult == null)
                    errors.Add("Game result not found"); 
            }

            var existingTournament = await _tournamentsDAO.GetById(gameResult.TournamentId);
            if (existingTournament == null)
                errors.Add("Tournament not found");

            var player1 = await _playerDAO.GetPlayerAsync(gameResult.Player1.PlayerId);
            if (player1 == null)
                errors.Add("Player 1 not found");

            var player2 = await _playerDAO.GetPlayerAsync(gameResult.Player2.PlayerId);
            if (player2 == null)
                errors.Add("Player 2 not found");

            return errors;
        }
    }
}
