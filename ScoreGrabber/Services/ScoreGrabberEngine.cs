using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using TecmoScoreGrabber.Configuration;
using TecmoScoreGrabber.Models;

namespace TecmoScoreGrabber.Services;

public sealed class ScoreGrabberEngine : IDisposable
{
    private readonly GrabberOptions _opt;
    private readonly ScreenCaptureService _capture;
    private readonly HttpClient _http;
    private readonly TecmoApiClient _api;
    private readonly OpenAiScoreParser _parser;
    private readonly RollingFileLogger _fileLog;
    private readonly string _debugDir;

    private Bitmap? _reference;
    private readonly object _previewLock = new();
    private Bitmap? _lastScreenGrab;
    private Bitmap? _lastVisionInputPreview;
    private bool _disposed;
    private bool _paused;
    private bool _postSaveWaitForFssToClear;
    /// <summary>After showing Game Not Found, block LLM/API until user leaves final score screen (same idea as post-save).</summary>
    private bool _postNoMatchWaitForFssToClear;
    /// <summary>When waiting for FSS to clear, restores tie vs non-tie phase text during cooldown ticks.</summary>
    private bool _postSaveWasTie;
    private string? _inFlightFssHash;

    public int Screenshots { get; private set; }
    /// <summary>Most recent full-frame FSS similarity (0–1) from the last screenshot comparison.</summary>
    public double? LastFssSimilarity { get; private set; }
    public int FssHits { get; private set; }
    public int LlmCalls { get; private set; }
    public int SavesOk { get; private set; }
    public int SavesFailed { get; private set; }
    public int NoMatch { get; private set; }
    public DateTime? LastApiSuccessUtc { get; private set; }
    public int? LastSaveHttpStatus { get; private set; }
    public string LastSaveMessage { get; private set; } = "";

    public event Action<GrabberLogEntry>? OnLog;
    public event Action<GrabberPhase>? OnPhase;
    public event Func<string, string, bool, Task>? OnSaveResultDialog;
    /// <summary>Raised after a tie game is saved with status In Progress (hook for future admin notifications).</summary>
    public event Action<int, int>? OnTieGameSaved;

    public ScoreGrabberEngine(
        GrabberOptions opt,
        ScreenCaptureService capture,
        HttpClient http,
        RollingFileLogger fileLog)
    {
        _opt = opt;
        _capture = capture;
        _http = http;
        _api = new TecmoApiClient(http);
        var fewShotPath = ResolveOptionalPath(opt.OpenAI.FewShotExampleImagePath);
        _parser = new OpenAiScoreParser(opt.OpenAI.ApiKey, opt.OpenAI.Model, fewShotPath);
        _fileLog = fileLog;
        _debugDir = Path.Combine(AppContext.BaseDirectory, opt.Capture.DebugFolder);
        Directory.CreateDirectory(_debugDir);
    }

    public void SetPaused(bool paused) => _paused = paused;

    public void LoadReference(string path)
    {
        _reference?.Dispose();
        _reference = FssDetector.LoadReference(path);
    }

    public async Task TickAsync(CancellationToken ct, bool ignorePause = false)
    {
        if ((!ignorePause && _paused) || _reference == null)
            return;

        SetPhase(GrabberPhase.Sampling);
        using var screen = _capture.CaptureMonitor(_opt.Capture.MonitorIndex);
        Screenshots++;

        lock (_previewLock)
        {
            _lastScreenGrab?.Dispose();
            _lastScreenGrab = (Bitmap)screen.Clone();
        }

        var sim = FssDetector.ComputeSimilarity(_reference, screen);
        LastFssSimilarity = sim;
        var hash = HashBitmap(screen);

        if (sim < _opt.Capture.FssSimilarityThreshold)
        {
            if (_postSaveWaitForFssToClear || _postNoMatchWaitForFssToClear)
            {
                Log("Info", "FSS cleared — ready for next game.", null, null);
                _postSaveWaitForFssToClear = false;
                _postSaveWasTie = false;
                _postNoMatchWaitForFssToClear = false;
                _inFlightFssHash = null;
            }
            SetPhase(GrabberPhase.WaitingForFinalScoreScreen);
            return;
        }

        FssHits++;

        // After a save or Game Not Found dialog, do not run LLM/API again until FSS similarity drops (user left the screen).
        // Hash-only matching allowed duplicate saves when PNG bytes jittered or tie games stayed in-progress.
        if (_postSaveWaitForFssToClear)
        {
            SetPhase(_postSaveWasTie ? GrabberPhase.TieAwaitingRematch : GrabberPhase.CooldownAfterSave);
            return;
        }

        if (_postNoMatchWaitForFssToClear)
        {
            SetPhase(GrabberPhase.NoMatchAwaitingClear);
            return;
        }

        if (_inFlightFssHash == hash)
            return;

        _inFlightFssHash = hash;
        await ProcessFssAsync(screen, hash, ct);
    }

    private async Task ProcessFssAsync(Bitmap screen, string hash, CancellationToken ct)
    {
        var correlation = Guid.NewGuid().ToString("N")[..12];
        try
        {
            SetPhase(GrabberPhase.LlmParsing);
            await PingApiAsync(ct);

            using var visionBmp = VisionInputNormalizer.NormalizeForLlm(screen);
            ReplaceVisionInputPreview((Bitmap)visionBmp.Clone());

            byte[] png;
            using (var ms = new MemoryStream())
            {
                visionBmp.Save(ms, ImageFormat.Png);
                png = ms.ToArray();
            }

            SetPhase(GrabberPhase.ApiMatching);
            var games = await _api.GetGameStationGamesAsync(ct);
            if (games != null)
                LastApiSuccessUtc = DateTime.UtcNow;

            var matchupHint = BuildVisionMatchupHint(games?.InProgress);
            if (!string.IsNullOrEmpty(matchupHint))
                Log("Info", "Vision prompt includes in-progress matchups from API.", matchupHint.ReplaceLineEndings(" | "), correlation);

            SetPhase(GrabberPhase.LlmParsing);
            LlmCalls++;
            var visionOutcome = await _parser.ParseScreenshotAsync(png, matchupHint, ct).ConfigureAwait(false);
            if (visionOutcome == null)
            {
                _inFlightFssHash = null;
                Log("Error", "Vision parser returned no parse result.", null, correlation);
                await SaveDebugCaptureAsync(screen, "parse-null", ct);
                return;
            }

            var parsed = visionOutcome.Parsed;
            var visionRawJsonForMatchLog = visionOutcome.RawAssistantJson;

            if (games == null)
            {
                _inFlightFssHash = null;
                NoMatch++;
                Log("Error", "Game station API returned null.", null, correlation);
                return;
            }

            if (!string.IsNullOrEmpty(visionRawJsonForMatchLog))
                Log("Info", "Matching in-progress game — vision (LLM) JSON used for team lookup.", visionRawJsonForMatchLog, correlation);

            var match = MatchInProgressGame(games.InProgress, parsed.Team1Name, parsed.Team2Name);
            if (match == null)
            {
                NoMatch++;
                Log("Warning", "No matching in-progress game for teams.", $"{parsed.Team1Name} vs {parsed.Team2Name}", correlation);
                _postNoMatchWaitForFssToClear = true;
                SetPhase(GrabberPhase.NoMatchAwaitingClear);
                var nl = Environment.NewLine + Environment.NewLine;
                var hint = "A matching game was not found, did you forget to start it?";
                var msg = "Game Not Found" + nl + hint;
                if (OnSaveResultDialog != null)
                    await OnSaveResultDialog("Game Not Found", msg, true);
                return;
            }

            SetPhase(GrabberPhase.Saving);
            var (p1, p2, swapped) = MapSides(match, parsed);
            var allowTie = parsed.IsTie;

            var body = new SaveGameResultRequest
            {
                GameResultId = match.GameResultId,
                TournamentId = match.TournamentId,
                // Tie: keep game in progress on the server; sudden-death / OT is entered manually by admin.
                Status = allowTie ? "InProgress" : "Completed",
                GameType = match.GameType,
                BracketGameId = match.BracketGameId,
                SaveSource = _opt.SaveSource,
                ClientCorrelationId = correlation,
                AllowTieScore = allowTie,
                AccumulateStatsFromTieLeg = false,
                Player1 = p1,
                Player2 = p2
            };

            var (status, responseText) = await _api.PutSaveGameResultAsync(match.GameResultId, body, ct);
            LastSaveHttpStatus = status;
            LastSaveMessage = responseText.Length > 500 ? responseText[..500] : responseText;

            if (status >= 200 && status < 300)
            {
                SavesOk++;
                Log("Info", "Final game score captured.", JsonSerializer.Serialize(body), correlation);
                _postSaveWaitForFssToClear = true;
                _postSaveWasTie = allowTie;

                if (allowTie)
                {
                    SetPhase(GrabberPhase.TieAwaitingRematch);
                    OnTieGameSaved?.Invoke(match.GameResultId, match.TournamentId);
                }
                else
                {
                    SetPhase(GrabberPhase.CooldownAfterSave);
                }

                var title = "Game Saved";
                var msg = FormatGameSavedDialogMessage(match, body, allowTie);
                if (OnSaveResultDialog != null)
                    await OnSaveResultDialog(title, msg, true);
            }
            else
            {
                SavesFailed++;
                _inFlightFssHash = null;
                Log("Error", $"Save failed HTTP {status}", responseText, correlation);
                await SaveDebugCaptureAsync(screen, $"save-{status}", ct);
                if (OnSaveResultDialog != null)
                    await OnSaveResultDialog("Save failed", $"HTTP {status}\n{LastSaveMessage}", false);
            }
        }
        catch (Exception ex)
        {
            SavesFailed++;
            _inFlightFssHash = null;
            Log("Error", ex.Message, ex.ToString(), correlation);
            await SaveDebugCaptureAsync(screen, "exception", ct);
            if (OnSaveResultDialog != null)
                await OnSaveResultDialog("Score grabber error", ex.Message, false);
        }
    }

    private async Task PingApiAsync(CancellationToken ct)
    {
        try
        {
            await _api.GetGameStationGamesAsync(ct);
            LastApiSuccessUtc = DateTime.UtcNow;
        }
        catch
        {
            // logged on real failure paths
        }
    }

    private static string FormatGameSavedDialogMessage(GameResultDto match, SaveGameResultRequest body, bool isTie)
    {
        static string PlayerVsTeamParens(string player, string team)
        {
            player = player.Trim();
            team = team.Trim();
            if (string.IsNullOrEmpty(player) && string.IsNullOrEmpty(team))
                return "—";
            if (string.IsNullOrEmpty(team))
                return player;
            if (string.IsNullOrEmpty(player))
                return $"({team})";
            return $"{player} ({team})";
        }

        const string tieExplanation =
            "Game was a tie. The stats have been saved, restart with the same teams and play sudden death. Results will be manually saved by the admin (Willis)";

        const string safeFooter = "It's safe to click the button below, really.";

        var matchup =
            $"{PlayerVsTeamParens(match.Player1.PlayerName, match.Player1.TeamName)} vs {PlayerVsTeamParens(match.Player2.PlayerName, match.Player2.TeamName)}";

        var total1 = body.Player1.PassingYards + body.Player1.RushingYards;
        var total2 = body.Player2.PassingYards + body.Player2.RushingYards;

        var statsBlock =
            matchup + Environment.NewLine + Environment.NewLine
            + $"Score: {body.Player1.Score} - {body.Player2.Score}" + Environment.NewLine + Environment.NewLine
            + $"Total Yards: {total1} - {total2}" + Environment.NewLine + Environment.NewLine
            + $"Passing: {body.Player1.PassingYards} - {body.Player2.PassingYards}" + Environment.NewLine + Environment.NewLine
            + $"Rushing: {body.Player1.RushingYards} - {body.Player2.RushingYards}";

        var nl = Environment.NewLine + Environment.NewLine;
        if (isTie)
        {
            return "Game Saved" + nl + tieExplanation + nl + statsBlock + nl + safeFooter;
        }

        return "Game Saved" + nl + statsBlock + nl + safeFooter;
    }

    private static (PlayerStatsRequest p1, PlayerStatsRequest p2, bool swapped) MapSides(GameResultDto g, ParsedGameResult p)
    {
        bool n1(string a, string b) => NamesMatch(a, b);
        if (n1(g.Player1.TeamName, p.Team1Name) && n1(g.Player2.TeamName, p.Team2Name))
        {
            return (Build(g.Player1, p.Team1Score, p.Team1PassingYards, p.Team1RushingYards),
                Build(g.Player2, p.Team2Score, p.Team2PassingYards, p.Team2RushingYards), false);
        }
        if (n1(g.Player1.TeamName, p.Team2Name) && n1(g.Player2.TeamName, p.Team1Name))
        {
            return (Build(g.Player1, p.Team2Score, p.Team2PassingYards, p.Team2RushingYards),
                Build(g.Player2, p.Team1Score, p.Team1PassingYards, p.Team1RushingYards), true);
        }
        return (Build(g.Player1, p.Team1Score, p.Team1PassingYards, p.Team1RushingYards),
            Build(g.Player2, p.Team2Score, p.Team2PassingYards, p.Team2RushingYards), false);
    }

    private static PlayerStatsRequest Build(PlayerSideDto side, int score, int pass, int rush) =>
        new()
        {
            PlayerId = side.PlayerId,
            GameTeamId = side.GameTeamId,
            Score = score,
            PassingYards = pass,
            RushingYards = rush
        };

    private static bool NamesMatch(string apiName, string parsedName)
    {
        var a = Normalize(apiName);
        var b = Normalize(parsedName);
        return a == b || a.Contains(b, StringComparison.OrdinalIgnoreCase) || b.Contains(a, StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string s) => s.Trim().ToUpperInvariant();

    private static string? ResolveOptionalPath(string? relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
            return null;
        var path = Path.IsPathRooted(relativeOrAbsolute)
            ? relativeOrAbsolute
            : Path.Combine(AppContext.BaseDirectory, relativeOrAbsolute);
        return File.Exists(path) ? path : null;
    }

    /// <summary>Lines appended to vision prompt listing games currently in-progress (API), to disambiguate team names vs pixel text.</summary>
    private static string? BuildVisionMatchupHint(List<GameResultDto>? inProgress)
    {
        if (inProgress == null || inProgress.Count == 0)
            return null;

        var bullets = new List<string>();
        foreach (var g in inProgress
                     .OrderBy(x => x.GameStartedAt ?? DateTime.MinValue)
                     .ThenBy(x => x.GameResultId))
        {
            var t1 = NormalizeTeamHint(g.Player1.TeamName);
            var t2 = NormalizeTeamHint(g.Player2.TeamName);
            if (t1.Length == 0 || t2.Length == 0)
                continue;
            bullets.Add($"  • {t1} vs {t2}");
        }

        if (bullets.Count == 0)
            return null;

        var nl = Environment.NewLine;
        return $"""TOURNAMENT CONTEXT (games currently in-progress on the tournament server — normally at most a few simultaneous). The final-score screen corresponds to ONE of these matchups; use ONLY these spellings for team1Name / team2Name (top headline row vs bottom headline row):{nl}{nl}{string.Join(nl, bullets)}""";

        static string NormalizeTeamHint(string name) =>
            (name ?? "").Trim().ToUpperInvariant();
    }

    private static GameResultDto? MatchInProgressGame(List<GameResultDto> list, string t1, string t2)
    {
        var hits = new List<GameResultDto>();
        foreach (var g in list)
        {
            var a = g.Player1.TeamName;
            var b = g.Player2.TeamName;
            if ((NamesMatch(a, t1) && NamesMatch(b, t2)) || (NamesMatch(a, t2) && NamesMatch(b, t1)))
                hits.Add(g);
        }
        if (hits.Count == 0)
            return null;
        return hits
            .OrderBy(x => x.GameStartedAt ?? DateTime.MaxValue)
            .ThenBy(x => x.GameResultId)
            .First();
    }

    private static string HashBitmap(Bitmap bmp)
    {
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        return Convert.ToHexString(MD5.HashData(ms.ToArray()));
    }

    private async Task SaveDebugCaptureAsync(Bitmap screen, string reason, CancellationToken ct)
    {
        try
        {
            var name = $"{DateTime.Now:yyyyMMdd-HHmmss}-{reason}.png";
            var path = Path.Combine(_debugDir, name);
            screen.Save(path, ImageFormat.Png);
            TrimDebugFolder(_opt.Capture.DebugFailedCaptureCount);
            Log("Info", $"Saved debug capture: {name}", null, null);
        }
        catch (Exception ex)
        {
            Log("Warning", "Could not save debug capture.", ex.Message, null);
        }
        await Task.CompletedTask;
    }

    private void TrimDebugFolder(int keepCount)
    {
        try
        {
            if (!Directory.Exists(_debugDir))
                return;
            var files = new DirectoryInfo(_debugDir).GetFiles("*.png")
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Skip(Math.Max(0, keepCount))
                .ToList();
            foreach (var f in files)
                f.Delete();
        }
        catch
        {
            // ignore
        }
    }

    private void Log(string severity, string message, string? details, string? correlation)
    {
        var line = string.IsNullOrEmpty(correlation)
            ? $"[{severity}] {message}"
            : $"[{severity}] [{correlation}] {message}";
        _fileLog.AppendLine(line);
        OnLog?.Invoke(new GrabberLogEntry
        {
            LocalTime = DateTime.Now,
            Severity = severity,
            Message = message,
            Details = details,
            CorrelationId = correlation
        });
    }

    private void SetPhase(GrabberPhase phase) => OnPhase?.Invoke(phase);

    /// <summary>Thread-safe clone of the most recent full-screen capture for UI preview.</summary>
    public Bitmap? CloneLastScreenGrabForPreview()
    {
        lock (_previewLock)
        {
            return _lastScreenGrab == null ? null : (Bitmap)_lastScreenGrab.Clone();
        }
    }

    /// <summary>Full-capture grayscale bitmap sent to the vision model (debug preview).</summary>
    public Bitmap? CloneLastVisionInputPreview()
    {
        lock (_previewLock)
        {
            return _lastVisionInputPreview == null ? null : (Bitmap)_lastVisionInputPreview.Clone();
        }
    }

    private void ReplaceVisionInputPreview(Bitmap? bmp)
    {
        lock (_previewLock)
        {
            _lastVisionInputPreview?.Dispose();
            _lastVisionInputPreview = bmp;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _reference?.Dispose();
        lock (_previewLock)
        {
            _lastScreenGrab?.Dispose();
            _lastScreenGrab = null;
            _lastVisionInputPreview?.Dispose();
            _lastVisionInputPreview = null;
        }
    }
}

public enum GrabberPhase
{
    WaitingForFinalScoreScreen,
    Sampling,
    LlmParsing,
    ApiMatching,
    Saving,
    CooldownAfterSave,
    TieAwaitingRematch,
    NoMatchAwaitingClear
}

public sealed class GrabberLogEntry
{
    public DateTime LocalTime { get; set; }
    public string Severity { get; set; } = "Info";
    public string Message { get; set; } = "";
    public string? Details { get; set; }
    public string? CorrelationId { get; set; }

    public string Header =>
        $"{LocalTime:yyyy-MM-dd HH:mm:ss} [{Severity}] {(string.IsNullOrEmpty(CorrelationId) ? "" : "[" + CorrelationId + "] ")}{Message}";
}
