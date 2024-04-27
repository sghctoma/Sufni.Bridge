using System;
using System.Text.Json.Serialization;
using SQLite;

namespace Sufni.Bridge.Models;

[Table("session")]
public class Session : Synchronizable
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Session() { }
    
    public Session(Guid id, string name, string description, Guid setup, string? data = null, int? timestamp = null, Guid? track = null)
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
    [PrimaryKey]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    [Column("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("description")]
    [Column("description")]
    public string Description { get; set; } = null!;

    [JsonPropertyName("setup")]
    [Column("setup_id")]
    public Guid Setup { get; set; }

    [JsonPropertyName("timestamp"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Column("timestamp")]
    public int? Timestamp { get; set; }

    [JsonPropertyName("track"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Column("track_id")]
    public Guid? Track { get; set; }

    [JsonPropertyName("data")]
    [Ignore]
    public string? RawData { get; set; }

    [JsonIgnore]
    [Column("data")]
    // ReSharper disable once UnusedMember.Global
    public byte[]? ProcessedData { get; set; }
    
    [JsonIgnore]
    [Column("front_springrate")]
    public string? FrontSpringRate { get; set; }
    
    [JsonIgnore]
    [Column("rear_springrate")]
    public string? RearSpringRate { get; set; }
    
    [JsonIgnore]
    [Column("front_hsc")]
    public uint? FrontHighSpeedCompression { get; set; }
    
    [JsonIgnore]
    [Column("rear_hsc")]
    public uint? RearHighSpeedCompression { get; set; }
    
    [JsonIgnore]
    [Column("front_lsc")]
    public uint? FrontLowSpeedCompression { get; set; }
    
    [JsonIgnore]
    [Column("rear_lsc")]
    public uint? RearLowSpeedCompression { get; set; }
    
    [JsonIgnore]
    [Column("front_lsr")]
    public uint? FrontLowSpeedRebound { get; set; }
    
    [JsonIgnore]
    [Column("rear_lsr")]
    public uint? RearLowSpeedRebound { get; set; }
    
    [JsonIgnore]
    [Column("front_hsr")]
    public uint? FrontHighSpeedRebound { get; set; }
    
    [JsonIgnore]
    [Column("rear_hsr")]
    public uint? RearHighSpeedRebound { get; set; }
}