using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Controllers
{
    [ApiController]
    [Route("api/tournaments")]
    public class TournamentsController : ControllerBase
    {
        private readonly IGameResultOrchestration _gameResultOrchestration;
        private readonly ITournamentsOrchestration _tournamentsOrchestration;

        public TournamentsController(ITournamentsOrchestration tournamentsOrchestration, IGameResultOrchestration gameResultOrchestration)
        {
            _tournamentsOrchestration = tournamentsOrchestration;
            _gameResultOrchestration = gameResultOrchestration;
        }

        [HttpGet]
        public async Task<IActionResult> ListAll()
        {
            var tournaments = await _tournamentsOrchestration.ListAllAsync();
            return tournaments.ToActionResult();
        }

        [HttpGet("{tournamentId}")]
        public async Task<IActionResult> GetById(int tournamentId)
        {
            var results = await _tournamentsOrchestration.GetById(tournamentId);
            return results.ToActionResult();
        }

        [HttpGet("{tournamentId}/results/{playerId}")]
        public async Task<IActionResult> ListResultsByPlayer(int tournamentId, int playerId)
        {
            var results = await _gameResultOrchestration.ListResultsByTournamentAsync(tournamentId);            
            return results.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> AddTournament([FromBody] UpdateTournamentRequestModel tournament)
        {
            var newTournament = await _tournamentsOrchestration.AddTournamentAsync(tournament);
            return newTournament.ToActionResult();
        }

        [HttpPut("{tournamentId}")]
        public async Task<IActionResult> UpdateTournament(int tournamentId, [FromBody] UpdateTournamentRequestModel tournament)
        {
            var updatedTournament = await _tournamentsOrchestration.UpdateTournamentAsync(tournamentId, tournament);
            return updatedTournament.ToActionResult();
        }

        [HttpPatch("{tournamentId}")]
        public async Task<IActionResult> UpdateTournamentBracketData(int tournamentId, [FromBody] string bracketData)
        {
            var updatedTournament = await _tournamentsOrchestration.UpdateBracketDataAsync(tournamentId, bracketData);
            return updatedTournament.ToActionResult();
        }

        [HttpPut("{tournamentId}/status")]
        public async Task<IActionResult> UpdateStatus(int tournamentId, ChangeTournamentStatusRequest request)
        {
            if(tournamentId != request.TournamentId)
                return BadRequest("Tournament ID in the URL does not match the ID in the request.");

            var result = await _tournamentsOrchestration.ChangeStatusAsync(request);
            return result.ToActionResult();
        }

        [HttpDelete("{tournamentId}")]
        public async Task<IActionResult> DeleteTournament(int tournamentId)
        {
            var result = await _tournamentsOrchestration.DeleteTournamentAsync(tournamentId);
            return NoContent();
        }

        [HttpGet("{tournamentId}/standings")]
        public async Task<IActionResult> GetStandings(int tournamentId, [FromQuery] TournamentStatus status)
        {
            var result = await _tournamentsOrchestration.GetStandingsAsync(tournamentId, status);
            return result.ToActionResult();
        }
    }
}
