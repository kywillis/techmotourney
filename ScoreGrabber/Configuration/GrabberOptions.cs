namespace TecmoScoreGrabber.Configuration;

public sealed class GrabberOptions
{
    public string ApiBaseUrl { get; set; } = "https://localhost:5001/api";
    public OpenAiOptions OpenAI { get; set; } = new();
    public CaptureOptions Capture { get; set; } = new();
    public UiOptions Ui { get; set; } = new();
    public string SaveSource { get; set; } = "score-grabber";
    public int LogMaxBytes { get; set; } = 2 * 1024 * 1024;
}

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = "";
    /// <summary>Vision model (e.g. gpt-4o for better FSS reading than gpt-4o-mini).</summary>
    public string Model { get; set; } = "gpt-4o";
    /// <summary>Optional few-shot PNG path (relative to app base or absolute). Empty/null disables.</summary>
    public string? FewShotExampleImagePath { get; set; } = "Assets/fss-reference.png";
}

public sealed class CaptureOptions
{
    public int IntervalSeconds { get; set; } = 60;
    public int MonitorIndex { get; set; }
    public string FssReferenceImagePath { get; set; } = "fss-reference.png";
    public double FssSimilarityThreshold { get; set; } = 0.8;
    public int CooldownAfterSaveSeconds { get; set; } = 15;
    public int DebugFailedCaptureCount { get; set; } = 5;
    public string DebugFolder { get; set; } = "debug-captures";
}

public sealed class UiOptions
{
    /// <summary>
    /// Game Saved / Game Not Found dialogs close automatically after this many seconds unless the user taps Keep Open. Use 0 to disable auto-close (single Close button, no countdown).
    /// </summary>
    public int StyledDialogAutoCloseSeconds { get; set; } = 30;
}
