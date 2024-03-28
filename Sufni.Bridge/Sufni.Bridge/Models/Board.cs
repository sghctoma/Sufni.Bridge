using System;
using System.Text.Json.Serialization;
using SQLite;

namespace Sufni.Bridge.Models;

[Table("board")]
public class Board : Synchronizable
{
    private readonly string id = null!;

    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Board() { }

    public Board(string id, Guid? setupId)
    {
        Id = id;
        SetupId = setupId;
    }

    [JsonPropertyName("id")]
    [PrimaryKey]
    [Column("id")]
    public string Id
    {
        get => id.ToLower();
        private init => id = value;
    }

    [JsonPropertyName("setup_id")]
    [Column("setup_id")]
    public Guid? SetupId { get; set; }
}
