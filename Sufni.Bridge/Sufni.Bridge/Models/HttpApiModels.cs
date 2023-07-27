using System.Text.Json.Serialization;

namespace Sufni.Bridge.Models;

public record class User(
    [property: JsonPropertyName("username")] string? Username = null,
    [property: JsonPropertyName("password")] string? Password = null);

public record class Tokens(
    [property: JsonPropertyName("access_token")] string? AccessToken = null,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null);