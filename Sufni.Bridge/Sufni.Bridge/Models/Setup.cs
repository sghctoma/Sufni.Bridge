using System.Text.Json.Serialization;
using SQLite;

namespace Sufni.Bridge.Models;

[Table("setup")]
public class Setup
{
    // Just to satisfy sql-net-pcl's parameterless constructor requirement
    // Uninitialized non-nullable property warnings are suppressed with null! initializer.
    public Setup()
    {
    }

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
    public string Name { get; set; } = null!;

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