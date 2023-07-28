using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.Services;

internal class TelemetryFileService : ITelemetryFileService
{
    public IEnumerable<TelemetryFile> GetTelemetryFiles()
    {
        var drives = DriveInfo.GetDrives()
            .Where(drive => drive is
            {
                IsReady: true,
                DriveType: DriveType.Removable,
                DriveFormat: "FAT32"
            } && File.Exists($"{drive.RootDirectory}/.boardid"))
            .Select(d => new TelemetryDataStore(d.VolumeLabel, d.RootDirectory))
            .ToList();

        var files = new List<TelemetryFile>();
        foreach (var drive in drives)
        {
            files.AddRange(drive.Files);
        }

        return files;
    }
}