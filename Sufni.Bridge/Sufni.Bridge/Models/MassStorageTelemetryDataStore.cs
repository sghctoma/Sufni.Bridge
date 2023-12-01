using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.Models;

public class MassStorageTelemetryDataStore : ITelemetryDataStore
{
    public string Name { get; }
    public IList<ITelemetryFile> Files { get; }
    public string? BoardId { get; }

    public DriveInfo DriveInfo { get; }

    public MassStorageTelemetryDataStore(DriveInfo driveInfo)
    {
        DriveInfo = driveInfo;
        var path = driveInfo.RootDirectory;
        Name = $"{driveInfo.VolumeLabel} ({driveInfo.RootDirectory.Name})";
        BoardId = File.ReadAllText($"{path.FullName}/.boardid").ToLower();
        Files = path.GetFiles("*.SST")
            .Select(f => (ITelemetryFile)new MassStorageTelemetryFile(f))
            .OrderBy(f => f.StartTime)
            .ToList();

        if (!Directory.Exists($"{path.FullName}/uploaded"))
            Directory.CreateDirectory($"{path.FullName}/uploaded");
    }
}