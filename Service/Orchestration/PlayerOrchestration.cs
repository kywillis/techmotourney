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
        private readonly IMapper _mapper;
        private readonly IPlayerTournamentDAO _playerTournamentDAO;

        public PlayerOrchestration(IPlayerDAO playerDAO, IMapper mapper, IPlayerTournamentDAO playerTournamentDAO)
        {
            _playerDAO = playerDAO;
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
                var playerDAOModels = await _playerDAO.ListPlayersAsync(tournamentId);
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
    }
}
