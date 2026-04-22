using TecmoTourney;

namespace TecmoTourney.Bracket;

public enum FeedKind
{
    Seed,
    Bye,
    Winner,
    Loser,
    Empty
}

public sealed class FeedRef
{
    public FeedKind Kind { get; set; }
    public string? MatchId { get; set; }
    public int? SeedSlot { get; set; }
}

public sealed class BracketMatch
{
    public string Id { get; set; } = string.Empty;
    public string Segment { get; set; } = string.Empty; // WB | LB | GF
    public int Round { get; set; }
    public int IndexInRound { get; set; }
    public FeedRef Top { get; set; } = new();
    public FeedRef Bottom { get; set; } = new();
}

public sealed class BracketParticipant
{
    public int PlayerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Seed { get; set; }
}

public sealed class ResolvedSlot
{
    public BracketParticipant? Participant { get; set; }
    public bool IsBye { get; set; }
}

public sealed class ResolvedMatch
{
    public BracketMatch Def { get; set; } = new();
    public ResolvedSlot Top { get; set; } = new();
    public ResolvedSlot Bottom { get; set; } = new();
    public int? GameResultId { get; set; }
    public GameStatus? Status { get; set; }
    public int? TopScore { get; set; }
    public int? BottomScore { get; set; }
    public int? WinnerId { get; set; }
    public bool IsPending { get; set; }
    public string? TopSourceLabel { get; set; }
    public string? BottomSourceLabel { get; set; }
    public string? WbMatchLabel { get; set; }
}

/// <summary>Minimal game facts for bracket resolution (mirrors Angular IGameResult fields used in resolve).</summary>
public sealed class BracketGameSnapshot
{
    public int GameResultId { get; set; }
    public int Player1Id { get; set; }
    public int Player2Id { get; set; }
    public int Player1Score { get; set; }
    public int Player2Score { get; set; }
    public GameStatus Status { get; set; }
    public DateTime DateAdded { get; set; }
}
