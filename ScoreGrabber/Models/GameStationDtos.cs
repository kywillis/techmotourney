using System.Text.Json.Serialization;

namespace TecmoScoreGrabber.Models;

public sealed class GameStationGamesResponse
{
    [JsonPropertyName("tournamentId")]
    public int TournamentId { get; set; }

    [JsonPropertyName("tournamentName")]
    public string TournamentName { get; set; } = "";

    [JsonPropertyName("waiting")]
    public List<GameResultDto> Waiting { get; set; } = new();

    [JsonPropertyName("inProgress")]
    public List<GameResultDto> InProgress { get; set; } = new();
}

public sealed class GameResultDto
{
    [JsonPropertyName("gameResultId")]
    public int GameResultId { get; set; }

    [JsonPropertyName("tournamentId")]
    public int TournamentId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "";

    [JsonPropertyName("gameType")]
    public string GameType { get; set; } = "";

    [JsonPropertyName("bracketGameId")]
    public int BracketGameId { get; set; }

    [JsonPropertyName("gameStartedAt")]
    public DateTime? GameStartedAt { get; set; }

    [JsonPropertyName("player1")]
    public PlayerSideDto Player1 { get; set; } = new();

    [JsonPropertyName("player2")]
    public PlayerSideDto Player2 { get; set; } = new();
}

public sealed class PlayerSideDto
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }

    [JsonPropertyName("teamName")]
    public string TeamName { get; set; } = "";

    [JsonPropertyName("playerName")]
    public string PlayerName { get; set; } = "";

    [JsonPropertyName("gameTeamId")]
    public int? GameTeamId { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("passingYards")]
    public int PassingYards { get; set; }

    [JsonPropertyName("rushingYards")]
    public int RushingYards { get; set; }
}

public sealed class SaveGameResultRequest
{
    [JsonPropertyName("gameResultId")]
    public int GameResultId { get; set; }

    [JsonPropertyName("player1")]
    public PlayerStatsRequest Player1 { get; set; } = new();

    [JsonPropertyName("player2")]
    public PlayerStatsRequest Player2 { get; set; } = new();

    [JsonPropertyName("tournamentId")]
    public int TournamentId { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Completed";

    [JsonPropertyName("gameType")]
    public string GameType { get; set; } = "Tournament";

    [JsonPropertyName("bracketGameId")]
    public int BracketGameId { get; set; }

    [JsonPropertyName("saveSource")]
    public string? SaveSource { get; set; }

    [JsonPropertyName("clientCorrelationId")]
    public string? ClientCorrelationId { get; set; }

    [JsonPropertyName("allowTieScore")]
    public bool AllowTieScore { get; set; }

    [JsonPropertyName("accumulateStatsFromTieLeg")]
    public bool AccumulateStatsFromTieLeg { get; set; }
}

public sealed class PlayerStatsRequest
{
    [JsonPropertyName("playerId")]
    public int PlayerId { get; set; }

    [JsonPropertyName("gameTeamId")]
    public int? GameTeamId { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("passingYards")]
    public int PassingYards { get; set; }

    [JsonPropertyName("rushingYards")]
    public int RushingYards { get; set; }
}
