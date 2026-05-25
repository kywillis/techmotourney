using System.Net;
using System.Text;
using AutoMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Chat;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Models.Requests;
using TecmoTourney.Orchestration.Interfaces;
using TecmoTourney.ResultPattern;

namespace TecmoTourney.Orchestration
{
    public class GameOddsGenerationService : IGameOddsGenerationService
    {
        private class OddsGenerationResponse
        {
            public string Player1Name { get; set; } = string.Empty;
            public string Player2Name { get; set; } = string.Empty;
            public int Spread { get; set; }
            public string Summary { get; set; } = string.Empty;
            public string FavoredPlayerName { get; set; } = string.Empty;
            public int? MoneyLinePlayer1 { get; set; }
            public int? MoneyLinePlayer2 { get; set; }
            public decimal? OverUnder { get; set; }
        }

        private readonly IGameResultDAO _gameResultDAO;
        private readonly IPlayerDAO _playerDAO;
        private readonly IGameTeamDAO _gameTeamDAO;
        private readonly IGameOddsDAO _gameOddsDAO;
        private readonly IMapper _mapper;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GameOddsGenerationService> _logger;

        public GameOddsGenerationService(
            IGameResultDAO gameResultDAO,
            IPlayerDAO playerDAO,
            IGameTeamDAO gameTeamDAO,
            IGameOddsDAO gameOddsDAO,
            IMapper mapper,
            IHostEnvironment environment,
            IConfiguration configuration,
            ILogger<GameOddsGenerationService> logger)
        {
            _gameResultDAO = gameResultDAO;
            _playerDAO = playerDAO;
            _gameTeamDAO = gameTeamDAO;
            _gameOddsDAO = gameOddsDAO;
            _mapper = mapper;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task EnsureOddsForNewGameResultsAsync(IReadOnlyList<GameResultDAOModel> savedGamesWithIds, CancellationToken cancellationToken = default)
        {
            if (savedGamesWithIds == null || savedGamesWithIds.Count == 0)
                return;

            var toGenerate = new List<GameOddsDAOModel>();
            foreach (var game in savedGamesWithIds)
            {
                if (game.GameResultId < 1)
                    continue;
                var existing = await _gameOddsDAO.GetByGameResultIdAsync(game.GameResultId);
                if (existing != null)
                    continue;

                var now = DateTime.UtcNow;
                toGenerate.Add(new GameOddsDAOModel
                {
                    GameResultId = game.GameResultId,
                    TournamentId = game.TournamentId,
                    Player1Id = game.Player1Id,
                    Player2Id = game.Player2Id,
                    BracketTypeId = (int)BracketLocation.Winners,
                    Spread = 0,
                    FavoredPlayerId = null,
                    Summary = string.Empty,
                    DateAdded = now,
                    DateModified = now
                });
            }

            if (toGenerate.Count == 0)
                return;

            try
            {
                await ApplyLlmOddsAsync(toGenerate, cancellationToken);
                foreach (var gameOdds in toGenerate)
                {
                    if (gameOdds.Spread > 0)
                        gameOdds.Spread *= -1;
                    await _gameOddsDAO.CreatePointSpreadsAsync(gameOdds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate or persist odds for {Count} new game(s)", toGenerate.Count);
            }
        }

        public async Task<Operation<List<GameOddsModel>, ApiError>> CreateOddsFromRequestsAsync(
            int tournamentId,
            IEnumerable<GameOddsRequestModel> pointSpreads,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var results = new List<GameOddsModel>();
                var requests = pointSpreads?.ToList() ?? new List<GameOddsRequestModel>();
                if (requests.Count == 0)
                    return results;

                var allGameOdds = (await _gameOddsDAO.GetByTournamentIdAsync(tournamentId)).ToList();
                var newGameOdds = new List<GameOddsDAOModel>();
                foreach (var request in requests)
                {
                    if (!allGameOdds.Any(g =>
                            ((g.Player1Id == request.Player1ID && g.Player2Id == request.Player2ID) ||
                             (g.Player1Id == request.Player2ID && g.Player2Id == request.Player1ID)) &&
                            g.BracketTypeId == (int)request.BracketType))
                    {
                        var gameOddsDAOModel = _mapper.Map<GameOddsDAOModel>(request);
                        gameOddsDAOModel.FavoredPlayerId = null;
                        gameOddsDAOModel.GameResultId = null;
                        var now = DateTime.UtcNow;
                        gameOddsDAOModel.DateAdded = now;
                        gameOddsDAOModel.DateModified = now;
                        newGameOdds.Add(gameOddsDAOModel);
                    }
                }

                if (newGameOdds.Count == 0)
                    return results;

                await ApplyLlmOddsAsync(newGameOdds, cancellationToken);
                foreach (var gameOdds in newGameOdds)
                {
                    if (gameOdds.Spread > 0)
                        gameOdds.Spread *= -1;
                    var created = await _gameOddsDAO.CreatePointSpreadsAsync(gameOdds);
                    results.Add(_mapper.Map<GameOddsModel>(created));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateOddsFromRequestsAsync failed for tournament {TournamentId}", tournamentId);
                return new ApiError(ex.Message, HttpStatusCode.InternalServerError);
            }
        }

        private async Task ApplyLlmOddsAsync(List<GameOddsDAOModel> pointSpreads, CancellationToken cancellationToken)
        {
            try
            {
                var allPlayers = (await _playerDAO.ListPlayersAsync(null, includeDeleted: true)).ToList();
                var matchupList = string.Empty;
                var aiInstructions = await BuildBaseAiTextAsync(cancellationToken);
                foreach (var gameOdds in pointSpreads)
                {
                    var p1 = allPlayers.First(p => p.PlayerId == gameOdds.Player1Id);
                    var p2 = allPlayers.First(p => p.PlayerId == gameOdds.Player2Id);
                    matchupList += p1.FullName + " vs " + p2.FullName + "\r\n";
                }

                aiInstructions = aiInstructions.Replace("{{matchups}}", matchupList);
                var key = _configuration["ApplicationConfig:gptKey"];
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogWarning("ApplicationConfig:gptKey is not set; skipping LLM odds generation");
                    return;
                }

                var client = new ChatClient("gpt-4o", key);
                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(ChatMessageContentPart.CreateTextPart(aiInstructions))
                };
                var completion = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
                var text = completion.Value.Content[0].Text;
                var responses = JsonConvert.DeserializeObject<IEnumerable<OddsGenerationResponse>>(text);
                if (responses == null)
                    return;

                foreach (var gameOdds in pointSpreads)
                {
                    var player1Name = allPlayers.First(p => p.PlayerId == gameOdds.Player1Id).FullName;
                    var player2Name = allPlayers.First(p => p.PlayerId == gameOdds.Player2Id).FullName;
                    var response = responses.FirstOrDefault(r =>
                        r.Player2Name == player2Name && r.Player1Name == player1Name);
                    if (response == null)
                        continue;
                    var favoredPlayer = allPlayers.FirstOrDefault(p => p.FullName == response.FavoredPlayerName);
                    gameOdds.FavoredPlayerId = favoredPlayer?.PlayerId;
                    gameOdds.Spread = response.Spread;
                    gameOdds.Summary = response.Summary;
                    gameOdds.MoneyLinePlayer1 = response.MoneyLinePlayer1;
                    gameOdds.MoneyLinePlayer2 = response.MoneyLinePlayer2;
                    gameOdds.OverUnder = response.OverUnder;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM odds generation failed");
            }
        }

        private async Task<string> BuildBaseAiTextAsync(CancellationToken cancellationToken)
        {
            var allGames = await _gameResultDAO.SearchAsync(null, null, null);
            var allPlayers = await _playerDAO.ListPlayersAsync(null, includeDeleted: true);
            var allTeams = await _gameTeamDAO.GetAll();
            var text = new StringBuilder(
                "Player1, Player2, Player1 Team, Player 2 Team, Player 1 Score, Player 2 Score, Player 1 Rushing Yards, Player 2 Rushing Yards, Player1 Passing Yards, Player2 Passing Yards, Game Date, GameType\r\n");
            foreach (var game in allGames)
            {
                var player1 = allPlayers.FirstOrDefault(p => p.PlayerId == game.Player1Id) ?? new PlayerDAOModel();
                var player2 = allPlayers.FirstOrDefault(p => p.PlayerId == game.Player2Id) ?? new PlayerDAOModel();
                var team1 = allTeams.FirstOrDefault(t => t.GameTeamId == game.Player1GameTeamID) ?? new GameTeamDAOModel();
                var team2 = allTeams.FirstOrDefault(t => t.GameTeamId == game.Player2GameTeamID) ?? new GameTeamDAOModel();
                text.Append(player1.FullName).Append(", ")
                    .Append(player2.FullName).Append(", ")
                    .Append(team1.TeamName).Append(", ")
                    .Append(team2.TeamName).Append(", ")
                    .Append(game.Player1Score).Append(", ")
                    .Append(game.Player2Score).Append(", ")
                    .Append(game.Player1RushingYards).Append(", ")
                    .Append(game.Player2RushingYards).Append(", ")
                    .Append(game.Player1PassingYards).Append(", ")
                    .Append(game.Player2PassingYards).Append(", ")
                    .Append(game.DateAdded).Append(", ")
                    .Append((GameType)game.GameTypeId).Append("\r\n");
            }

            var filePath = Path.Combine(_environment.ContentRootPath, "gptFiles", "pointspread.instructions.txt");
            var template = await File.ReadAllTextAsync(filePath, cancellationToken);
            return template.Replace("{{gamedata}}", text.ToString());
        }
    }
}
