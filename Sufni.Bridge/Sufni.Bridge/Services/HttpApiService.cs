using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MessagePack;
using Sufni.Bridge.Models.Telemetry;
using Calibration = Sufni.Bridge.Models.Calibration;
using Linkage = Sufni.Bridge.Models.Linkage;

namespace Sufni.Bridge.Services;

internal class HttpApiService : IHttpApiService
{
    #region Private fields

    private string? serverUrl;
    private readonly HttpClient client = new();
    
    #endregion
    
    public async Task<string> RefreshTokensAsync(string url, string refreshToken)
    {
        serverUrl = url;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        using HttpResponseMessage response = await client.PostAsync($"{serverUrl}/auth/refresh", null);
        
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<Tokens>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        Debug.Assert(tokens != null);
        Debug.Assert(tokens.AccessToken != null);
        Debug.Assert(tokens.RefreshToken != null);
        return tokens.RefreshToken;
    }

    public async Task<string> RegisterAsync(string url, string username, string password)
    {
        serverUrl = url;
        using HttpResponseMessage response = await client.PostAsJsonAsync($"{serverUrl}/auth/login",
            new User(Username: username, Password: password));

        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<Tokens>();
        Debug.Assert(tokens != null);
        Debug.Assert(tokens.AccessToken != null);
        Debug.Assert(tokens.RefreshToken != null);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);
        return tokens.RefreshToken;
    }

    public async Task UnregisterAsync(string refreshToken)
    {
        _ = await client.DeleteAsync($"{serverUrl}/auth/logout");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        _ = await client.DeleteAsync($"{serverUrl}/auth/logout");
        client.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<List<Board>> GetBoardsAsync()
    {
        using var response = await client.GetAsync($"{serverUrl}/api/board");
        response.EnsureSuccessStatusCode() ;
        var boards = await response.Content.ReadFromJsonAsync<List<Board>>();
        Debug.Assert(boards != null);
        return boards;
    }

    public async Task PutBoardAsync(Board board)
    {
        using HttpResponseMessage response = await client.PutAsJsonAsync($"{serverUrl}/api/board", board);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PutResponse>();
        Debug.Assert(result != null);
    }
    
    public async Task<List<Linkage>> GetLinkagesAsync()
    {
        using var response = await client.GetAsync($"{serverUrl}/api/linkage");
        response.EnsureSuccessStatusCode();
        var linkages = await response.Content.ReadFromJsonAsync<List<Linkage>>();
        Debug.Assert(linkages != null);
        return linkages;
    }
    
    public async Task<int> PutLinkageAsync(Linkage linkage)
    {
        using HttpResponseMessage response = await client.PutAsJsonAsync($"{serverUrl}/api/linkage", linkage);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PutResponse>();
        Debug.Assert(result != null);
        linkage.Id = result.Id;
        return result.Id;
    }

    public async Task DeleteLinkageAsync(int id)
    {
        using var response = await client.DeleteAsync($"{serverUrl}/api/linkage/{id}");
        response.EnsureSuccessStatusCode();
    }
    
    public async Task<List<CalibrationMethod>> GetCalibrationMethodsAsync()
    {
        using var response = await client.GetAsync($"{serverUrl}/api/calibration-method");
        response.EnsureSuccessStatusCode();
        var methods = await response.Content.ReadFromJsonAsync<List<CalibrationMethod>>();
        Debug.Assert(methods != null);
        return methods;
    }

    public async Task<List<Calibration>> GetCalibrationsAsync()
    {
        using var response = await client.GetAsync($"{serverUrl}/api/calibration");
        response.EnsureSuccessStatusCode() ;
        var calibrations = await response.Content.ReadFromJsonAsync<List<Calibration>>();
        Debug.Assert(calibrations != null);
        return calibrations;
    }
    
    public async Task<int> PutCalibrationAsync(Calibration calibration)
    {
        using HttpResponseMessage response = await client.PutAsJsonAsync($"{serverUrl}/api/calibration", calibration);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PutResponse>();
        Debug.Assert(result != null);
        calibration.Id = result.Id;
        return result.Id;
    }

    public async Task DeleteCalibrationAsync(int id)
    {
        using var response = await client.DeleteAsync($"{serverUrl}/api/calibration/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<Setup>> GetSetupsAsync()
    {
        using var response = await client.GetAsync($"{serverUrl}/api/setup");
        response.EnsureSuccessStatusCode() ;
        var setups = await response.Content.ReadFromJsonAsync<List<Setup>>();
        Debug.Assert(setups != null);
        return setups;
    }
    
    public async Task<int> PutSetupAsync(Setup setup)
    {
        using HttpResponseMessage response = await client.PutAsJsonAsync($"{serverUrl}/api/setup", setup);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PutResponse>();
        Debug.Assert(result != null);
        setup.Id = result.Id;
        return result.Id;
    }

    public async Task DeleteSetupAsync(int id)
    {
        using var response = await client.DeleteAsync($"{serverUrl}/api/setup/{id}");
        response.EnsureSuccessStatusCode();
    }
    
    public async Task<List<Session>> GetSessionsAsync()
    {
        using var response = await client.GetAsync($"{serverUrl}/api/session");
        response.EnsureSuccessStatusCode() ;
        var sessions = await response.Content.ReadFromJsonAsync<List<Session>>();
        Debug.Assert(sessions != null);
        return sessions;
    }

    public async Task<TelemetryData> GetSessionPsstAsync(int id)
    {
        using var response = await client.GetAsync($"{serverUrl}/api/session/{id}/psst");
        response.EnsureSuccessStatusCode() ;
        var psst = await response.Content.ReadAsByteArrayAsync();
        Debug.Assert(psst != null);
        return MessagePackSerializer.Deserialize<TelemetryData>(psst);
    }
    
    public async Task<int> PutSessionAsync(Session session)
    {
        using HttpResponseMessage response = await client.PutAsJsonAsync($"{serverUrl}/api/session", session);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PutResponse>();
        Debug.Assert(result != null);
        session.Id = result.Id;
        return result.Id;
    }

    public async Task DeleteSessionAsync(int id)
    {
        using var response = await client.DeleteAsync($"{serverUrl}/api/session/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task ImportSessionAsync(TelemetryFile session, int setupId)
    {
        if (!session.ShouldBeImported) return;

        using HttpResponseMessage response = await client.PutAsJsonAsync($"{serverUrl}/api/session",
            new Session(
                name: session.Name,
                description: session.Description,
                setup: setupId,
                data: session.Base64Data,
                id: null, timestamp: null, track: null));

        response.EnsureSuccessStatusCode();
    }
}
