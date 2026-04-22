using TecmoTourney;
using TecmoTourney.Bracket;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;
using System.Net;

namespace TecmoTourney.Orchestration;

public class TournamentBracketReconciliationService : ITournamentBracketReconciliationService
{
    private const int MaxPasses = 12;
    private readonly ITournamentsDAO _tournamentsDAO;
    private readonly IGameResultDAO _gameResultDAO;
    private readonly IGameOddsDAO _gameOddsDAO;
    private readonly IWagerDAO _wagerDAO;
    private readonly IPlayerDAO _playerDAO;
    private readonly IWagerAuditDAO _wagerAuditDAO;
    private readonly IGameOddsGenerationService _gameOddsGenerationService;

    public TournamentBracketReconciliationService(
        ITournamentsDAO tournamentsDAO,
        IGameResultDAO gameResultDAO,
        IGameOddsDAO gameOddsDAO,
        IWagerDAO wagerDAO,
        IPlayerDAO playerDAO,
        IWagerAuditDAO wagerAuditDAO,
        IGameOddsGenerationService gameOddsGenerationService)
    {
        _tournamentsDAO = tournamentsDAO;
        _gameResultDAO = gameResultDAO;
        _gameOddsDAO = gameOddsDAO;
        _wagerDAO = wagerDAO;
        _playerDAO = playerDAO;
        _wagerAuditDAO = wagerAuditDAO;
        _gameOddsGenerationService = gameOddsGenerationService;
    }

    public async Task<Operation<RecalculateBracketResponseModel, ApiError>> ReconcileAsync(
        int tournamentId,
        IReadOnlyList<TournamentStandingModel> standings)
    {
        var aggregate = new RecalculateBracketResponseModel();
        var tournament = await _tournamentsDAO.GetById(tournamentId);
        if (tournament == null)
            return new ApiError($"Tournament {tournamentId} not found", HttpStatusCode.NotFound);

        if ((TournamentStatus)tournament.StatusId != TournamentStatus.Tournament)
        {
            aggregate.Skipped = true;
            aggregate.SkipReason = "Tournament is not in bracket phase.";
            return aggregate;
        }

        if (!string.IsNullOrWhiteSpace(tournament.BracketImage))
        {
            aggregate.Skipped = true;
            aggregate.SkipReason = "Tournament uses a static bracket image.";
            return aggregate;
        }

        if (DoubleElimBracketResolver.TournamentUsesLegacyJqueryBracket(tournament.BracketData))
        {
            aggregate.Skipped = true;
            aggregate.SkipReason = "Tournament uses legacy bracket data.";
            return aggregate;
        }

        var entrantCount = standings.Count;
        if (entrantCount < 4 || entrantCount > 32)
            return new ApiError("Tournament must have between 4 and 32 players for bracket reconciliation.", HttpStatusCode.BadRequest);

        var entrants = DoubleElimBracketResolver.BuildEntrantsFromStandings(standings);

        var allCreatedIds = new List<int>();
        var allDeletedIds = new List<int>();
        OddsGenerationStatusModel lastOdds = new() { Attempted = false, Success = true };

        for (var pass = 0; pass < MaxPasses; pass++)
        {
            var tourGames = (await _gameResultDAO.ListResultsByTournamentAsync(tournamentId, false))
                .Where(g => g.GameTypeId == (int)GameType.Tournament && !g.IsDeleted)
                .ToList();

            var snapshots = tourGames.Select(ToSnapshot).ToList();
            var (_, resolved, _) = DoubleElimBracketResolver.ResolveDoubleElimination(entrantCount, entrants, snapshots);

            var assignment = new Dictionary<int, ResolvedMatch>();
            foreach (var rm in resolved)
            {
                if (rm.GameResultId == null)
                    continue;
                assignment[rm.GameResultId.Value] = rm;
            }

            var invalidIds = new List<int>();
            foreach (var g in tourGames)
            {
                if (!assignment.TryGetValue(g.GameResultId, out var rm))
                {
                    invalidIds.Add(g.GameResultId);
                    continue;
                }

                var p1 = rm.Top.Participant?.PlayerId;
                var p2 = rm.Bottom.Participant?.PlayerId;
                if (p1 == null || p2 == null ||
                    !DoubleElimBracketResolver.SamePair(p1.Value, p2.Value, g.Player1Id, g.Player2Id))
                    invalidIds.Add(g.GameResultId);
            }

            var toCreate = resolved
                .Where(rm => rm.IsPending &&
                             rm.Top.Participant != null &&
                             rm.Bottom.Participant != null &&
                             rm.Top.Participant.PlayerId != rm.Bottom.Participant.PlayerId)
                .ToList();

            if (invalidIds.Count == 0 && toCreate.Count == 0)
                break;

            foreach (var id in invalidIds.Distinct())
            {
                await _gameOddsDAO.SoftDeleteByGameResultIdAsync(id);
                await CancelPendingWagersKeepingGameResultAsync(id, actorPlayerId: null);
                var g = tourGames.FirstOrDefault(x => x.GameResultId == id);
                if (g != null)
                {
                    g.IsDeleted = true;
                    await _gameResultDAO.UpdateGameResultAsync(id, g);
                }

                allDeletedIds.Add(id);
            }

            var baseTime = DateTime.UtcNow;
            var ordinal = 0;
            foreach (var rm in toCreate)
            {
                ordinal++;
                var pTop = rm.Top.Participant!.PlayerId;
                var pBot = rm.Bottom.Participant!.PlayerId;
                var newGame = new GameResultDAOModel
                {
                    TournamentId = tournamentId,
                    Player1Id = pTop,
                    Player2Id = pBot,
                    Player1Score = 0,
                    Player2Score = 0,
                    Player1PassingYards = 0,
                    Player2PassingYards = 0,
                    Player1RushingYards = 0,
                    Player2RushingYards = 0,
                    StatusId = (int)GameStatus.Waiting,
                    GameTypeId = (int)GameType.Tournament,
                    IsDeleted = false,
                    DateAdded = baseTime.AddTicks(ordinal * 10L)
                };
                var saved = await _gameResultDAO.CreateGameResultAsync(newGame);
                allCreatedIds.Add(saved.GameResultId);
            }
        }

        if (allCreatedIds.Count > 0)
        {
            var rows = new List<GameResultDAOModel>();
            foreach (var id in allCreatedIds.Distinct())
            {
                var g = await _gameResultDAO.GetGameResultAsync(id);
                if (g != null)
                    rows.Add(g);
            }

            if (rows.Count > 0)
                lastOdds = await _gameOddsGenerationService.EnsureOddsForNewGameResultsAsync(rows);
        }

        aggregate.CreatedGameResultIds = allCreatedIds.Distinct().ToList();
        aggregate.SoftDeletedGameResultIds = allDeletedIds.Distinct().ToList();
        aggregate.OddsGeneration = lastOdds;
        return aggregate;
    }

    private static BracketGameSnapshot ToSnapshot(GameResultDAOModel g) =>
        new()
        {
            GameResultId = g.GameResultId,
            Player1Id = g.Player1Id,
            Player2Id = g.Player2Id,
            Player1Score = g.Player1Score,
            Player2Score = g.Player2Score,
            Status = (GameStatus)g.StatusId,
            DateAdded = g.DateAdded == default ? DateTime.MinValue : g.DateAdded
        };

    private async Task CancelPendingWagersKeepingGameResultAsync(int gameResultId, int? actorPlayerId)
    {
        var wagers = (await _wagerDAO.GetByGameResultIdAsync(gameResultId)).ToList();
        foreach (var w in wagers.Where(x => x.Status == WagerStatus.Pending))
        {
            var player = await _playerDAO.GetPlayerAsync(w.PlayerId);
            if (player == null)
                continue;

            var balanceBefore = player.Balance;
            var balanceAfter = balanceBefore + w.StakeAmount;
            var now = DateTime.UtcNow;

            var updated = await _wagerDAO.CancelPendingKeepingGameResultAsync(w.WagerId, now);
            if (!updated)
                continue;

            await _playerDAO.UpdatePlayerBalanceAsync(w.PlayerId, balanceAfter);
            await _wagerAuditDAO.InsertAsync(new WagerAuditDAOModel
            {
                TournamentId = w.TournamentId,
                TargetPlayerId = w.PlayerId,
                ActorPlayerId = actorPlayerId,
                Action = WagerAuditAction.GameResultRemoved,
                WagerId = w.WagerId,
                GameResultId = gameResultId,
                Amount = w.StakeAmount,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                CreatedAt = now
            });
        }
    }
}
