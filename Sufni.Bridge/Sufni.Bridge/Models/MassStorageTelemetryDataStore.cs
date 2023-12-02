using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sufni.Bridge.Models;

public class MassStorageTelemetryDataStore : ITelemetryDataStore
{
    public string Name { get; }
    public string? BoardId { get; }
    public DriveInfo DriveInfo { get; }

    public Task<List<ITelemetryFile>> GetFiles()
    {
        var files = DriveInfo.RootDirectory.GetFiles("*.SST")
            .Select(f => (ITelemetryFile)new MassStorageTelemetryFile(f))
            .OrderBy(f => f.StartTime)
            .ToList();
        return Task.FromResult(files);
    }
    
    public MassStorageTelemetryDataStore(DriveInfo driveInfo)
    {
        DriveInfo = driveInfo;
        Name = $"{driveInfo.VolumeLabel} ({DriveInfo.RootDirectory.Name})";
        BoardId = File.ReadAllText($"{DriveInfo.RootDirectory.FullName}/.boardid").ToLower();

        if (!Directory.Exists($"{DriveInfo.RootDirectory.FullName}/uploaded"))
            Directory.CreateDirectory($"{DriveInfo.RootDirectory.FullName}/uploaded");
    }
}