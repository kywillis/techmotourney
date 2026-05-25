namespace TecmoScoreGrabber.Models;

public sealed class ParsedGameResult
{
    public string Team1Name { get; set; } = "";
    public string Team2Name { get; set; } = "";
    public int Team1Score { get; set; }
    public int Team2Score { get; set; }
    public int Team1PassingYards { get; set; }
    public int Team2PassingYards { get; set; }
    public int Team1RushingYards { get; set; }
    public int Team2RushingYards { get; set; }
    public bool IsTie => Team1Score == Team2Score;
}
