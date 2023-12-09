using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Avalonia.Threading;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.ViewModels;

public partial class ImportSessionsViewModel : ViewModelBase
{
    #region Public properties

    public string ImportLabel => "Import Selected";

    #endregion Public properties

    #region Observable properties

    public ObservableCollection<ITelemetryDataStore>? TelemetryDataStores { get; set; }
    public ObservableCollection<ITelemetryFile> TelemetryFiles { get; } = new();
    private readonly SourceCache<SessionViewModel, Guid> sessions;

    [ObservableProperty] private ITelemetryDataStore? selectedDataStore;
    [ObservableProperty] private bool newDataStoresAvailable;
    [ObservableProperty] private bool importInProgress;

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
            SelectedSetup = null;
            return;
        }

        // Need to clear the DataStoresAvailable flag so that the notification does not show
        // up when the first datastore appears and auto-selected.
        ClearNewDataStoresAvailable();
        
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
        var files = await value.GetFiles();
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
    public ImportSessionsViewModel() : this(new SourceCache<SessionViewModel, Guid>(m => m.Guid)) {}
    
    public ImportSessionsViewModel(SourceCache<SessionViewModel, Guid> sessions)
    {
        databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        var telemetryDataStoreService = App.Current?.Services?.GetService<ITelemetryDataStoreService>();

        this.sessions = sessions;
        
        Debug.Assert(databaseService != null, nameof(telemetryDataStoreService) + " != null");
        Debug.Assert(telemetryDataStoreService != null, nameof(telemetryDataStoreService) + " != null");
        
        TelemetryDataStores = telemetryDataStoreService.DataStores;
        TelemetryDataStores.CollectionChanged += (_, e) =>
        {
            var comparer = new TelemetryDataStoreComparer();
            var removed = (ITelemetryDataStore)e.OldItems?[0]!;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        NewDataStoresAvailable = true;
                        SelectedDataStore ??= TelemetryDataStores[0];
                    });
                    break;
                case NotifyCollectionChangedAction.Remove:
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        if (TelemetryDataStores.Count == 0 || !comparer.Equals(SelectedDataStore, removed)) return;
                        // XXX: The files from the correct datastore show up, but the ComboBox won't show the datastore
                        //      as selected. Probably has something to do with this fix, since it only handle adds:
                        //      https://github.com/AvaloniaUI/Avalonia/pull/4593/commits/8dfc65d17be00b7f7c96c294dabe7616916951b2
                        SelectedDataStore = TelemetryDataStores[^1];
                    });
                    break;
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                case NotifyCollectionChangedAction.Reset:
                    return;
            }
        };
        if (TelemetryDataStores.Count > 0)
        {
            SelectedDataStore = TelemetryDataStores[0];
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
        Debug.Assert(SelectedDataStore != null);
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        ImportInProgress = true;
        
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

                var psst = await telemetryFile.GeneratePsstAsync(linkageBytes, calibrationsBytes);
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
                sessions.AddOrUpdate(svm);
                
                telemetryFile.OnImported();
                Notifications.Insert(0, $"{svm.Name} was successfully imported.");
            }
            catch (Exception e)
            {
                ErrorMessages.Add($"Could not import {telemetryFile.Name}: {e.Message}");
            }
        }
        
        var files = await SelectedDataStore.GetFiles();
        TelemetryFiles.Clear();
        foreach (var file in files)
        {
            TelemetryFiles.Add(file);
        }

        ImportInProgress = false;
    }

    private bool CanImportSessions()
    {
        return SelectedSetup != null;
    }

    [RelayCommand]
    private void ClearNewDataStoresAvailable()
    {
        NewDataStoresAvailable = false;
    }
    
    #endregion Commands
}