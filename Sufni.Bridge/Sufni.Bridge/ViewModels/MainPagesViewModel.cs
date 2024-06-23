using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class MainPagesViewModel : ViewModelBase
{
    #region Observable properties

    [ObservableProperty] private bool databaseLoaded;
    [ObservableProperty] private ImportSessionsViewModel importSessionsPage;
    [ObservableProperty] private SettingsViewModel settingsPage = new();
    [ObservableProperty] private int selectedIndex;
    [ObservableProperty] private bool isImportSessionsPageSelected = true;
    [ObservableProperty] private bool hasLinkages;
    [ObservableProperty] private bool hasCalibrationMethods;
    [ObservableProperty] private bool hasCalibrations;
    [ObservableProperty] private bool syncInProgress;

    [ObservableProperty] private string? setupSearchText;
    [ObservableProperty] private string? linkageSearchText;
    [ObservableProperty] private string? calibrationSearchText;
    [ObservableProperty] private string? sessionSearchText;

    [ObservableProperty] private DateTime? dateFilterFrom;
    [ObservableProperty] private DateTime? dateFilterTo;
    [ObservableProperty] private bool dateFilterVisible;

    private readonly SourceCache<LinkageViewModel, Guid> linkagesSource = new(x => x.Id);
    public ReadOnlyObservableCollection<LinkageViewModel> Linkages => linkages;
    private readonly ReadOnlyObservableCollection<LinkageViewModel> linkages;

    private readonly SourceCache<CalibrationViewModel, Guid> calibrationsSource = new(x => x.Id);
    public ReadOnlyObservableCollection<CalibrationViewModel> Calibrations => calibrations;
    private readonly ReadOnlyObservableCollection<CalibrationViewModel> calibrations;

    private readonly SourceCache<SetupViewModel, Guid> setupsSource = new(x => x.Id);
    public ReadOnlyObservableCollection<SetupViewModel> Setups => setups;
    private readonly ReadOnlyObservableCollection<SetupViewModel> setups;

    private readonly SourceCache<SessionViewModel, Guid> sessionsSource = new(x => x.Id);
    public ReadOnlyObservableCollection<SessionViewModel> Sessions => sessions;
    private readonly ReadOnlyObservableCollection<SessionViewModel> sessions;

    private ObservableCollection<CalibrationMethod> CalibrationMethods { get; } = new();
    private ObservableCollection<Board> Boards { get; } = new();

    #endregion

    #region Property change handlers

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSessionSearchTextChanged(string? value)
    {
        sessionsSource.Refresh();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDateFilterFromChanged(DateTime? value)
    {
        sessionsSource.Refresh();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDateFilterToChanged(DateTime? value)
    {
        sessionsSource.Refresh();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnLinkageSearchTextChanged(string? value)
    {
        linkagesSource.Refresh();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnCalibrationSearchTextChanged(string? value)
    {
        calibrationsSource.Refresh();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSetupSearchTextChanged(string? value)
    {
        setupsSource.Refresh();
    }

    #endregion

    #region Private members

    private readonly IDatabaseService? databaseService;

    #endregion

    #region Constructors

    public MainPagesViewModel()
    {
        databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        ImportSessionsPage = new ImportSessionsViewModel(sessionsSource);

        calibrationsSource.CountChanged.Subscribe(_ => { HasCalibrations = calibrationsSource.Count != 0; });
        linkagesSource.CountChanged.Subscribe(_ => { HasLinkages = linkagesSource.Count != 0; });
        setupsSource.CountChanged.Subscribe(_ =>
        {
            DeleteLinkageCommand.NotifyCanExecuteChanged();
            DeleteCalibrationCommand.NotifyCanExecuteChanged();
        });

        sessionsSource.Connect()
            .Filter(svm => string.IsNullOrEmpty(SessionSearchText) ||
                           (svm.Name is not null && svm.Name!.Contains(SessionSearchText,
                               StringComparison.CurrentCultureIgnoreCase)) ||
                           (svm.Description is not null && svm.Description!.Contains(SessionSearchText,
                               StringComparison.CurrentCultureIgnoreCase)))
            .Filter(svm => (DateFilterFrom is null || svm.Timestamp >= DateFilterFrom) &&
                           (DateFilterTo is null || svm.Timestamp <= DateFilterTo))
            .Sort(SortExpressionComparer<SessionViewModel>.Descending(svm => svm.Timestamp!))
            .Bind(out sessions)
            .DisposeMany()
            .Subscribe();

        linkagesSource.Connect()
            .Filter(lvm => string.IsNullOrEmpty(LinkageSearchText) ||
                           (lvm.Name is not null && lvm.Name.Contains(LinkageSearchText,
                               StringComparison.CurrentCultureIgnoreCase)))
            .Bind(out linkages)
            .DisposeMany()
            .Subscribe();

        calibrationsSource.Connect()
            .Filter(cvm => string.IsNullOrEmpty(CalibrationSearchText) ||
                           (cvm.Name is not null && cvm.Name.Contains(CalibrationSearchText,
                               StringComparison.CurrentCultureIgnoreCase)))
            .Bind(out calibrations)
            .DisposeMany()
            .Subscribe();

        setupsSource.Connect()
            .Filter(svm => string.IsNullOrEmpty(SetupSearchText) ||
                           (svm.Name is not null &&
                            svm.Name.Contains(SetupSearchText, StringComparison.CurrentCultureIgnoreCase)))
            .Bind(out setups)
            .DisposeMany()
            .Subscribe();

        SettingsPage.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(SettingsPage.IsRegistered))
            {
                return;
            }

            SyncCommand.NotifyCanExecuteChanged();
        };

        CalibrationMethods.CollectionChanged += (_, _) => { HasCalibrationMethods = CalibrationMethods.Count != 0; };

        _ = LoadDatabaseContent();
    }

    #endregion

    #region Public methods

    public async Task OnEntityAdded(ViewModelBase entity)
    {
        switch (entity)
        {
            case LinkageViewModel lvm:
                linkagesSource.AddOrUpdate(lvm);
                break;
            case CalibrationViewModel cvm:
                calibrationsSource.AddOrUpdate(cvm);
                break;
            case SetupViewModel svm:
                setupsSource.AddOrUpdate(svm);
                await ImportSessionsPage.EvaluateSetupExists();
                break;
        }
    }

    #endregion
    
    #region Private methods

    private async Task LoadLinkagesAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var linkagesList = await databaseService.GetLinkagesAsync();
            foreach (var linkage in linkagesList)
            {
                linkagesSource.AddOrUpdate(new LinkageViewModel(linkage, true));
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Linkages: {e.Message}");
        }
    }

    private async Task LoadCalibrationMethodsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var methods = await databaseService.GetCalibrationMethodsAsync();

            foreach (var method in methods)
            {
                CalibrationMethods.Add(method);
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Calibration methods: {e.Message}");
        }
    }

    private async Task LoadCalibrationsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var calibrationsList = await databaseService.GetCalibrationsAsync();
            foreach (var calibration in calibrationsList)
            {
                calibrationsSource.AddOrUpdate(new CalibrationViewModel(calibration, CalibrationMethods, true));
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Calibrations: {e.Message}");
        }
    }

    private async Task LoadBoardsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var boards = await databaseService.GetBoardsAsync();

            foreach (var board in boards)
            {
                Boards.Add(board);
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Boards: {e.Message}");
        }
    }

    private async Task LoadSetupsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var setupList = await databaseService.GetSetupsAsync();
            foreach (var setup in setupList)
            {
                var board = Boards.FirstOrDefault(b => b?.SetupId == setup.Id, null);
                var svm = new SetupViewModel(
                    setup,
                    board?.Id,
                    true,
                    linkagesSource,
                    calibrationsSource);
                svm.PropertyChanged += (sender, args) =>
                {
                    if (sender is null ||
                        args.PropertyName != nameof(SetupViewModel.IsDirty) ||
                        ((SetupViewModel)sender).IsDirty) return;
                    DeleteCalibrationCommand.NotifyCanExecuteChanged();
                    DeleteLinkageCommand.NotifyCanExecuteChanged();
                };
                setupsSource.AddOrUpdate(svm);
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Setups: {e.Message}");
        }
    }

    private async Task LoadSessionsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var sessionList = await databaseService.GetSessionsAsync();
            foreach (var session in sessionList)
            {
                sessionsSource.AddOrUpdate(new SessionViewModel(session, true));
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Sessions: {e.Message}");
        }
    }

    private async Task LoadDatabaseContent()
    {
        DatabaseLoaded = false;
        
        linkagesSource.Clear();
        CalibrationMethods.Clear();
        calibrationsSource.Clear();
        setupsSource.Clear();
        Boards.Clear();
        sessionsSource.Clear();
        await LoadLinkagesAsync();
        await LoadCalibrationMethodsAsync();
        await LoadCalibrationsAsync();
        await LoadBoardsAsync();
        await LoadSetupsAsync();
        await LoadSessionsAsync();

        DatabaseLoaded = true;
    }
    
    #endregion

    #region Commands
    
    [RelayCommand]
    private void AddLinkage()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var linkage = new Linkage(Guid.NewGuid(), "new linkage", 65, 180, 65, "");
            var lvm = new LinkageViewModel(linkage, false)
            {
                IsDirty = true
            };
            //linkagesSource.AddOrUpdate(lvm);
            
            OpenPage(lvm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Linkage: {e.Message}");
        }
    }

    private bool CanDeleteLinkage(LinkageViewModel? linkage)
    {
        return !Setups.Any(s => s.SelectedLinkage != null && s.SelectedLinkage.Id == linkage?.Id);
    }

    [RelayCommand(CanExecute = nameof(CanDeleteLinkage))]
    private async Task DeleteLinkage(LinkageViewModel linkage)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            // If this linkage was not yet saved into the database, we just need to remove it from Linkages.
            if (!linkage.IsInDatabase)
            {
                linkagesSource.Remove(linkage);
                return;
            }

            await databaseService.DeleteLinkageAsync(linkage.Id);
            linkagesSource.Remove(linkage);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Linkage: {e.Message}");
        }
    }

    [RelayCommand]
    private void AddCalibration()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var methodId = CalibrationMethods[0].Id;
            var inputs = new Dictionary<string, double>();
            foreach (var input in CalibrationMethods[0].Properties.Inputs)
            {
                inputs.Add(input, 0.0);
            }

            var calibration = new Calibration(Guid.NewGuid(), "new calibration", methodId, inputs);

            var cvm = new CalibrationViewModel(calibration, CalibrationMethods, false)
            {
                IsDirty = true
            };
            //calibrationsSource.AddOrUpdate(cvm);
            
            OpenPage(cvm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Calibration: {e.Message}");
        }
    }

    private bool CanDeleteCalibration(CalibrationViewModel? calibration)
    {
        return !Setups.Any(s =>
            (s.SelectedFrontCalibration != null && s.SelectedFrontCalibration.Id == calibration?.Id) ||
            (s.SelectedRearCalibration != null && s.SelectedRearCalibration.Id == calibration?.Id));
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCalibration))]
    private async Task DeleteCalibration(CalibrationViewModel calibration)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            // If this calibration was not yet saved into the database, we just need to remove it from Calibrations.
            if (!calibration.IsInDatabase)
            {
                calibrationsSource.Remove(calibration);
                return;
            }

            await databaseService.DeleteCalibrationAsync(calibration.Id);
            calibrationsSource.Remove(calibration);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Calibration: {e.Message}");
        }
    }

    [RelayCommand]
    private void AddSetup()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var setup = new Setup(
                Guid.NewGuid(),
                "new setup",
                Linkages[0].Id,
                null,
                null);

            // Use the SST datastore's board ID only if it's not already associated to another setup;
            string? newSetupsBoardId = null;
            var datastoreBoardId = ImportSessionsPage.SelectedDataStore?.BoardId;
            var datastoreBoard = Boards.FirstOrDefault(b => 
                b?.Id.ToLower() == datastoreBoardId && b?.SetupId is not null, null);
            if (datastoreBoard is null || datastoreBoard.SetupId is null)
            {
                newSetupsBoardId = datastoreBoardId;
            }

            var svm = new SetupViewModel(setup, newSetupsBoardId, false, linkagesSource, calibrationsSource)
            {
                IsDirty = true
            };
            svm.PropertyChanged += (sender, args) =>
            {
                if (sender is null ||
                    args.PropertyName != nameof(SetupViewModel.IsDirty) ||
                    ((SetupViewModel)sender).IsDirty) return;
                DeleteCalibrationCommand.NotifyCanExecuteChanged();
                DeleteLinkageCommand.NotifyCanExecuteChanged();
            };
            //setupsSource.AddOrUpdate(svm);
            
            OpenPage(svm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Setup: {e.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSetup(SetupViewModel setup)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            // If this setup was not yet saved into the database, we just need to remove it from Setups.
            if (!setup.IsInDatabase)
            {
                setupsSource.Remove(setup);
                return;
            }

            // If this setup is associated with a board ID, clear that association.
            if (setup.BoardId is not null)
            {
                await databaseService.PutBoardAsync(new Board(setup.BoardId, null));
            }

            await databaseService.DeleteSetupAsync(setup.Id);
            setupsSource.Remove(setup);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Setup: {e.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSession(SessionViewModel session)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            await databaseService.DeleteSessionAsync(session.Id);
            var toDelete = Sessions.First(s => s.Id == session.Id);
            sessionsSource.Remove(toDelete);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Session: {e.Message}");
        }
    }

    [RelayCommand]
    private void ClearSearchText(string which)
    {
        switch (which)
        {
            case "linkage":
                LinkageSearchText = null;
                break;
            case "calibration":
                CalibrationSearchText = null;
                break;
            case "setup":
                SetupSearchText = null;
                break;
            case "session":
                SessionSearchText = null;
                break;
        }
    }

    [RelayCommand]
    private void ClearDateFilter(string which)
    {
        switch (which)
        {
            case "from":
                DateFilterFrom = null;
                break;
            case "to":
                DateFilterTo = null;
                break;
        }
    }

    [RelayCommand]
    private void ToggleDateFilter()
    {
        DateFilterVisible = !DateFilterVisible;
    }

    private bool CanSync()
    {
        return SettingsPage.IsRegistered;
    }

    private async Task PushLocalChanges(int lastSyncTime, IHttpApiService httpApiService)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        var changes = new SynchronizationData
        {
            Boards = await databaseService.GetChangedBoardsAsync(lastSyncTime),
            CalibrationMethods = await databaseService.GetChangedCalibrationMethodsAsync(lastSyncTime),
            Calibrations = await databaseService.GetChangedCalibrationsAsync(lastSyncTime),
            Linkages = await databaseService.GetChangedLinkagesAsync(lastSyncTime),
            Setups = await databaseService.GetChangedSetupsAsync(lastSyncTime),
            Sessions = await databaseService.GetChangedSessionsAsync(lastSyncTime)
        };
        await httpApiService.PushSyncAsync(changes);
    }

    private async Task PushIncompleteSessions(IHttpApiService httpApiService)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        var incompleteSessions = await httpApiService.GetIncompleteSessionIdsAsync();
        foreach (var id in incompleteSessions)
        {
            var psst = await databaseService.GetSessionRawPsstAsync(id);
            if (psst is not null)
            {
                await httpApiService.PatchSessionPsstAsync(id, psst);
            }
        }
    }

    private async Task PullRemoteChanges(int lastSyncTime, IHttpApiService httpApiService)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        var syncData = await httpApiService.PullSyncAsync(lastSyncTime);
        foreach (var board in syncData.Boards)
        {
            if (board.Deleted.HasValue)
            {
                await databaseService.DeleteBoardAsync(board.Id);
            }
            else
            {
                await databaseService.PutBoardAsync(board);
            }
        }

        foreach (var calibrationMethod in syncData.CalibrationMethods)
        {
            if (calibrationMethod.Deleted.HasValue)
            {
                await databaseService.DeleteCalibrationMethodAsync(calibrationMethod.Id);
            }
            else
            {
                await databaseService.PutCalibrationMethodAsync(calibrationMethod);
            }
        }

        foreach (var calibration in syncData.Calibrations)
        {
            if (calibration.Deleted.HasValue)
            {
                await databaseService.DeleteCalibrationAsync(calibration.Id);
            }
            else
            {
                await databaseService.PutCalibrationAsync(calibration);
            }
        }

        foreach (var linkage in syncData.Linkages)
        {
            if (linkage.Deleted.HasValue)
            {
                await databaseService.DeleteLinkageAsync(linkage.Id);
            }
            else
            {
                await databaseService.PutLinkageAsync(linkage);
            }
        }

        foreach (var setup in syncData.Setups)
        {
            if (setup.Deleted.HasValue)
            {
                await databaseService.DeleteSetupAsync(setup.Id);
            }
            else
            {
                await databaseService.PutSetupAsync(setup);
            }
        }

        foreach (var session in syncData.Sessions)
        {
            if (session.Deleted.HasValue)
            {
                await databaseService.DeleteSessionAsync(session.Id);
            }
            else
            {
                await databaseService.PutSessionAsync(session);
            }
        }
    }

    private async Task PullIncompleteSessions(IHttpApiService httpApiService)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        var incompleteSessionIds = await databaseService.GetIncompleteSessionIdsAsync();
        foreach (var id in incompleteSessionIds)
        {
            var psst = await httpApiService.GetSessionPsstAsync(id);
            if (psst is not null)
            {
                await databaseService.PatchSessionPsstAsync(id, psst);
            }
        }
    }

    private async void SyncInternal()
    {
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        SyncInProgress = true;

        try
        {
            var lastSyncTime = await databaseService.GetLastSyncTimeAsync();

            await PushLocalChanges(lastSyncTime, httpApiService);
            await PullRemoteChanges(lastSyncTime, httpApiService);
            await PushIncompleteSessions(httpApiService);
            await PullIncompleteSessions(httpApiService);
            
            await databaseService.UpdateLastSyncTimeAsync();

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await LoadDatabaseContent();

                Notifications.Add("Sync successful");
                ErrorMessages.Clear();
            });
        }
        catch (Exception e)
        {
            Dispatcher.UIThread.Post(() =>
            {
                ErrorMessages.Add($"Sync failed: {e.Message}");
            });
        }

        SyncInProgress = false;
    }
    
    [RelayCommand(CanExecute = nameof(CanSync))]
    private void Sync()
    {
        new Thread(SyncInternal).Start();
    }

    [RelayCommand]
    private void ShowConnectPage()
    {
        var mainViewModel = App.Current?.Services?.GetService<MainViewModel>();
        Debug.Assert(mainViewModel != null, nameof(mainViewModel) + " != null");
        mainViewModel.OpenView(SettingsPage);
    }
    
    [RelayCommand]
    private void ShowImportPage()
    {
        var mainViewModel = App.Current?.Services?.GetService<MainViewModel>();
        Debug.Assert(mainViewModel != null, nameof(mainViewModel) + " != null");
        mainViewModel.OpenView(ImportSessionsPage);
    }
    
    #endregion
}