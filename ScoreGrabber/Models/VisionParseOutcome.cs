namespace TecmoScoreGrabber.Models;

/// <summary>Vision (LLM) parse result plus the raw JSON string returned by the assistant.</summary>
public sealed record VisionParseOutcome(ParsedGameResult Parsed, string RawAssistantJson);
