using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.DataAccess.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System.IO;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration
{
    public class PlayerOrchestration : IPlayerOrchestration
    {
        private readonly IPlayerDAO _playerDAO;
        private readonly IGameResultDAO _gameResultDAO;
        private readonly IMapper _mapper;
        private readonly IPlayerTournamentDAO _playerTournamentDAO;

        public PlayerOrchestration(IPlayerDAO playerDAO, IGameResultDAO gameResultDAO, IMapper mapper, IPlayerTournamentDAO playerTournamentDAO)
        {
            _playerDAO = playerDAO;
            _gameResultDAO = gameResultDAO;
            _mapper = mapper;
            _playerTournamentDAO = playerTournamentDAO;
        }

        public async Task<Operation<PlayerModel, ApiError>> CreatePlayerAsync(CreatePlayerRequestModel player)
        {
            try
            {
                var existingPlayer = _playerDAO.ListPlayersAsync().Result.FirstOrDefault(p => string.Equals(p.EmailAddress, player.EmailAddress, StringComparison.OrdinalIgnoreCase));
                if(existingPlayer != null)
                    return new ApiError($"player with email address of {player.EmailAddress} already exists", System.Net.HttpStatusCode.BadRequest);

                var playerDAOModel = _mapper.Map<PlayerDAOModel>(player);               
                var addedPlayer = await _playerDAO.AddPlayerAsync(playerDAOModel);
                return _mapper.Map<PlayerModel>(addedPlayer);
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<PlayerModel, ApiError>> DeletePlayerAsync(int id)
        {
            try
            {
                var deleted = await _playerDAO.DeletePlayerAsync(id);
                if (!deleted)
                {
                    return new ApiError("Player not found", System.Net.HttpStatusCode.BadRequest);
                }

                return new PlayerModel { PlayerId = id };
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<PlayerModel, ApiError>> GetPlayerAsync(int playerId)
        {
            try
            {
                var playerDAOModel = await _playerDAO.GetPlayerAsync(playerId);
                if (playerDAOModel == null)
                {
                    return new ApiError("Player not found", System.Net.HttpStatusCode.NotFound);
                }

                return _mapper.Map<PlayerModel>(playerDAOModel);
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<List<PlayerModel>, ApiError>> GetAllPlayersAsync()
        {
            try
            {
                var playerDAOModels = await _playerDAO.ListPlayersAsync();
                return _mapper.Map<List<PlayerModel>>(playerDAOModels);
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<List<PlayerModel>, ApiError>> GetPlayersAsync(int tournamentId)
        {
            try
            {
                var playerDAOModels = await _playerDAO.ListPlayersAsync(tournamentId, true);
                return _mapper.Map<List<PlayerModel>>(playerDAOModels);
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<PlayerModel, ApiError>> UpdatePlayerAsync(int playerId, PlayerModel player)
        {
            try
            {
                var playerDAOModel = _mapper.Map<PlayerDAOModel>(player);
                var updatedPlayer = await _playerDAO.UpdatePlayerAsync(playerId, playerDAOModel);
                return _mapper.Map<PlayerModel>(updatedPlayer);
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<bool, ApiError>> AddPlayerToTournament(int playerId, int tournamentId)
        {
            try
            {
                await _playerTournamentDAO.CreatePlayerTournamentAsync(new PlayerTournamentDAOModel()
                {
                    PlayerId = playerId,
                    TournamentId = tournamentId
                });
                return true;
            }
            catch (InvalidOperationException e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.BadRequest);
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<bool, ApiError>> RemovePlayerFromTournament(int playerId, int tournamentId)
        {
            try
            {
                await _playerTournamentDAO.DeleteByPlayerAndTournamentIdAsync(playerId, tournamentId);
                return true;
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<Operation<List<PlayerSummaryModel>, ApiError>> GetAllPlayersSummariesAsync()
        {
            try
            {
                var results = await _gameResultDAO.SearchAsync(null, null, null);
                var playerDAOModels = await _playerDAO.ListPlayersAsync();
                var playerModels = _mapper.Map<List<PlayerModel>>(playerDAOModels);
                var summaries = _mapper.Map<List<PlayerSummaryModel>>(playerModels);
                
                foreach (var result in results)
                {                    
                    var playerSummary = summaries.FirstOrDefault(s => s.PlayerId == result.Player1Id);
                    if(playerSummary != null)
                    {
                        if(!playerSummary.TournamentIds.Contains(result.TournamentId))
                            playerSummary.TournamentIds.Add(result.TournamentId);
                        updateSummary(playerSummary, result);
                    }

                    playerSummary = summaries.FirstOrDefault(s => s.PlayerId == result.Player2Id);
                    if (playerSummary != null)
                    {
                        if (!playerSummary.TournamentIds.Contains(result.TournamentId))
                        playerSummary.TournamentIds.Add(result.TournamentId);

                        updateSummary(playerSummary, result);
                    }
                }

                return summaries;
            }
            catch (Exception e)
            {
                return new ApiError(e.Message, System.Net.HttpStatusCode.InternalServerError);
            }
        }

        private void updateSummary(PlayerSummaryModel summary, GameResultDAOModel result)
        {
            if (!summary.TournamentIds.Contains(result.TournamentId))
                summary.TournamentIds.Add(result.TournamentId);

            if (result.Player1Id == summary.PlayerId)
            {
                if (result.Player1Score > result.Player2Score)
                    summary.Wins++;
                else
                    summary.Loses++;
            }
            else
            {
                if (result.Player2Score > result.Player1Score)
                    summary.Wins++;
                else
                    summary.Loses++;
            }
        }
    }
}
