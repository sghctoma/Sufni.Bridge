using Sufni.Bridge.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Sufni.Bridge.ViewModels;
public class MainViewModel : ViewModelBase
{
    public ObservableCollection<TelemetrySession>? TelemetryFiles { get; set; }

    public bool IsRegistered { get; set; }

    public MainViewModel()
    {
        IsRegistered = true;

        var drives = DriveInfo.GetDrives()
            .Where(drive => drive is
            {
                IsReady: true,
                DriveType: DriveType.Removable,
                DriveFormat: "FAT32"
            } && File.Exists($"{drive.RootDirectory}/.boardid"))
            .Select(d => new TelemetryDataStore(d.VolumeLabel, d.RootDirectory))
            .ToList();

        List<TelemetrySession> sessions = new List<TelemetrySession>();
        foreach (var drive in drives)
        {
            sessions.AddRange(drive.Files);
        }
        TelemetryFiles = new ObservableCollection<TelemetrySession>(sessions);
    }
}