using System.Text.Json.Serialization;
using SQLite;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// The code itself does not call some getters / setters explicitly,
// but they are used by sql-net-pcl and/or JsonSerializer/JsonDeserializer.

namespace Sufni.Bridge.Models;

[Table("linkage")]
public class Linkage
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
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
    public string Name { get; set; } = null!;

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
