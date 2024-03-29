﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Observable properties

    [ObservableProperty] private Thickness? safeAreaPadding;
    [ObservableProperty] private ImportSessionsViewModel importSessionsPage;
    [ObservableProperty] private SettingsViewModel settingsPage = new();
    [ObservableProperty] private int selectedIndex;
    [ObservableProperty] private bool isImportSessionsPageSelected = true;
    [ObservableProperty] private bool hasLinkages;
    [ObservableProperty] private bool hasCalibrationMethods;
    [ObservableProperty] private bool hasCalibrations;
    [ObservableProperty] private bool sessionUploadInProgress;
    
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
    
    private readonly SourceCache<SessionViewModel, Guid> sessionsSourceCache = new(x => x.Id);
    public ReadOnlyObservableCollection<SessionViewModel> Sessions => sessions;
    private readonly ReadOnlyObservableCollection<SessionViewModel> sessions;
    
    private ObservableCollection<CalibrationMethod> CalibrationMethods { get; } = new();
    private ObservableCollection<Board> Boards { get; } = new();

    #endregion

    #region Property change handlers

    partial void OnSelectedIndexChanged(int value)
    {
        if (value == (int)PageIndices.ImportSessions)
        {
            IsImportSessionsPageSelected = true;
            _ = ImportSessionsPage.EvaluateSetupExists();
        }
        else
        {
            IsImportSessionsPageSelected = false;
        }
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSessionSearchTextChanged(string? value)
    {
        sessionsSourceCache.Refresh();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDateFilterFromChanged(DateTime? value)
    {
        sessionsSourceCache.Refresh();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDateFilterToChanged(DateTime? value)
    {
        sessionsSourceCache.Refresh();
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
    
    private enum PageIndices
    {
        ImportSessions = 0,
        Sessions = 1,
        /*
        Settings = 2,
        Linkages = 2,
        Calibrations = 3,
        BikeSetups = 4,
        */
    }

    private readonly IDatabaseService? databaseService;

    #endregion
    
    #region Constructors

    public MainViewModel()
    {
        SafeAreaPadding = App.Current?.SafeAreaPadding;
        databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        ImportSessionsPage = new ImportSessionsViewModel(sessionsSourceCache);

        calibrationsSource.CountChanged.Subscribe(_ => { HasCalibrations = calibrationsSource.Count != 0; });
        linkagesSource.CountChanged.Subscribe(_ => { HasLinkages = linkagesSource.Count != 0; });
        setupsSource.CountChanged.Subscribe(_ =>
        {
            DeleteLinkageCommand.NotifyCanExecuteChanged();
            DeleteCalibrationCommand.NotifyCanExecuteChanged();
        });

        sessionsSourceCache.Connect()
            .Filter(svm => string.IsNullOrEmpty(SessionSearchText) || 
                           (svm.Name is not null && svm.Name!.Contains(SessionSearchText, StringComparison.CurrentCultureIgnoreCase)) ||
                           (svm.Description is not null && svm.Description!.Contains(SessionSearchText, StringComparison.CurrentCultureIgnoreCase)))
            .Filter(svm => (DateFilterFrom is null || svm.Timestamp >= DateFilterFrom) &&
                           (DateFilterTo is null || svm.Timestamp <= DateFilterTo))
            .Sort(SortExpressionComparer<SessionViewModel>.Descending(svm => svm.Timestamp!))
            .Bind(out sessions)
            .DisposeMany()
            .Subscribe();
        
        linkagesSource.Connect()
            .Filter(lvm => string.IsNullOrEmpty(LinkageSearchText) ||
                           (lvm.Name is not null && lvm.Name.Contains(LinkageSearchText, StringComparison.CurrentCultureIgnoreCase)))
            .Bind(out linkages)
            .DisposeMany()
            .Subscribe();
        
        calibrationsSource.Connect()
            .Filter(cvm => string.IsNullOrEmpty(CalibrationSearchText) ||
                           (cvm.Name is not null && cvm.Name.Contains(CalibrationSearchText, StringComparison.CurrentCultureIgnoreCase)))
            .Bind(out calibrations)
            .DisposeMany()
            .Subscribe();
        
        setupsSource.Connect()
            .Filter(svm => string.IsNullOrEmpty(SetupSearchText) ||
                           (svm.Name is not null && svm.Name.Contains(SetupSearchText, StringComparison.CurrentCultureIgnoreCase)))
            .Bind(out setups)
            .DisposeMany()
            .Subscribe();
        
        SelectPage();
        SettingsPage.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(SettingsPage.IsRegistered))
            {
                return;
            }

            UploadSessionsCommand.NotifyCanExecuteChanged();
            SelectPage();
        };
        
        CalibrationMethods.CollectionChanged += (_, _) =>
        {
            HasCalibrationMethods = CalibrationMethods.Count != 0;
        };

        _ = LoadDatabaseContent();
    }

    #endregion

    #region Private methods

    private void SelectPage()
    {
        if (ImportSessionsPage.SelectedDataStore is not null)
        {
            SelectedIndex = (int)PageIndices.ImportSessions;
        }
        else
        {
            SelectedIndex = (int)PageIndices.Sessions;
        }
    }
    
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
                sessionsSourceCache.AddOrUpdate(new SessionViewModel(session, true));
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Sessions: {e.Message}");
        }
    }

    private async Task LoadDatabaseContent()
    {
        linkagesSource.Clear();
        CalibrationMethods.Clear();
        calibrationsSource.Clear();
        setupsSource.Clear();
        Boards.Clear();
        sessionsSourceCache.Clear();
        await LoadLinkagesAsync();
        await LoadCalibrationMethodsAsync();
        await LoadCalibrationsAsync();
        await LoadBoardsAsync();
        await LoadSetupsAsync();
        await LoadSessionsAsync();
    }

    private async Task ReloadAfterSync()
    {
        await LoadLinkagesAsync();
        await LoadCalibrationMethodsAsync();
        await LoadCalibrationsAsync();
        await LoadBoardsAsync();
        await LoadSetupsAsync();
    }
    
    #endregion

    #region Commands

    private bool CanUploadSessions()
    {
        return SettingsPage.IsRegistered;
    }
    
    [RelayCommand(CanExecute = nameof(CanUploadSessions))]
    private async Task UploadSessions()
    {
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        Debug.Assert(SettingsPage.IsRegistered, "SettingsPage.IsRegistered");

        SessionUploadInProgress = true;
        
        List<Session> remoteSessions;
        try
        {
            remoteSessions = await httpApiService.GetSessionsAsync();
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Remote sessions could not be loaded: {e.Message}");
            return;
        }
        
        foreach (var svm in Sessions)
        {
            try
            {
                var timestamp = ((DateTimeOffset)svm.Timestamp!).ToUnixTimeSeconds();
                if (remoteSessions.Any(s => s.Timestamp == timestamp))
                {
                    continue;
                }

                var psst = await databaseService.GetSessionRawPsstAsync(svm.Id);
                await httpApiService.PutProcessedSessionAsync(svm.Name!, svm.Description!, psst!);
                Notifications.Insert(0, $"{svm.Name} was successfully uploaded.");
            }
            catch(Exception e)
            {
                ErrorMessages.Add($"Session \"{svm.Name}\" could not be uploaded: {e.Message}");
            }
        }

        SessionUploadInProgress = false;
    }

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
            linkagesSource.AddOrUpdate(lvm);
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
            calibrationsSource.AddOrUpdate(cvm);
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
            var datastoreBoard = Boards.FirstOrDefault(b => b?.Id == datastoreBoardId, null);
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
            setupsSource.AddOrUpdate(svm);
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
    private async Task DeleteSession(Guid id)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        try
        {
            await databaseService.DeleteSessionAsync(id);
            var toDelete = Sessions.First(s => s.Id == id);
            sessionsSourceCache.Remove(toDelete);
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
    
    [RelayCommand]
    private async Task Sync()
    {
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        try
        {
            var lastSyncTime = await databaseService.GetLastSyncTimeAsync();
            var changes = new SynchronizationData
            {
                Boards = await databaseService.GetChangedBoardsAsync(lastSyncTime),
                CalibrationMethods = await databaseService.GetChangedCalibrationMethodsAsync(lastSyncTime),
                Calibrations = await databaseService.GetChangedCalibrationsAsync(lastSyncTime),
                Linkages = await databaseService.GetChangedLinkagesAsync(lastSyncTime),
                Setups = await databaseService.GetChangedSetupsAsync(lastSyncTime),
            };
            await httpApiService.PushSyncAsync(changes);
            
            var syncData = await httpApiService.PullSyncAsync();
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

            await databaseService.UpdateLastSyncTimeAsync();
            await ReloadAfterSync();
            
            Notifications.Add("Sync successful");
            ErrorMessages.Clear();
        }
        catch(Exception e)
        {
            ErrorMessages.Add($"Could not sync: {e.Message}");
        }
    }

    #endregion
}