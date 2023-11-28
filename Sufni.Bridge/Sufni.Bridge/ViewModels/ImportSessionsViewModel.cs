using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Sufni.Bridge.ViewModels;

public partial class ImportSessionsViewModel : ViewModelBase
{
    #region Public properties

    public string ImportLabel => "Import Selected";

    #endregion Public properties

    #region Observable properties

    public ObservableCollection<ITelemetryDataStore>? TelemetryDataStores { get; set; }
    public ObservableCollection<ITelemetryFile> TelemetryFiles { get; } = new();
    private readonly ObservableCollection<SessionViewModel> sessions;

    [ObservableProperty] private ITelemetryDataStore? selectedDataStore;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ImportSessionsCommand))]
    private int? selectedSetup;
    
    #endregion Observable properties

    #region Property change handlers

    async partial void OnSelectedDataStoreChanged(ITelemetryDataStore? value)
    {           
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        if (value == null)
        {
            TelemetryFiles.Clear();
            return;
        }

        try
        {
            var boards = await databaseService.GetBoardsAsync();
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

    private readonly IDatabaseService? databaseService;

    #endregion Private members
    
    #region Constructors

    // This is only here for the designer
    public ImportSessionsViewModel() : this(new ObservableCollection<SessionViewModel>()) {}
    
    public ImportSessionsViewModel(ObservableCollection<SessionViewModel> sessions)
    {
        databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        var telemetryDataStoreService = App.Current?.Services?.GetService<ITelemetryDataStoreService>();

        this.sessions = sessions;
        
        Debug.Assert(databaseService != null, nameof(telemetryDataStoreService) + " != null");
        Debug.Assert(telemetryDataStoreService != null, nameof(telemetryDataStoreService) + " != null");

        try
        {
            TelemetryDataStores = telemetryDataStoreService.DataStores;
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
    
    #region Public methods

    public async Task EvaluateSetupExists()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        var boards = await databaseService.GetBoardsAsync();
        var selectedBoard = boards.FirstOrDefault(b => b?.Id == SelectedDataStore?.BoardId, null);
        SelectedSetup = selectedBoard?.SetupId;
    }

    #endregion
    
    #region Commands

    [RelayCommand(CanExecute = nameof(CanImportSessions))]
    private async Task ImportSessions()
    {
        Debug.Assert(SelectedSetup != null);
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        foreach (var telemetryFile in TelemetryFiles.Where(f => f.ShouldBeImported))
        {
            try
            {
                var setup = await databaseService.GetSetupAsync(SelectedSetup!.Value);
                var linkage = await databaseService.GetLinkageAsync(setup!.LinkageId);
                var fcal = await databaseService.GetCalibrationAsync(setup.FrontCalibrationId ?? 0);
                var fmethod = fcal is null ? null : await databaseService.GetCalibrationMethodAsync(fcal.MethodId);
                var rcal = await databaseService.GetCalibrationAsync(setup.RearCalibrationId ?? 0);
                var rmethod = rcal is null ? null : await databaseService.GetCalibrationMethodAsync(rcal.MethodId);

                var linkageBytes = JsonSerializer.SerializeToUtf8Bytes(linkage);
                var calibrationsBytes = Encoding.UTF8.GetBytes(Calibration.GetCalibrationsJson(
                    fcal, fmethod, rcal, rmethod));

                var psst = telemetryFile.GeneratePsst(linkageBytes, calibrationsBytes);
                var session = new Session(
                    id: null,
                    name: telemetryFile.Name,
                    description: telemetryFile.Description,
                    setup: SelectedSetup ?? 0,
                    timestamp: (int)((DateTimeOffset)telemetryFile.StartTime).ToUnixTimeSeconds())
                    {
                        ProcessedData = psst
                    };

                await databaseService.PutSessionAsync(session);
                
                var svm = new SessionViewModel(session);
                var index = 0;
                while (index < sessions.Count && svm.Timestamp < sessions[index].Timestamp)
                {
                    ++index;
                }
                sessions.Insert(index, svm);
                
                telemetryFile.OnImported();
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"Could not import {telemetryFile.Name}: {e.Message}");
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
    
    #endregion Commands
}