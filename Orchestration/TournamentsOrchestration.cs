using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.DataAccess.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class TournamentsOrchestration : ITournamentsOrchestration
    {
        private readonly ITournamentsDAO _tournamentsDAO;

        public TournamentsOrchestration(ITournamentsDAO tournamentsDAO)
        {
            _tournamentsDAO = tournamentsDAO;
        }

        public async Task<IEnumerable<TournamentModel>> ListAllAsync()
        {
            return await _tournamentsDAO.ListAllAsync();
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
