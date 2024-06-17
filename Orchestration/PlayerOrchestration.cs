using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.DataAccess.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using System.IO;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.DataAccess;

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

        public async Task<PlayerModel> CreatePlayerAsync(CreatePlayerRequestModel player, IFormFile logo)
        {
            var playerDAOModel = _mapper.Map<PlayerDAOModel>(player);

            if (logo != null && logo.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await logo.CopyToAsync(memoryStream);
                    //playerDAOModel.ProfilePic = memoryStream.ToArray();
                }
            }

            var addedPlayer = await _playerDAO.AddPlayerAsync(playerDAOModel);
            return _mapper.Map<PlayerModel>(addedPlayer);
        }

        public async Task<PlayerModel> DeletePlayerAsync(int id)
        {
            var deleted = await _playerDAO.DeletePlayerAsync(id);
            if (!deleted)
            {
                return null;
            }

            return new PlayerModel { PlayerId = id };
        }

        public async Task<PlayerModel> GetPlayerAsync(int playerId)
        {
            var playerDAOModel = await _playerDAO.GetPlayerAsync(playerId);
            if (playerDAOModel == null)
            {
                return null;
            }

            return _mapper.Map<PlayerModel>(playerDAOModel);
        }

        public async Task<IEnumerable<PlayerModel>> GetPlayersAsync(int tourneyId)
        {
            var playerDAOModels = await _playerDAO.ListPlayersAsync(tourneyId);
            return _mapper.Map<IEnumerable<PlayerModel>>(playerDAOModels);
        }

        public async Task<PlayerModel> UpdatePlayerAsync(int playerId, PlayerModel player)
        {
            var playerDAOModel = _mapper.Map<PlayerDAOModel>(player);
            var updatedPlayer = await _playerDAO.UpdatePlayerAsync(playerId, playerDAOModel);
            return _mapper.Map<PlayerModel>(updatedPlayer);
        }

        public async Task AddPlayerToTournament(int playerId, int tourneyId)
        {
            await _playerTournamentDAO.DeleteByPlayerAndTournamentIdAsync(playerId, tourneyId);
        }

        public async Task RemovePlayerFromTournament(int playerId, int tourneyId)
        {
            await _playerTournamentDAO.CreatePlayerTournamentAsync(new PlayerTournamentDAOModel() { 
                  PlayerId = playerId,
                  TournamentId = tourneyId
                });
        }
    }
}
