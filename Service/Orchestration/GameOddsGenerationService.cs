using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OpenAI.Chat;
using TecmoTourney;
using TecmoTourney.DataAccess.Interfaces;
using TecmoTourney.DataAccess.Models;
using TecmoTourney.Models;
using TecmoTourney.Orchestration.Interfaces;

namespace TecmoTourney.Orchestration
{
    public class GameOddsGenerationService : IGameOddsGenerationService
    {
        private class OddsGenerationResponse
        {
            public string Player1Name { get; set; } = string.Empty;
            public string Player2Name { get; set; } = string.Empty;
            public decimal Spread { get; set; }
            public string Summary { get; set; } = string.Empty;
            public string FavoredPlayerName { get; set; } = string.Empty;
            public decimal? MoneyLinePlayer1 { get; set; }
            public decimal? MoneyLinePlayer2 { get; set; }
            public decimal? OverUnder { get; set; }
        }

        private readonly IGameResultDAO _gameResultDAO;
        private readonly IPlayerDAO _playerDAO;
        private readonly IGameTeamDAO _gameTeamDAO;
        private readonly IGameOddsDAO _gameOddsDAO;
        private readonly IHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GameOddsGenerationService> _logger;

        public GameOddsGenerationService(
            IGameResultDAO gameResultDAO,
            IPlayerDAO playerDAO,
            IGameTeamDAO gameTeamDAO,
            IGameOddsDAO gameOddsDAO,
            IHostEnvironment environment,
            IConfiguration configuration,
            ILogger<GameOddsGenerationService> logger)
        {
            _gameResultDAO = gameResultDAO;
            _playerDAO = playerDAO;
            _gameTeamDAO = gameTeamDAO;
            _gameOddsDAO = gameOddsDAO;
            _environment = environment;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<OddsGenerationStatusModel> EnsureOddsForNewGameResultsAsync(
            IReadOnlyList<GameResultDAOModel> savedGamesWithIds,
            CancellationToken cancellationToken = default)
        {
            if (savedGamesWithIds == null || savedGamesWithIds.Count == 0)
                return new OddsGenerationStatusModel { Attempted = false, Success = true };

            var toGenerate = new List<GameOddsDAOModel>();
            foreach (var game in savedGamesWithIds)
            {
                if (game.GameResultId < 1)
                    continue;
                var existing = await _gameOddsDAO.GetByGameResultIdAsync(game.GameResultId);
                if (existing != null)
                    continue;

                var bracketTypeId = game.GameTypeId == (int)GameType.Preliminary
                    ? (int)BracketLocation.Preliminary
                    : (int)BracketLocation.Winners;

                var now = DateTime.UtcNow;
                toGenerate.Add(new GameOddsDAOModel
                {
                    GameResultId = game.GameResultId,
                    TournamentId = game.TournamentId,
                    Player1Id = game.Player1Id,
                    Player2Id = game.Player2Id,
                    BracketTypeId = bracketTypeId,
                    Spread = 0m,
                    FavoredPlayerId = null,
                    Summary = string.Empty,
                    DateAdded = now,
                    DateModified = now
                });
            }

            if (toGenerate.Count == 0)
                return new OddsGenerationStatusModel { Attempted = false, Success = true };

            var llmOk = false;
            try
            {
                llmOk = await TryApplyLlmOddsAsync(toGenerate, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM odds step failed before insert");
            }

            try
            {
                foreach (var gameOdds in toGenerate)
                {
                    if (gameOdds.Spread > 0m)
                        gameOdds.Spread = -gameOdds.Spread;
                    await _gameOddsDAO.CreatePointSpreadsAsync(gameOdds);
                }

                return new OddsGenerationStatusModel
                {
                    Attempted = true,
                    Success = llmOk,
                    Message = llmOk
                        ? null
                        : "Games are scheduled but automatic odds lines did not complete; use admin tools to set or verify lines."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist odds rows for {Count} game(s)", toGenerate.Count);
                return new OddsGenerationStatusModel
                {
                    Attempted = true,
                    Success = false,
                    Message = $"Odds could not be saved: {ex.Message}"
                };
            }
        }

        /// <summary>Returns true only if GPT returned JSON for every matchup with a non-empty summary and lines were applied.</summary>
        private async Task<bool> TryApplyLlmOddsAsync(List<GameOddsDAOModel> pointSpreads, CancellationToken cancellationToken)
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
                    return false;
                }

                var client = new ChatClient("gpt-5.4", key);
                var messages = new List<ChatMessage>
                {
                    new UserChatMessage(ChatMessageContentPart.CreateTextPart(aiInstructions))
                };
                var completion = await client.CompleteChatAsync(messages, cancellationToken: cancellationToken);
                var rawText = completion.Value.Content[0].Text;
                var jsonText = ExtractJsonPayload(rawText);
                var responses = JsonConvert.DeserializeObject<List<OddsGenerationResponse>>(jsonText);
                if (responses == null || responses.Count == 0)
                {
                    _logger.LogWarning("LLM odds: could not deserialize response as a non-empty JSON array");
                    return false;
                }

                var matched = new List<(GameOddsDAOModel Game, OddsGenerationResponse Response, decimal SpreadNorm)>();
                foreach (var gameOdds in pointSpreads)
                {
                    var player1Name = allPlayers.First(p => p.PlayerId == gameOdds.Player1Id).FullName;
                    var player2Name = allPlayers.First(p => p.PlayerId == gameOdds.Player2Id).FullName;
                    var response = responses.FirstOrDefault(r =>
                        NamesMatch(r.Player1Name, player1Name) && NamesMatch(r.Player2Name, player2Name));
                    if (response == null)
                    {
                        _logger.LogWarning(
                            "LLM odds: no JSON object matched players {P1} vs {P2}",
                            player1Name,
                            player2Name);
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(response.Summary))
                    {
                        _logger.LogWarning(
                            "LLM odds: matched row for {P1} vs {P2} is missing required non-empty summary",
                            player1Name,
                            player2Name);
                        return false;
                    }

                    if (!TryNormalizeHalfPointSpread(response.Spread, out var spreadNorm))
                    {
                        _logger.LogWarning(
                            "LLM odds: spread {Spread} for {P1} vs {P2} must be a non-zero half-point line (one decimal, ending in .5)",
                            response.Spread,
                            player1Name,
                            player2Name);
                        return false;
                    }

                    matched.Add((gameOdds, response, spreadNorm));
                }

                foreach (var (gameOdds, response, spreadNorm) in matched)
                {
                    var favoredPlayer = allPlayers.FirstOrDefault(p => p.FullName == response.FavoredPlayerName?.Trim());
                    gameOdds.FavoredPlayerId = favoredPlayer?.PlayerId;
                    gameOdds.Spread = spreadNorm;
                    gameOdds.Summary = response.Summary.Trim();
                    gameOdds.MoneyLinePlayer1 = NormalizeMoneyLineOneDecimal(response.MoneyLinePlayer1);
                    gameOdds.MoneyLinePlayer2 = NormalizeMoneyLineOneDecimal(response.MoneyLinePlayer2);
                    gameOdds.OverUnder = response.OverUnder;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM odds generation failed");
                return false;
            }
        }

        private static bool NamesMatch(string? fromModel, string fromDb)
        {
            if (string.IsNullOrWhiteSpace(fromModel))
                return false;
            return string.Equals(fromModel.Trim(), fromDb.Trim(), StringComparison.Ordinal);
        }

        /// <summary>Spread must be non-zero with exactly one decimal place and fractional part .5 (no integer / whole-number pushes).</summary>
        private static bool TryNormalizeHalfPointSpread(decimal raw, out decimal normalized)
        {
            normalized = Math.Round(raw, 1, MidpointRounding.AwayFromZero);
            if (normalized == 0m)
                return false;
            var abs = Math.Abs(normalized);
            var frac = abs - Math.Truncate(abs);
            return frac == 0.5m;
        }

        private static decimal? NormalizeMoneyLineOneDecimal(decimal? raw)
        {
            if (!raw.HasValue)
                return null;
            return Math.Round(raw.Value, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>Strips optional markdown fences so Newtonsoft can parse the array.</summary>
        private static string ExtractJsonPayload(string text)
        {
            var t = text.Trim();
            if (!t.StartsWith("```", StringComparison.Ordinal))
                return t;
            var afterFirstLine = t.IndexOf('\n');
            if (afterFirstLine < 0)
                return t;
            t = t[(afterFirstLine + 1)..].TrimStart();
            var fenceEnd = t.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd >= 0)
                t = t[..fenceEnd];
            return t.Trim();
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
