using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using SQLite;

namespace Sufni.Bridge.Models;

[Table("calibration")]
public class Calibration
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
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
    public string Name { get; set; } = null!;

    [JsonPropertyName("method_id")]
    [Column("method_id")]
    public int MethodId { get; set; }
    
    [JsonPropertyName("inputs")]
    [Ignore]
    public Dictionary<string, double> Inputs { get; set; } = null!;

    [JsonIgnore]
    [Column("inputs")]
    public string? InputsRaw
    {
        get => JsonSerializer.Serialize(Inputs);
        set
        {
            Debug.Assert(value != null, nameof(value) + " != null");
            Inputs = JsonSerializer.Deserialize<Dictionary<string, double>>(value) ??
                     throw new InvalidOperationException();
        }
    }
}
