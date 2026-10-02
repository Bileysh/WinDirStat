using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Volumetric.UITests.E2E;

public sealed class WinAppDriverSession : IDisposable
{
    private const int AppLaunchTimeoutSeconds = 30;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient _http;
    private readonly string _sessionId;

    private WinAppDriverSession(HttpClient http, string sessionId)
    {
        _http = http;
        _sessionId = sessionId;
    }

    public static async Task<WinAppDriverSession> StartAsync(Uri serverUri, string appUserModelId)
    {
        var http = new HttpClient
        {
            BaseAddress = serverUri,
            Timeout = TimeSpan.FromSeconds(AppLaunchTimeoutSeconds + 15)
        };

        var body = new JsonObject
        {
            ["desiredCapabilities"] = new JsonObject
            {
                ["app"] = appUserModelId,
                ["ms:waitForAppLaunch"] = AppLaunchTimeoutSeconds
            }
        };
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

    public async Task<string> GetTitleAsync() =>
        (await GetAsync("title"))?.GetValue<string>() ?? string.Empty;

    public async Task<bool> ElementWithAccessibilityIdIsDisplayedAsync(string accessibilityId) =>
        await FindElementAsync("accessibility id", accessibilityId) is { } elementId &&
        await IsDisplayedAsync(elementId);

    public async Task<string?> FindElementAsync(string strategy, string value)
    {
        var response = await _http.PostAsJsonAsync($"session/{_sessionId}/element", new { @using = strategy, value });
        var json = await response.Content.ReadFromJsonAsync<JsonNode>();
        return response.IsSuccessStatusCode ? json?["value"]?["ELEMENT"]?.GetValue<string>() : null;
    }

    public async Task<string> WaitForElementAsync(string strategy, string value, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        do
        {
            if (await FindElementAsync(strategy, value) is { } elementId)
            {
                return elementId;
            }

            await Task.Delay(PollInterval);
        } while (elapsed.Elapsed < timeout);

        throw new TimeoutException($"Element '{value}' (by {strategy}) did not appear within {timeout}.");
    }

    public async Task<bool> IsDisplayedAsync(string elementId) =>
        (await GetAsync($"element/{elementId}/displayed"))?.GetValue<bool>() ?? false;

    public Task ClickAsync(string elementId) => PostAsync($"element/{elementId}/click", new { });

    public async Task<string> GetWindowHandleAsync() =>
        (await GetAsync("window_handle"))?.GetValue<string>() ??
        throw new InvalidOperationException("WinAppDriver returned no current window handle.");

    public async Task<IReadOnlyList<string>> GetWindowHandlesAsync() =>
        (await GetAsync("window_handles"))?.AsArray().Select(h => h!.GetValue<string>()).ToList() ?? [];

    public async Task<string> WaitForNewWindowAsync(IReadOnlyCollection<string> knownHandles, TimeSpan timeout)
    {
        var elapsed = Stopwatch.StartNew();
        do
        {
            var newHandle = (await GetWindowHandlesAsync()).FirstOrDefault(h => !knownHandles.Contains(h));
            if (newHandle is not null)
            {
                return newHandle;
            }

            await Task.Delay(PollInterval);
        } while (elapsed.Elapsed < timeout);

        throw new TimeoutException($"No new application window appeared within {timeout}.");
    }

    public Task SwitchToWindowAsync(string handle) => PostAsync("window", new { name = handle });

    public async Task CloseCurrentWindowAsync()
    {
        var response = await _http.DeleteAsync($"session/{_sessionId}/window");
        await EnsureSuccessAsync(response, "close window");
    }

    private async Task<JsonNode?> GetAsync(string relativePath)
    {
        var response = await _http.GetAsync($"session/{_sessionId}/{relativePath}");
        await EnsureSuccessAsync(response, relativePath);
        return (await response.Content.ReadFromJsonAsync<JsonNode>())?["value"];
    }

    private async Task PostAsync(string relativePath, object body)
    {
        var response = await _http.PostAsJsonAsync($"session/{_sessionId}/{relativePath}", body);
        await EnsureSuccessAsync(response, relativePath);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"WinAppDriver '{operation}' failed ({(int)response.StatusCode}): " +
                await response.Content.ReadAsStringAsync());
        }
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
