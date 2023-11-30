using Sufni.Bridge.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tmds.MDns;
using Timer = System.Timers.Timer;

namespace Sufni.Bridge.Services;

internal class TelemetryDataStoreService : ITelemetryDataStoreService
{
    private const string ServiceType = "_gosst._tcp";
    private static readonly object DataStoreLock = new();
    public ObservableCollection<ITelemetryDataStore> DataStores { get; } = new();
    
    private void GetMassStorageDatastores()
    {
        var drives = DriveInfo.GetDrives()
            .Where(drive => drive is
            {
                IsReady: true,
                DriveType: DriveType.Removable,
                DriveFormat: "FAT32"
            } && File.Exists($"{drive.RootDirectory}/.boardid"))
            .Select(d => new MassStorageTelemetryDataStore($"{d.VolumeLabel} ({d.RootDirectory.Name})", d.RootDirectory))
            .ToArray();
        var added = drives.Except(DataStores, new TelemetryDataStoreComparer()).ToArray();
        var removed = DataStores
            .Where(ds => ds is MassStorageTelemetryDataStore)
            .Except(drives, new TelemetryDataStoreComparer())
            .ToArray();
            
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

    private void RemoveNetworkDataStore(ServiceAnnouncementEventArgs e)
    {
        var ipAddress = e.Announcement.Addresses[0];
        var port = e.Announcement.Port;
        var name = $"gosst://{ipAddress}:{port}";

        lock (DataStoreLock)
        {
            var toRemove = DataStores.First(x => x.Name == name);
            DataStores.Remove(toRemove);
        }
    }
    
    private async Task AddNetworkDataStore(ServiceAnnouncementEventArgs e)
    {
        var ipAddress = e.Announcement.Addresses[0];
        var port = e.Announcement.Port;

        var ds = new NetworkTelemetryDataStore(ipAddress, port);
        await ds.Initialization;
        
        lock (DataStoreLock)
        {
            DataStores.Add(ds); 
        }
    }
    
    public TelemetryDataStoreService()
    {
        var serviceBrowser = new ServiceBrowser();
        serviceBrowser.ServiceAdded += async (_, e) => await AddNetworkDataStore(e);
        serviceBrowser.ServiceRemoved += (_, e) => RemoveNetworkDataStore(e);
        serviceBrowser.StartBrowse(ServiceType);
        
        GetMassStorageDatastores();
        var timer = new Timer(1000);
        timer.AutoReset = true;
        timer.Elapsed += (_, _) => GetMassStorageDatastores();
        timer.Enabled = true;
    }
}