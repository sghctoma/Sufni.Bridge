using System.Text.Json.Serialization;
using SQLite;

namespace Sufni.Bridge.Models;

[Table("session")]
public class Session
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
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
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    [Column("description")]
    public string Description { get; set; } = null!;

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
    // ReSharper disable once UnusedMember.Global
    public byte[]? ProcessedData { get; set; }
}