using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Sufni.Bridge.Models;

public record User(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password);

public record Tokens(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("refresh_token")] string RefreshToken);

public record Board(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("setup_id")] int SetupId);

public record Session(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("setup")] int Setup,
    [property: JsonPropertyName("data")] string Data);
    
public record Linkage(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("head_angle")] double HeadAngle,
    [property: JsonPropertyName("front_stroke")] double? FrontStroke,
    [property: JsonPropertyName("rear_stroke")] double? RearStroke,
    [property: JsonPropertyName("data")] string Data);

public record CalibrationMethodProperties(
    [property: JsonPropertyName("inputs")] List<string> Inputs,
    [property: JsonPropertyName("intermediates")] Dictionary<string, string> Intermediates,
    [property: JsonPropertyName("expression")] string Expression);

public record CalibrationMethod(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("properties")] CalibrationMethodProperties Properties);

public record Calibration(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("method_id")] int MethodId,
    [property: JsonPropertyName("inputs")] Dictionary<string, double> Inputs);

public record Setup(
    [property: JsonPropertyName("id")] int? Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("linkage_id")] int LinkageId,
    [property: JsonPropertyName("front_calibration_id")] int? FrontCalibrationId,
    [property: JsonPropertyName("rear_calibration_id")] int? RearCalibrationId);

public record PutResponse(
    [property: JsonPropertyName("id")] int Id);