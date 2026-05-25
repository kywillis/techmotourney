using System.ClientModel;
using System.Text;
using System.Text.Json;
using OpenAI.Chat;
using TecmoScoreGrabber.Models;

namespace TecmoScoreGrabber.Services;

public sealed class OpenAiScoreParser
{
    /// <summary>Ground truth for the bundled few-shot image (Assets/fss-reference.png).</summary>
    private const string FewShotGoldenJson =
        """{"team1Name":"BROWNS","team2Name":"BILLS","team1Score":21,"team2Score":14,"team1PassingYards":268,"team2PassingYards":57,"team1RushingYards":2,"team2RushingYards":178}""";

    private readonly string _apiKey;
    private readonly string _model;
    private readonly byte[]? _fewShotExamplePng;

    private static readonly string BaseVisionInstructions =
        """
        You are reading a Tecmo Super Bowl final score screen (TECMO SPORTS NEWS style).

        The live capture is a full-monitor (or full-window) grab converted to grayscale; the game may not fill the frame-still read scores and stats from the FSS region.

        Team names: read the large team names in the header area (e.g. RAIDERS, BROWNS), not abbreviations from lower tables (e.g. CLE., BUF.).

        Final scores (critical): The game score is shown as two large numbers in the header next to the two team rows-one score aligned with the top team (team1) and one with the bottom team (team2). Read those headline scores exactly. Do NOT take team1Score/team2Score from PASS, RUN, TEAM STATISTICS, TEAM LEADER, yards columns, or any other part of the screen. Do NOT use 0 as a placeholder when real headline scores are visible.

        Passing and rushing yards: Use the TEAM STATISTICS section-PASS row for passing yards per team, RUNS row for rushing yards (the YDS column for each team). Map yards to the same team order as the header (team1 = top, team2 = bottom).

        If an example image and JSON are provided, they show the correct layout and field meanings for one real screen; your job is to extract the same fields from the live capture (which may be a different resolution or monitor).

        When TOURNAMENT CONTEXT lists in-progress matchups below, treat those team spellings as authoritative for JSON team1Name/team2Name: the screenshot is ONE of those matchups. Assign team1Name to the TOP headline row and team2Name to the BOTTOM headline row, using exactly one pair from that list as screen order dictates. Pixel text is easy to misread-for example COLTS vs COWBOYS have different lettering and lengths; rely on BOTH the header glyphs and this matchup list to choose the correct two teams.

        Return ONLY valid JSON with this exact shape (no markdown). Replace all numeric placeholders with values read from the live capture:
        """ +
        "{\"team1Name\":\"\",\"team2Name\":\"\",\"team1Score\":0,\"team2Score\":0,\"team1PassingYards\":0,\"team2PassingYards\":0,\"team1RushingYards\":0,\"team2RushingYards\":0}";

    public OpenAiScoreParser(string apiKey, string model, string? fewShotExampleImagePath = null)
    {
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o" : model;
        if (!string.IsNullOrWhiteSpace(fewShotExampleImagePath) && File.Exists(fewShotExampleImagePath))
            _fewShotExamplePng = File.ReadAllBytes(fewShotExampleImagePath);
    }

    /// <summary>
    /// <param name="matchupHint">Optional in-progress matchups from the game station API.</param>
    /// </summary>
    public async Task<VisionParseOutcome?> ParseScreenshotAsync(byte[] pngBytes, string? matchupHint, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");

        var client = new ChatClient(_model, _apiKey);

        var instructions = BaseVisionInstructions.Trim();
        if (!string.IsNullOrWhiteSpace(matchupHint))
            instructions += Environment.NewLine + Environment.NewLine + matchupHint.Trim();

        var parts = new List<ChatMessageContentPart> { ChatMessageContentPart.CreateTextPart(instructions) };

        if (_fewShotExamplePng is { Length: > 0 })
        {
            parts.Add(ChatMessageContentPart.CreateTextPart(
                "Example final score screen (reference only — different games will look similar but with different numbers and resolution):"));
            parts.Add(ChatMessageContentPart.CreateImagePart(
                BinaryData.FromBytes(_fewShotExamplePng), "image/png", ChatImageDetailLevel.High));
            parts.Add(ChatMessageContentPart.CreateTextPart(
                $"For the example image above, the correct JSON is:\n{FewShotGoldenJson}\n\nNow read THIS live capture and return ONLY JSON in the same shape:"));
        }
        else
        {
            parts.Add(ChatMessageContentPart.CreateTextPart("Live capture — extract from this image:"));
        }

        parts.Add(ChatMessageContentPart.CreateImagePart(
            BinaryData.FromBytes(pngBytes), "image/png", ChatImageDetailLevel.High));

        var messages = new List<ChatMessage> { new UserChatMessage(parts) };
        var options = new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() };

        var completion = await client.CompleteChatAsync(messages, options, ct).ConfigureAwait(false);
        var text = ExtractAssistantText(completion.Value);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var rawJson = text.Trim();
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;
        var parsed = new ParsedGameResult
        {
            Team1Name = root.GetProperty("team1Name").GetString() ?? "",
            Team2Name = root.GetProperty("team2Name").GetString() ?? "",
            Team1Score = root.GetProperty("team1Score").GetInt32(),
            Team2Score = root.GetProperty("team2Score").GetInt32(),
            Team1PassingYards = root.GetProperty("team1PassingYards").GetInt32(),
            Team2PassingYards = root.GetProperty("team2PassingYards").GetInt32(),
            Team1RushingYards = root.GetProperty("team1RushingYards").GetInt32(),
            Team2RushingYards = root.GetProperty("team2RushingYards").GetInt32()
        };
        return new VisionParseOutcome(parsed, rawJson);
    }

    /// <summary>
    /// OpenAI can return an empty <see cref="ChatCompletion.Content"/> list (e.g. refusal, content filter).
    /// Indexing <c>Content[0]</c> throws <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    private static string? ExtractAssistantText(ChatCompletion completion)
    {
        if (!string.IsNullOrWhiteSpace(completion.Refusal))
            return null;

        if (completion.Content is not { Count: > 0 })
            return null;

        var sb = new StringBuilder();
        foreach (var part in completion.Content)
        {
            if (part.Kind == ChatMessageContentPartKind.Text && !string.IsNullOrEmpty(part.Text))
                sb.Append(part.Text);
        }

        return sb.Length > 0 ? sb.ToString() : null;
    }
}
