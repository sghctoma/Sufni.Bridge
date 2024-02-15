using System.Text.Json.Serialization;

namespace Sufni.Bridge.Models;

// ReSharper disable NotAccessedPositionalProperty.Global
public record User(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password);

// ReSharper disable once ClassNeverInstantiated.Global
// It's used in response.Content.ReadFromJsonAsync<Tokens>() calls
public record Tokens(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken);
