using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.DataAccess.Interfaces;
using AutoMapper;
using TecmoTourney.DataAccess.Models;

namespace TecmoTourney.Orchestration 
{
    public class TournamentsOrchestration : ITournamentsOrchestration
    {
        private readonly ITournamentsDAO _tournamentsDAO;
        private readonly IMapper _mapper;

        public TournamentsOrchestration(ITournamentsDAO tournamentsDAO, IMapper mapper)
        {
            _tournamentsDAO = tournamentsDAO;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TournamentModel>> ListAllAsync()
        {
            var tournaments = await _tournamentsDAO.ListAllAsync();
            return _mapper.Map< IEnumerable<TournamentDAOModel>, IEnumerable<TournamentModel>>(tournaments);
        }

        public async Task<PlayerModel> ListResultsByPlayerAsync(int playerId)
        {
            return await _tournamentsDAO.ListResultsByPlayerAsync(playerId);
        }

        public async Task AddTournamentAsync(CreateTournamentRequestModel tournament)
        {
            await _tournamentsDAO.AddTournamentAsync(tournament);
        }

        public async Task UpdateTournamentAsync(int tournamentId, UpdateTournamentRequestModel tournament)
        {
            await _tournamentsDAO.UpdateTournamentAsync(tournamentId, tournament);
        }

        public async Task DeleteTournamentAsync(int tournamentId)
        {
            await _tournamentsDAO.DeleteTournamentAsync(tournamentId);
        }
    }
}
