using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Sufni.Bridge.ViewModels;

public partial class ImportSessionsViewModel : ViewModelBase
{
    #region Public properties

    public string ImportLabel { get; } = "Import Selected";

    #endregion Public properties

    #region Observable properties

    public ObservableCollection<TelemetryDataStore> TelemetryDataStores { get; }
    public ObservableCollection<TelemetryFile> TelemetryFiles { get; } = new();

    [ObservableProperty] private TelemetryDataStore? selectedDataStore;
    
    #endregion Observable properties

    #region Property change handlers

    partial void OnSelectedDataStoreChanged(TelemetryDataStore? value)
    {
        TelemetryFiles.Clear();
        if (value == null) return;
        var files = value.Files;
        foreach (var file in files)
        {
            TelemetryFiles.Add(file);
        }
    }

    #endregion Property change handlers

    #region Private members

    private readonly IHttpApiService _httpApiService;
    private readonly ITelemetryDataStoreService _telemetryDataStoreService;

    #endregion Private members

    #region Constructors

    public ImportSessionsViewModel()
    {
        _httpApiService = this.GetServiceOrCreateInstance<IHttpApiService>();
        _telemetryDataStoreService = this.GetServiceOrCreateInstance<ITelemetryDataStoreService>();

        var ds = _telemetryDataStoreService.GetTelemetryDataStores();
        TelemetryDataStores = new ObservableCollection<TelemetryDataStore>(ds);

        if (TelemetryDataStores.Count > 0)
        {
            SelectedDataStore = TelemetryDataStores[0];
        }
    }

    #endregion Constructors

    #region Commands

    [RelayCommand]
    private async Task ImportSessions()
    {
        Debug.Assert(SelectedDataStore != null);

        var boards = await _httpApiService.GetBoards();
        var boardsDict = boards.ToDictionary(b => b.Id!, b => b.SetupId);
        var boardId = SelectedDataStore.BoardId;
        var setupId = boardsDict[boardId];

        Debug.Assert(setupId != null);

        foreach (var telemetryFile in TelemetryFiles.Where(f => f.ShouldBeImported))
        {
            await _httpApiService.ImportSession(telemetryFile, setupId.Value);
            File.Move(telemetryFile.FullName,
                $"{Path.GetDirectoryName(telemetryFile.FullName)}/uploaded/{telemetryFile.FileName}");
        }

        ReloadTelemetryDataStores();
    }

    [RelayCommand]
    private void ReloadTelemetryDataStores()
    {
        TelemetryDataStores.Clear();
        var ds = _telemetryDataStoreService.GetTelemetryDataStores();
        foreach (var store in ds)
        {
            TelemetryDataStores.Add(store);
        }

        if (TelemetryDataStores.Count > 0)
        {
            SelectedDataStore = TelemetryDataStores[0];
        }
    }

    #endregion Commands
}