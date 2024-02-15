using SQLite;

namespace Sufni.Bridge.Models;

public class Synchronizable
{
    [Column("updated"), NotNull] public int Updated { get; set; }
    [Column("deleted")] public int? Deleted { get; set; }
}