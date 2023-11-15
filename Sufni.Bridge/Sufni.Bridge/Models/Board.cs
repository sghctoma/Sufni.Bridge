using System.Text.Json.Serialization;
using SQLite;

namespace Sufni.Bridge.Models;

[Table("board")]
public class Board
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Board() { }

    public Board(string id, int? setupId)
    {
        Id = id;
        SetupId = setupId;
    }

    [JsonPropertyName("id")]
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; } = null!;

    [JsonPropertyName("setup_id")]
    [Column("setup_id")]
    public int? SetupId { get; set; }
}
