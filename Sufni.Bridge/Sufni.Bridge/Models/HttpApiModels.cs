using System.Text.Json.Serialization;

namespace Sufni.Bridge.Models;

public record class User(
    [property: JsonPropertyName("username")] string? Username = null,
    [property: JsonPropertyName("password")] string? Password = null);

public record class Tokens(
    [property: JsonPropertyName("access_token")] string? AccessToken = null,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null);

public record class Board(
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("setup_id")] int? SetupId = null);

public record class Session(
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("setup")] int? Setup = null,
    [property: JsonPropertyName("data")] string? Data = null);