using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.Services;

internal class TelemetryDataStoreService : ITelemetryDataStoreService
{
    public IEnumerable<TelemetryDataStore> GetTelemetryDataStores()
    {
        return DriveInfo.GetDrives()
            .Where(drive => drive is
            {
                IsReady: true,
                // TODO: commented these out for testing in Android emulator, should uncomment them later.
                //DriveType: DriveType.Removable,
                //DriveFormat: "FAT32"
            } && File.Exists($"{drive.RootDirectory}/.boardid"))
            .Select(d => new TelemetryDataStore(d.VolumeLabel, d.RootDirectory))
            .ToList();
    }
}