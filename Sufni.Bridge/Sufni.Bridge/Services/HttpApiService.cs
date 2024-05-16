using System;
using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

internal class HttpApiService : IHttpApiService
{
    #region Private fields

    private string? serverUrl;
    private readonly HttpClient client = new(Handler);
    
    private static readonly HttpClientHandler Handler = new()
    {
        UseCookies = false,
#if  DEBUG
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
#endif
    };
    
    #endregion
    
    // ReSharper disable once ClassNeverInstantiated.Local
    // It's used in response.Content.ReadFromJsonAsync<Tokens>() calls
    private record PutResponse(
        [property: JsonPropertyName("id")] Guid Id);
    
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
    
    public async Task<List<Session>> GetSessionsAsync()
    {
        using var response = await client.GetAsync($"{serverUrl}/api/session");
        response.EnsureSuccessStatusCode() ;
        var sessions = await response.Content.ReadFromJsonAsync<List<Session>>();
        Debug.Assert(sessions != null);
        return sessions;
    }

    public async Task<Guid> PutProcessedSessionAsync(string name, string description, byte[] data)
    {
        using HttpResponseMessage response = await client.PutAsJsonAsync($"{serverUrl}/api/session/psst",
            new
            {
                name,
                description,
                data = Convert.ToBase64String(data)
            });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PutResponse>();
        Debug.Assert(result != null);
        return result.Id;
    }
    
    public async Task<SynchronizationData> PullSyncAsync(int since = 0)
    {
        using var response = await client.GetAsync($"{serverUrl}/api/sync/pull?since={since}");
        response.EnsureSuccessStatusCode();
        var entities = await response.Content.ReadFromJsonAsync<SynchronizationData>();
        Debug.Assert(entities != null);
        return entities;
    }

    public async Task PushSyncAsync(SynchronizationData syncData)
    {
        using var response = await client.PutAsJsonAsync($"{serverUrl}/api/sync/push", syncData);
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]?> GetSessionPsstAsync(Guid id)
    {
        using var response = await client.GetAsync($"{serverUrl}/api/session/{id}/psst");
        response.EnsureSuccessStatusCode() ;
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task PatchSessionPsstAsync(Guid id, byte[] data)
    {
        using var response = await client.PatchAsync($"{serverUrl}/api/session/{id}/psst",
            new ByteArrayContent(data));
        response.EnsureSuccessStatusCode();
    }
}
