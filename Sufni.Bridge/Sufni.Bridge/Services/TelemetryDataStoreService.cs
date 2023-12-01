using System.Collections.Generic;
using Sufni.Bridge.Models;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tmds.MDns;
using Timer = System.Timers.Timer;

namespace Sufni.Bridge.Services;

internal class DriveInfoComparer : IEqualityComparer<DriveInfo>
{
    public bool Equals(DriveInfo? ds1, DriveInfo? ds2)
    {
        if (ReferenceEquals(ds1, ds2))
            return true;

        if (ds1 is null || ds2 is null)
            return false;
        
        if (!ds1.IsReady || !ds2.IsReady)
            return false;

        return ds1.VolumeLabel == ds2.VolumeLabel;
    }

    public int GetHashCode(DriveInfo ds) => ds.IsReady ? ds.VolumeLabel.GetHashCode() : 0;
}

internal class TelemetryDataStoreService : ITelemetryDataStoreService
{
    private const string ServiceType = "_gosst._tcp";
    private static readonly object DataStoreLock = new();
    public ObservableCollection<ITelemetryDataStore> DataStores { get; } = new();
    
    private void GetMassStorageDatastores()
    {
        var comparer = new DriveInfoComparer();
        var current = DriveInfo.GetDrives()
            .Where(drive => drive is
            {
                IsReady: true,
                DriveType: DriveType.Removable,
                DriveFormat: "FAT32",
            } && File.Exists($"{drive.RootDirectory}/.boardid"))
            .ToArray();
        var known = DataStores
            .Where(ds => ds is MassStorageTelemetryDataStore)
            .Select(ds => ((MassStorageTelemetryDataStore)ds).DriveInfo)
            .ToArray();
        var added = current.Except(known, comparer).ToArray();
        var removed = known.Except(current, comparer).ToArray();
        
        foreach (var drive in added)
        {
            DataStores.Add(new MassStorageTelemetryDataStore(drive));
        }

        foreach (var drive in removed)
        {
            var toRemove = DataStores
                .First(ds => ds is MassStorageTelemetryDataStore msds && 
                             comparer.Equals(msds.DriveInfo, drive));
            DataStores.Remove(toRemove);
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
        timer.Elapsed += (_, _) =>
        {
            lock (DataStoreLock)
            {
                GetMassStorageDatastores();
            }
        };
        timer.Enabled = true;
    }
}