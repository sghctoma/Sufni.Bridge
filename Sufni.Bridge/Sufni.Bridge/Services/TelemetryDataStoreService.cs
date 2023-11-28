using Sufni.Bridge.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Timer = System.Timers.Timer;

namespace Sufni.Bridge.Services;

internal class TelemetryDataStoreService : ITelemetryDataStoreService
{
    private static readonly object DataStoreLock = new();
    public ObservableCollection<ITelemetryDataStore> DataStores { get; } = new();
    
    private void GetMassStorageDatastores()
    {
        var drives = DriveInfo.GetDrives()
            .Where(drive => drive is
            {
                IsReady: true,
                // TODO: commented these out for testing in Android emulator, should uncomment them later.
                //DriveType: DriveType.Removable,
                //DriveFormat: "FAT32"
            } && File.Exists($"{drive.RootDirectory}/.boardid"))
            .Select(d => new MassStorageTelemetryDataStore(d.VolumeLabel, d.RootDirectory))
            .ToArray();
        var added = drives.Except(DataStores, new TelemetryDataStoreComparer());
        var removed = DataStores
            .Where(ds => ds is MassStorageTelemetryDataStore)
            .Except(drives, new TelemetryDataStoreComparer());
            
        lock (DataStoreLock)
        {
            foreach (var drive in added)
            {
                DataStores.Add(drive);
            }

            foreach (var drive in removed)
            {
                DataStores.Remove(drive);
            }
        }
    }

    public TelemetryDataStoreService()
    {
        GetMassStorageDatastores();
        var timer = new Timer(1000);
        timer.AutoReset = true;
        timer.Elapsed += (_, _) => GetMassStorageDatastores();
        timer.Enabled = true;
    }
}