using System.Collections.Generic;
using System.Text.Json.Serialization;
using SQLite;

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

// ReSharper disable once ClassNeverInstantiated.Global
// It's used in response.Content.ReadFromJsonAsync<Tokens>() calls
public record PutResponse(
    [property: JsonPropertyName("id")] int Id);

[Table("board")]
public class Board
{
    public Board() { }
    
    public Board(string id, int? setupId)
    {
        Id = id;
        SetupId = setupId;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public string Id { get; set; }

    [JsonPropertyName("setup_id")]
    [Column("setup_id")]
    public int? SetupId { get; set; }
}

[Table("session")]
public class Session
{
    public Session() { }
    
    public Session(int? id, string name, string description, int setup, string? data = null, int? timestamp = null, int? track = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Setup = setup;
        Timestamp = timestamp;
        Track = track;
        RawData = data;
    }

    [JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    [Column("description")]
    public string Description { get; set; }

    [JsonPropertyName("setup")]
    [Column("setup_id")]
    public int Setup { get; set; }

    [JsonPropertyName("timestamp"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Column("timestamp")]
    public int? Timestamp { get; set; }

    [JsonPropertyName("track"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Column("track_id")]
    public int? Track { get; set; }

    [JsonPropertyName("data")]
    [Ignore]
    public string? RawData { get; set; }

    [JsonIgnore]
    [Column("data")]
    public byte[]? ProcessedData { get; set; }
}

[Table("linkage")]
public class Linkage
{
    public Linkage() { }
    
    public Linkage(int? id, string name, double headAngle, double? frontStroke, double? rearStroke, string? data)
    {
        Id = id;
        Name = name;
        HeadAngle = headAngle;
        FrontStroke = frontStroke;
        RearStroke = rearStroke;
        Data = data;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; }

    [JsonPropertyName("head_angle")]
    [Column("head_angle")]
    public double HeadAngle { get; set; }

    [JsonPropertyName("front_stroke")]
    [Column("front_stroke")]
    public double? FrontStroke { get; set; }

    [JsonPropertyName("rear_stroke")]
    [Column("rear_stroke")]
    public double? RearStroke { get; set; }

    [JsonPropertyName("data")]
    [Column("raw_lr_data")]
    public string? Data { get; set; }
}

public record CalibrationMethodProperties(
    [property: JsonPropertyName("inputs")] List<string> Inputs,
    [property: JsonPropertyName("intermediates")] Dictionary<string, string> Intermediates,
    [property: JsonPropertyName("expression")] string Expression);

[Table("calibration_method")]
public class CalibrationMethod
{
    public CalibrationMethod() { }
    
    public CalibrationMethod(int id, string name, string description, CalibrationMethodProperties properties)
    {
        Id = id;
        Name = name;
        Description = description;
        Properties = properties;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    [Column("description")]
    public string Description { get; set; }

    [JsonPropertyName("properties")]
    [Ignore]
    public CalibrationMethodProperties Properties { get; set; }
    
    [JsonIgnore]
    [Column("data")]
    public string? PropertiesRaw { get; set; }
}

[Table("calibration")]
public class Calibration
{
    public Calibration() { }
    
    public Calibration(int? id, string name, int methodId, Dictionary<string, double> inputs)
    {
        Id = id;
        Name = name;
        MethodId = methodId;
        Inputs = inputs;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; }

    [JsonPropertyName("method_id")]
    [Column("method_id")]
    public int MethodId { get; set; }
    
    [JsonPropertyName("inputs")]
    [Ignore]
    public Dictionary<string, double> Inputs { get; set; }
    
    [JsonIgnore]
    [Column("inputs")]
    public string? InputsRaw { get; set; }
}

[Table("setup")]
public class Setup
{
    public Setup() { }
    
    public Setup(int? id, string name, int linkageId, int? frontCalibrationId, int? rearCalibrationId)
    {
        Id = id;
        Name = name;
        LinkageId = linkageId;
        FrontCalibrationId = frontCalibrationId;
        RearCalibrationId = rearCalibrationId;
    }

    [JsonPropertyName("id")]
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int? Id { get; set; }

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; }

    [JsonPropertyName("linkage_id")]
    [Column("linkage_id")]
    public int LinkageId { get; set; }

    [JsonPropertyName("front_calibration_id")]
    [Column("front_calibration_id")]
    public int? FrontCalibrationId { get; set; }

    [JsonPropertyName("rear_calibration_id")]
    [Column("rear_calibration_id")]
    public int? RearCalibrationId { get; set; }
}