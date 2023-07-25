using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Sufni.Bridge.Services;

internal class HttpApiService : IHttpApiService
{
    public string? ServerUrl
    {
        get
        {
            return _client.BaseAddress?.ToString();
        }

        set
        {
            if (value != null)
            {
                _client.BaseAddress = new Uri(value);
            }
        }
    }

    private string? _sessionToken;

    private readonly HttpClient _client = new();

    public async Task<bool> IsRegisteredAsync()
    {
        if (_sessionToken == null || ServerUrl == null)
        {
            return false;
        }

        var response = await _client.GetAsync("/auth/user");
        return response.StatusCode == System.Net.HttpStatusCode.OK;
    }

    public async Task<JsonObject?> RegisterAsync(string url, string username, string password)
    {
        _client.BaseAddress = new Uri(url);

        var jsonObject = new JsonObject();
        jsonObject["username"] = username;
        jsonObject["password"] = password;
        var content = new StringContent(jsonObject.ToString(), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/auth/login", content);

        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }
}
