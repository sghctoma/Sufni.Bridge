using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using SecureStorage;

namespace Sufni.Bridge.ViewModels;

public partial class ImportSessionsViewModel : ViewModelBase
{
    #region Public properties

    public string ImportLabel => "Import Selected";

    #endregion Public properties

    #region Observable properties

    public ObservableCollection<TelemetryDataStore>? TelemetryDataStores { get; set; }

    public ObservableCollection<TelemetryFile> TelemetryFiles { get; } = new();

    [ObservableProperty] private TelemetryDataStore? selectedDataStore;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportSessionsCommand))]
    private int? selectedSetup;
    
    #endregion Observable properties

    #region Property change handlers

    async partial void OnSelectedDataStoreChanged(TelemetryDataStore? value)
    {           
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");

        if (value == null) return;

        try
        {
            var boards = await _httpApiService.GetBoards();
            var selectedBoard = boards.FirstOrDefault(b => b?.Id == value.BoardId, null);
            SelectedSetup = selectedBoard?.SetupId;
        }
        catch
        {
            // ignored
        }
        
        TelemetryFiles.Clear();
        var files = value.Files;
        foreach (var file in files)
        {
            TelemetryFiles.Add(file);
        }
    }

    #endregion Property change handlers

    #region Private members

    private readonly IHttpApiService? _httpApiService;
    private readonly ITelemetryDataStoreService? _telemetryDataStoreService;

    #endregion Private members
    
    #region Constructors

    public ImportSessionsViewModel()
    {
        _httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        _telemetryDataStoreService = App.Current?.Services?.GetService<ITelemetryDataStoreService>();
        
        Debug.Assert(_httpApiService != null, nameof(_telemetryDataStoreService) + " != null");
        Debug.Assert(_telemetryDataStoreService != null, nameof(_telemetryDataStoreService) + " != null");

        var ds = _telemetryDataStoreService.GetTelemetryDataStores();
        TelemetryDataStores = new ObservableCollection<TelemetryDataStore>(ds);

        if (TelemetryDataStores.Count > 0)
        {
            SelectedDataStore = TelemetryDataStores[0];
        }
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanImportSessions))]
    private async Task ImportSessions()
    {
        Debug.Assert(SelectedSetup != null);
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        
        foreach (var telemetryFile in TelemetryFiles.Where(f => f.ShouldBeImported))
        {
            try
            {
                await _httpApiService.ImportSession(telemetryFile, SelectedSetup.Value);
                telemetryFile.Imported = true;
                File.Move(telemetryFile.FullName,
                    $"{Path.GetDirectoryName(telemetryFile.FullName)}/uploaded/{telemetryFile.FileName}");
            }
            catch (HttpRequestException)
            {
                telemetryFile.Imported = false;
            }
            catch (Exception)
            {
                // NOTE: Move to "uploaded" failed. Should we handle this somehow?
            }
        }

        var newTelemetryFiles = TelemetryFiles.Where(f => !f.Imported).ToList();
        TelemetryFiles.Clear();
        foreach (var file in newTelemetryFiles)
        {
            TelemetryFiles.Add(file);
        }
    }

    private bool CanImportSessions()
    {
        var secureStorage = App.Current?.Services?.GetService<ISecureStorage>();
        Debug.Assert(secureStorage != null, nameof(secureStorage) + " != null");
        
        return SelectedSetup != null &&
               !string.IsNullOrEmpty(secureStorage.GetString("RefreshToken"));
        
        //TODO: ShouldBeImported changes do not notify, so the last condition
        //      is evaluated only when the program starts.
    }
    
    [RelayCommand]
    private void ReloadTelemetryDataStores()
    {
        Debug.Assert(TelemetryDataStores != null, nameof(TelemetryDataStores) + " != null");
        Debug.Assert(_telemetryDataStoreService != null, nameof(_telemetryDataStoreService) + " != null");

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