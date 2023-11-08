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
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        if (value == null) return;

        try
        {
            var boards = await httpApiService.GetBoardsAsync();
            var selectedBoard = boards.FirstOrDefault(b => b?.Id == value.BoardId, null);
            SelectedSetup = selectedBoard?.SetupId;
        }
        catch(Exception e)
        {
            ErrorMessages.Add($"Error while changing data store: {e.Message}");
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

    private readonly IHttpApiService? httpApiService;
    private readonly ITelemetryDataStoreService? telemetryDataStoreService;

    #endregion Private members
    
    #region Constructors

    public ImportSessionsViewModel()
    {
        httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        telemetryDataStoreService = App.Current?.Services?.GetService<ITelemetryDataStoreService>();
        
        Debug.Assert(httpApiService != null, nameof(telemetryDataStoreService) + " != null");
        Debug.Assert(telemetryDataStoreService != null, nameof(telemetryDataStoreService) + " != null");

        try
        {
            var ds = telemetryDataStoreService.GetTelemetryDataStores();
            TelemetryDataStores = new ObservableCollection<TelemetryDataStore>(ds);
            if (TelemetryDataStores.Count > 0)
            {
                SelectedDataStore = TelemetryDataStores[0];
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load data stores: {e.Message}");
        }
    }

    #endregion

    #region Commands

    [RelayCommand(CanExecute = nameof(CanImportSessions))]
    private async Task ImportSessions()
    {
        Debug.Assert(SelectedSetup != null);
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        foreach (var telemetryFile in TelemetryFiles.Where(f => f.ShouldBeImported))
        {
            try
            {
                await httpApiService.ImportSessionAsync(telemetryFile, SelectedSetup.Value);
                telemetryFile.Imported = true;
                File.Move(telemetryFile.FullName,
                    $"{Path.GetDirectoryName(telemetryFile.FullName)}/uploaded/{telemetryFile.FileName}");
            }
            catch (HttpRequestException e)
            {
                telemetryFile.Imported = false;
                ErrorMessages.Add($"Could not import {telemetryFile.FileName}: {e.Message}");
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"Could not move {telemetryFile.Name} to uploaded: {e.Message}");
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
        return SelectedSetup != null;
    }
    
    [RelayCommand]
    private void ReloadTelemetryDataStores()
    {
        Debug.Assert(TelemetryDataStores != null, nameof(TelemetryDataStores) + " != null");
        Debug.Assert(telemetryDataStoreService != null, nameof(telemetryDataStoreService) + " != null");

        try
        {
            TelemetryDataStores.Clear();
            var ds = telemetryDataStoreService.GetTelemetryDataStores();
            foreach (var store in ds)
            {
                TelemetryDataStores.Add(store);
            }

            if (TelemetryDataStores.Count > 0)
            {
                SelectedDataStore = TelemetryDataStores[0];
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not reload data stores: {e.Message}");
        }
    }

    #endregion Commands
}