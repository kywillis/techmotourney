using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using TecmoScoreGrabber.Models;

namespace TecmoScoreGrabber.Services;

public sealed class TecmoApiClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public TecmoApiClient(HttpClient http) => _http = http;

    public async Task<GameStationGamesResponse?> GetGameStationGamesAsync(CancellationToken ct)
    {
        return await _http.GetFromJsonAsync<GameStationGamesResponse>("game-station/games", _json, ct);
    }

    public async Task<(int StatusCode, string Body)> PutSaveGameResultAsync(int gameResultId, SaveGameResultRequest body, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(body, _json);
        using var req = new HttpRequestMessage(HttpMethod.Put, $"results/{gameResultId}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        return ((int)resp.StatusCode, text);
    }
}
