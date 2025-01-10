using System.Collections.Generic;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.DataAccess.Interfaces;
using AutoMapper;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.ResultPattern;
using System.Net;

namespace TecmoTourney.Orchestration
{
    public class GameTeamsOrchestration : IGameTeamOrchestration
    {
        private readonly IGameTeamDAO _gameTeamDAO;
        private readonly IMapper _mapper;

        public GameTeamsOrchestration(IGameTeamDAO gameTeamDAO, IMapper mapper)
        {
            _gameTeamDAO = gameTeamDAO;
            _mapper = mapper;
        }

        public async Task<Operation<List<GameTeamModel>, ApiError>> GetAll()
        {
            try
            {
                var gameTeamsDAOModels = await _gameTeamDAO.GetAll();
                return _mapper.Map<List<GameTeamDAOModel>, List<GameTeamModel>>(gameTeamsDAOModels.ToList());
            }
            catch (Exception e)
            {
                return new ApiError($"could not get all gameteam: {e}", HttpStatusCode.InternalServerError);
            }
        }
} 
}
