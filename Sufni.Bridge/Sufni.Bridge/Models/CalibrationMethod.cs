using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite;

namespace Sufni.Bridge.Models;

public record CalibrationMethodProperties(
    [property: JsonPropertyName("inputs")] List<string> Inputs,
    [property: JsonPropertyName("intermediates")] Dictionary<string, string> Intermediates,
    [property: JsonPropertyName("expression")] string Expression);

[Table("calibration_method")]
public class CalibrationMethod
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
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
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    [Column("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("properties")]
    [Ignore]
    public CalibrationMethodProperties Properties { get; set; } = null!;

    [JsonIgnore]
    [Column("data")]
    // ReSharper disable once UnusedMember.Global
    public string? PropertiesRaw
    {
        get => JsonSerializer.Serialize(Properties);
        set
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            Properties = JsonSerializer.Deserialize<CalibrationMethodProperties>(value) ??
                         throw new InvalidOperationException();
        }
    }
}