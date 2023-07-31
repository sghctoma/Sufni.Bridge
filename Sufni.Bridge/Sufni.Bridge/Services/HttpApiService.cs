using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

internal class HttpApiService : IHttpApiService
{
    private string? _serverUrl;

    private readonly HttpClient _client = new();

    public async Task<string> RefreshTokensAsync(string url, string refreshToken)
    {
        _serverUrl = url;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        using HttpResponseMessage response = await _client.PostAsync($"{_serverUrl}/auth/refresh", null);
        
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<Tokens>();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        Debug.Assert(tokens != null);
        Debug.Assert(tokens.AccessToken != null);
        Debug.Assert(tokens.RefreshToken != null);
        return tokens.RefreshToken;
    }

    public async Task<string> RegisterAsync(string url, string username, string password)
    {
        _serverUrl = url;
        using HttpResponseMessage response = await _client.PostAsJsonAsync($"{_serverUrl}/auth/login",
            new User(Username: username, Password: password));

        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<Tokens>();
        Debug.Assert(tokens != null);
        Debug.Assert(tokens.AccessToken != null);
        Debug.Assert(tokens.RefreshToken != null);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return tokens.RefreshToken;
    }

    public async Task Unregister(string refreshToken)
    {
        _ = await _client.DeleteAsync($"{_serverUrl}/auth/logout");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        _ = await _client.DeleteAsync($"{_serverUrl}/auth/logout");
        _client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<List<Board>> GetBoards()
    {
        using HttpResponseMessage response = await _client.GetAsync($"{_serverUrl}/api/board");
        response.EnsureSuccessStatusCode() ;
        var boards = await response.Content.ReadFromJsonAsync<List<Board>>();
        Debug.Assert(boards != null);
        return boards;
    }

    public async Task ImportSession(TelemetryFile session, int setupId)
    {
        if (!session.ShouldBeImported) return;

        using HttpResponseMessage response = await _client.PutAsJsonAsync($"{_serverUrl}/api/session",
            new Session(
                Name: session.Name,
                Description: session.Description,
                Setup: setupId,
                Data: session.Data));

        response.EnsureSuccessStatusCode();
    }
}
