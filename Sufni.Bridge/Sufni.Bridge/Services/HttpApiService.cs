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
    #region Private fields

    private string? _serverUrl;
    private string? _refreshToken;
    private readonly HttpClient _client = new();
    
    #endregion
    
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
        _refreshToken = tokens.RefreshToken;
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
        using var response = await _client.GetAsync($"{_serverUrl}/api/board");
        response.EnsureSuccessStatusCode() ;
        var boards = await response.Content.ReadFromJsonAsync<List<Board>>();
        Debug.Assert(boards != null);
        return boards;
    }

    public async Task<List<Linkage>> GetLinkages()
    {
        using var response = await _client.GetAsync($"{_serverUrl}/api/linkage");
        response.EnsureSuccessStatusCode() ;
        var linkages = await response.Content.ReadFromJsonAsync<List<Linkage>>();
        Debug.Assert(linkages != null);
        return linkages;
    }

    public async Task<List<CalibrationMethod>> GetCalibrationMethods()
    {
        using var response = await _client.GetAsync($"{_serverUrl}/api/calibration-method");
        response.EnsureSuccessStatusCode();
        var methods = await response.Content.ReadFromJsonAsync<List<CalibrationMethod>>();
        Debug.Assert(methods != null);
        return methods;
    }

    public async Task<List<Calibration>> GetCalibrations()
    {
        using var response = await _client.GetAsync($"{_serverUrl}/api/calibration");
        response.EnsureSuccessStatusCode() ;
        var calibrations = await response.Content.ReadFromJsonAsync<List<Calibration>>();
        Debug.Assert(calibrations != null);
        return calibrations;
    }

    public async Task<List<Setup>> GetSetups()
    {
        using var response = await _client.GetAsync($"{_serverUrl}/api/setup");
        response.EnsureSuccessStatusCode() ;
        var setups = await response.Content.ReadFromJsonAsync<List<Setup>>();
        Debug.Assert(setups != null);
        return setups;
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
