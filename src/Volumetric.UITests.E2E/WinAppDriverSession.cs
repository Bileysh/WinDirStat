using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Volumetric.UITests.E2E;

public sealed class WinAppDriverSession : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _sessionId;

    private WinAppDriverSession(HttpClient http, string sessionId)
    {
        _http = http;
        _sessionId = sessionId;
    }

    public static async Task<WinAppDriverSession> StartAsync(Uri serverUri, string appUserModelId)
    {
        var http = new HttpClient { BaseAddress = serverUri, Timeout = TimeSpan.FromSeconds(30) };

        var body = new { desiredCapabilities = new { app = appUserModelId } };
        var response = await http.PostAsJsonAsync("session", body);
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();

        if (!response.IsSuccessStatusCode || json?["sessionId"] is null)
        {
            http.Dispose();
            throw new InvalidOperationException(
                $"WinAppDriver session creation failed ({(int)response.StatusCode}): {json?.ToJsonString()}");
        }

        return new WinAppDriverSession(http, json["sessionId"]!.GetValue<string>());
    }

    public async Task<string> GetTitleAsync()
    {
        var json = await _http.GetFromJsonAsync<JsonNode>($"session/{_sessionId}/title");
        return json?["value"]?.GetValue<string>() ?? string.Empty;
    }

    public async Task<bool> ElementWithAccessibilityIdIsDisplayedAsync(string accessibilityId)
    {
        var findBody = new { @using = "accessibility id", value = accessibilityId };
        var findResponse = await _http.PostAsJsonAsync($"session/{_sessionId}/element", findBody);
        var findJson = await findResponse.Content.ReadFromJsonAsync<JsonNode>();

        if (!findResponse.IsSuccessStatusCode || findJson?["value"]?["ELEMENT"] is null)
        {
            return false;
        }

        var elementId = findJson["value"]!["ELEMENT"]!.GetValue<string>();
        var displayedJson = await _http.GetFromJsonAsync<JsonNode>(
            $"session/{_sessionId}/element/{elementId}/displayed");

        return displayedJson?["value"]?.GetValue<bool>() ?? false;
    }

    public void Dispose()
    {
        try
        {
            _http.Send(new HttpRequestMessage(HttpMethod.Delete, $"session/{_sessionId}"));
        }
        catch (Exception)
        {
        }

        _http.Dispose();
    }
}
