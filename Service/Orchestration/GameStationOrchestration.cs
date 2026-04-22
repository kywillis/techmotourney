using System.Collections.Generic;
using System.Linq;
using System.Net;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration;

public class GameStationOrchestration : IGameStationOrchestration
{
    private readonly ITournamentsOrchestration _tournamentsOrchestration;
    private readonly IGameResultOrchestration _gameResultOrchestration;
    private readonly IGameResultDAO _gameResultDAO;
    private readonly IPlayerDAO _playerDAO;
    private readonly IGameTeamDAO _gameTeamDAO;

    private static IEnumerable<GameTeamDAOModel> _gameTeams = Array.Empty<GameTeamDAOModel>();

    public GameStationOrchestration(
        ITournamentsOrchestration tournamentsOrchestration,
        IGameResultOrchestration gameResultOrchestration,
        IGameResultDAO gameResultDAO,
        IPlayerDAO playerDAO,
        IGameTeamDAO gameTeamDAO)
    {
        _tournamentsOrchestration = tournamentsOrchestration;
        _gameResultOrchestration = gameResultOrchestration;
        _gameResultDAO = gameResultDAO;
        _playerDAO = playerDAO;
        _gameTeamDAO = gameTeamDAO;
    }

    public async Task<Operation<GameStationGamesResponseModel, ApiError>> GetGamesForActiveTournamentAsync()
    {
        var activeOp = await _tournamentsOrchestration.GetActive();
        if (!activeOp.IsSuccess || activeOp.Data == null)
            return activeOp.Failure;

        var tournament = activeOp.Data;
        var listOp = await _gameResultOrchestration.ListResultsByTournamentAsync(tournament.TournamentId, false);
        if (!listOp.IsSuccess || listOp.Data == null)
            return listOp.Failure;

        var games = listOp.Data
            .Where(g => g.Status == GameStatus.Waiting || g.Status == GameStatus.InProgress)
            .ToList();

        var waiting = games.Where(g => g.Status == GameStatus.Waiting).OrderBy(g => g.GameResultId).ToList();
        var inProgress = games.Where(g => g.Status == GameStatus.InProgress).OrderBy(g => g.GameResultId).ToList();

        var players = (await _playerDAO.ListPlayersAsync()).ToList();
        foreach (var g in waiting)
            ApplyPlayerProfilePics(g, players);
        foreach (var g in inProgress)
            ApplyPlayerProfilePics(g, players);

        return new GameStationGamesResponseModel
        {
            TournamentId = tournament.TournamentId,
            TournamentName = tournament.Name,
            Waiting = waiting,
            InProgress = inProgress
        };
    }

    public async Task<Operation<GameResultModel, ApiError>> UpdateGameAsync(int gameResultId, GameStationUpdateRequestModel request)
    {
        var activeOp = await _tournamentsOrchestration.GetActive();
        if (!activeOp.IsSuccess || activeOp.Data == null)
            return activeOp.Failure;

        var tournamentId = activeOp.Data.TournamentId;

        var dao = await _gameResultDAO.GetGameResultAsync(gameResultId);
        if (dao == null || dao.IsDeleted)
            return new ApiError("Game not found", HttpStatusCode.NotFound);

        if (dao.TournamentId != tournamentId)
            return new ApiError("Game is not part of the active tournament", HttpStatusCode.BadRequest);

        var status = (GameStatus)dao.StatusId;
        if (status != GameStatus.Waiting && status != GameStatus.InProgress)
            return new ApiError("Only waiting or in-progress games can be updated here", HttpStatusCode.BadRequest);

        var err = ValidateTeams(request.Player1GameTeamId, request.Player2GameTeamId);
        if (err != null)
            return new ApiError(err, HttpStatusCode.BadRequest);

        if (request.RevertToWaiting)
        {
            if (status != GameStatus.InProgress)
                return new ApiError("Only in-progress games can be moved back to waiting", HttpStatusCode.BadRequest);

            dao.Player1GameTeamID = request.Player1GameTeamId;
            dao.Player2GameTeamID = request.Player2GameTeamId;
            dao.StatusId = (int)GameStatus.Waiting;
            dao.GameStartedAt = null;
        }
        else if (status == GameStatus.Waiting)
        {
            if (!request.StartGame)
                return new ApiError("Use Start game to begin a waiting match", HttpStatusCode.BadRequest);

            dao.Player1GameTeamID = request.Player1GameTeamId;
            dao.Player2GameTeamID = request.Player2GameTeamId;
            dao.StatusId = (int)GameStatus.InProgress;
            dao.GameStartedAt = DateTime.UtcNow;
        }
        else
        {
            dao.Player1GameTeamID = request.Player1GameTeamId;
            dao.Player2GameTeamID = request.Player2GameTeamId;
        }

        await _gameResultDAO.UpdateGameResultAsync(gameResultId, dao);

        var updated = await _gameResultDAO.GetGameResultAsync(gameResultId);
        if (updated == null)
            return new ApiError("Game not found after update", HttpStatusCode.InternalServerError);

        var model = await MapToModelWithNamesAsync(updated);
        return model;
    }

    /// <summary>
    /// Ensures player1/player2 have ProfilePic and names from TC_Players for this endpoint’s JSON
    /// (camelCase <c>profilePic</c>), so the game-station UI can show distinct face sprites.
    /// </summary>
    private static void ApplyPlayerProfilePics(GameResultModel game, IReadOnlyList<PlayerDAOModel> players)
    {
        var p1 = players.FirstOrDefault(p => p.PlayerId == game.Player1.PlayerId);
        if (p1 != null)
        {
            game.Player1.PlayerName = p1.FullName;
            game.Player1.ProfilePic = p1.ProfilePic >= 1 ? p1.ProfilePic : 1;
        }

        var p2 = players.FirstOrDefault(p => p.PlayerId == game.Player2.PlayerId);
        if (p2 != null)
        {
            game.Player2.PlayerName = p2.FullName;
            game.Player2.ProfilePic = p2.ProfilePic >= 1 ? p2.ProfilePic : 1;
        }
    }

    private static string? ValidateTeams(int t1, int t2)
    {
        if (t1 <= 0 || t2 <= 0)
            return "Each player must select a team.";
        if (t1 == t2)
            return "Each player must use a different team.";
        return null;
    }

    private async Task<GameResultModel> MapToModelWithNamesAsync(GameResultDAOModel dao)
    {
        var game = new GameResultModel
        {
            GameResultId = dao.GameResultId,
            TournamentId = dao.TournamentId,
            Status = (GameStatus)dao.StatusId,
            GameType = (GameType)dao.GameTypeId,
            Date = dao.DateAdded,
            GameStartedAt = dao.GameStartedAt,
            BracketGameId = dao.BracketGameId ?? 0,
            SeedingExemptPlayerId = dao.SeedingExemptPlayerId,
            Player1 = new GameResultStatsModel
            {
                PlayerId = dao.Player1Id,
                Score = dao.Player1Score,
                PassingYards = dao.Player1PassingYards,
                RushingYards = dao.Player1RushingYards,
                GameTeamId = dao.Player1GameTeamID
            },
            Player2 = new GameResultStatsModel
            {
                PlayerId = dao.Player2Id,
                Score = dao.Player2Score,
                PassingYards = dao.Player2PassingYards,
                RushingYards = dao.Player2RushingYards,
                GameTeamId = dao.Player2GameTeamID
            }
        };

        var players = await _playerDAO.ListPlayersAsync();
        var p1 = players.FirstOrDefault(p => p.PlayerId == game.Player1.PlayerId);
        var p2 = players.FirstOrDefault(p => p.PlayerId == game.Player2.PlayerId);
        if (p1 != null)
        {
            game.Player1.PlayerName = p1.FullName;
            game.Player1.ProfilePic = p1.ProfilePic >= 1 ? p1.ProfilePic : 1;
        }
        if (p2 != null)
        {
            game.Player2.PlayerName = p2.FullName;
            game.Player2.ProfilePic = p2.ProfilePic >= 1 ? p2.ProfilePic : 1;
        }

        if (!_gameTeams.Any())
            _gameTeams = await _gameTeamDAO.GetAll();
        var team1 = _gameTeams.FirstOrDefault(t => t.GameTeamId == game.Player1.GameTeamId);
        var team2 = _gameTeams.FirstOrDefault(t => t.GameTeamId == game.Player2.GameTeamId);
        if (team1 != null) game.Player1.TeamName = team1.TeamName;
        if (team2 != null) game.Player2.TeamName = team2.TeamName;

        return game;
    }
}
