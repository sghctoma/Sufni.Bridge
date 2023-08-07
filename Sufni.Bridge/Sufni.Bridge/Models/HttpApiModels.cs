using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sufni.Bridge.Models;

public record User(
    [property: JsonPropertyName("username")] string? Username = null,
    [property: JsonPropertyName("password")] string? Password = null);

public record Tokens(
    [property: JsonPropertyName("access_token")] string? AccessToken = null,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken = null);

public record Board(
    [property: JsonPropertyName("id")] string? Id = null,
    [property: JsonPropertyName("setup_id")] int? SetupId = null);

public record Session(
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("setup")] int? Setup = null,
    [property: JsonPropertyName("data")] string? Data = null);
    
public record Linkage(
    [property: JsonPropertyName("id")] int? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("head_angle")] double? HeadAngle = null,
    [property: JsonPropertyName("front_stroke")] double? FrontStroke = null,
    [property: JsonPropertyName("rear_stroke")] double? RearStroke = null,
    [property: JsonPropertyName("data")] string? Data = null);

public record CalibrationMethodProperties(
    [property: JsonPropertyName("inputs")] List<string>? Inputs = null,
    [property: JsonPropertyName("intermediates")] Dictionary<string, string>? Intermediates = null,
    [property: JsonPropertyName("expression")] string? Expression = null);

public record CalibrationMethod(
    [property: JsonPropertyName("id")] int? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("description")] string? Description = null,
    [property: JsonPropertyName("properties")] CalibrationMethodProperties? Properties = null);

public record Calibration(
    [property: JsonPropertyName("id")] int? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("method_id")] int? MethodId = null,
    [property: JsonPropertyName("inputs")] Dictionary<string, double>? Inputs = null);

public record Setup(
    [property: JsonPropertyName("id")] int? Id = null,
    [property: JsonPropertyName("name")] string? Name = null,
    [property: JsonPropertyName("linkage_id")] int? LinkageId = null,
    [property: JsonPropertyName("front_calibration_id")] int? FrontCalibrationId = null,
    [property: JsonPropertyName("rear_calibration_id")] int? RearCalibrationId = null);