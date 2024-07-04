using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Sufni.Bridge.ViewModels.Items;

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
    [ObservableProperty] private ItemViewModelBase? lastDeletedItem;

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

    private ObservableCollection<CalibrationMethod> CalibrationMethods { get; } = [];
    private ObservableCollection<Board> Boards { get; } = [];

    public ObservableCollection<PullMenuItemViewModel> SessionsMenuItems { get; set; } = [];
    public ObservableCollection<PullMenuItemViewModel> LinkagesMenuItems { get; set; } = [];
    public ObservableCollection<PullMenuItemViewModel> CalibrationsMenuItems { get; set; } = [];
    public ObservableCollection<PullMenuItemViewModel> SetupsMenuItems { get; set; } = [];

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

        SessionsMenuItems.Add(new("sync", SyncCommand));
        SessionsMenuItems.Add(new("import", OpenPageCommand, ImportSessionsPage));

        LinkagesMenuItems.Add(new("sync", SyncCommand));
        LinkagesMenuItems.Add(new("add", AddLinkageCommand));

        CalibrationsMenuItems.Add(new("sync", SyncCommand));
        CalibrationsMenuItems.Add(new("add", AddCalibrationCommand));

        SetupsMenuItems.Add(new("sync", SyncCommand));
        SetupsMenuItems.Add(new("add", AddSetupCommand));

        _ = LoadDatabaseContent();
    }

    #endregion

    #region Public methods

    public async Task OnEntityAdded(ItemViewModelBase item)
    {
        switch (item)
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

    public async Task DeleteItem(ItemViewModelBase item)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        switch (item)
        {
            case LinkageViewModel lvm:
                linkagesSource.Remove(lvm);
                await databaseService.DeleteLinkageAsync(lvm.Id);
                break;
            case CalibrationViewModel cvm:
                calibrationsSource.Remove(cvm);
                await databaseService.DeleteCalibrationAsync(cvm.Id);
                break;
            case SetupViewModel svm:
                // If this setup is associated with a board ID, clear that association.
                if (svm.BoardId is not null)
                {
                    await databaseService.PutBoardAsync(new Board(svm.BoardId, null));
                }

                // Delete setup
                setupsSource.Remove(svm);
                await databaseService.DeleteSetupAsync(svm.Id);

                // Notify associated calibrations and linkages about the deletion
                svm.SelectedFrontCalibration?.DeleteCommand.NotifyCanExecuteChanged();
                svm.SelectedRearCalibration?.DeleteCommand.NotifyCanExecuteChanged();
                svm.SelectedLinkage?.DeleteCommand.NotifyCanExecuteChanged();
                svm.SelectedFrontCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
                svm.SelectedRearCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
                svm.SelectedLinkage?.FakeDeleteCommand.NotifyCanExecuteChanged();
                break;
            case SessionViewModel svm:
                sessionsSource.Remove(svm);
                await databaseService.DeleteSessionAsync(svm.Id);
                break;
        }
    }

    public void UndoableDelete(ItemViewModelBase item)
    {
        LastDeletedItem = item;
        switch (item)
        {
            case LinkageViewModel lvm:
                linkagesSource.Remove(lvm);
                break;
            case CalibrationViewModel cvm:
                calibrationsSource.Remove(cvm);
                break;
            case SetupViewModel svm:
                setupsSource.Remove(svm);
                break;
            case SessionViewModel svm:
                sessionsSource.Remove(svm);
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

    private void OnSetupDirtinessChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetupViewModel.IsDirty) &&
            sender is SetupViewModel svm &&
            !svm.IsDirty)
        {
            svm.SelectedFrontCalibration?.DeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedRearCalibration?.DeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedLinkage?.DeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedFrontCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedRearCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedLinkage?.FakeDeleteCommand.NotifyCanExecuteChanged();
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
                svm.PropertyChanged += OnSetupDirtinessChanged;
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

            OpenPage(lvm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Linkage: {e.Message}");
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

            OpenPage(cvm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Calibration: {e.Message}");
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
            svm.PropertyChanged += OnSetupDirtinessChanged;

            OpenPage(svm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Setup: {e.Message}");
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

    private async void SyncInternal()
    {
        var synchronizationService = App.Current?.Services?.GetService<ISynchronizationService>();
        Debug.Assert(synchronizationService != null, nameof(synchronizationService) + " != null");

        SyncInProgress = true;

        try
        {
            await synchronizationService.SyncAll();
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

    [RelayCommand]
    public async Task FinalizeDelete()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        switch (LastDeletedItem)
        {
            case LinkageViewModel lvm:
                await databaseService.DeleteLinkageAsync(lvm.Id);
                break;
            case CalibrationViewModel cvm:
                await databaseService.DeleteCalibrationAsync(cvm.Id);
                break;
            case SetupViewModel svm:
                await databaseService.DeleteSetupAsync(svm.Id);
                break;
            case SessionViewModel svm:
                await databaseService.DeleteSessionAsync(svm.Id);
                break;
        }

        LastDeletedItem = null;
    }

    [RelayCommand]
    public void UndoDelete()
    {
        switch (LastDeletedItem)
        {
            case LinkageViewModel lvm:
                linkagesSource.AddOrUpdate(lvm);
                break;
            case CalibrationViewModel cvm:
                calibrationsSource.AddOrUpdate(cvm);
                break;
            case SetupViewModel svm:
                setupsSource.AddOrUpdate(svm);
                break;
            case SessionViewModel svm:
                sessionsSource.AddOrUpdate(svm);
                break;
        }

        LastDeletedItem = null;
    }

    #endregion
}