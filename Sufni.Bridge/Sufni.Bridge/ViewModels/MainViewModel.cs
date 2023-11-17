using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Observable properties
    
    [ObservableProperty] private ImportSessionsViewModel importSessionsPage;
    [ObservableProperty] private SettingsViewModel settingsPage = new();
    [ObservableProperty] private int selectedIndex;
    [ObservableProperty] private bool isImportSessionsPageSelected = true;
    [ObservableProperty] private bool hasLinkages;
    [ObservableProperty] private bool hasCalibrationMethods;
    [ObservableProperty] private bool hasCalibrations;

    public ObservableCollection<LinkageViewModel> Linkages { get; } = new();
    public ObservableCollection<CalibrationViewModel> Calibrations { get; } = new();
    public ObservableCollection<SetupViewModel> Setups { get; } = new();
    public ObservableCollection<SessionViewModel> Sessions { get; } = new();
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

    //private readonly IHttpApiService? httpApiService;
    private readonly IDatabaseService? databaseService;

    #endregion
    
    #region Constructors

    public MainViewModel()
    {
        databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        ImportSessionsPage = new ImportSessionsViewModel(Sessions);
        
        SelectPage();
        SettingsPage.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(SettingsPage.IsRegistered))
            {
                return;
            }

            SelectPage();
        };

        Linkages.CollectionChanged += (_, _) =>
        {
            HasLinkages = Linkages.Count != 0;
        };
        CalibrationMethods.CollectionChanged += (_, _) =>
        {
            HasCalibrationMethods = CalibrationMethods.Count != 0;
        };
        Calibrations.CollectionChanged += (_, _) =>
        {
            HasCalibrations = Calibrations.Count != 0;
        };
        Setups.CollectionChanged += (_, _) =>
        {
            DeleteLinkageCommand.NotifyCanExecuteChanged();
            DeleteCalibrationCommand.NotifyCanExecuteChanged();
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
            var linkages = await databaseService.GetLinkagesAsync();

            foreach (var linkage in linkages)
            {
                Linkages.Add(new LinkageViewModel(linkage));
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
            var calibrations = await databaseService.GetCalibrationsAsync();

            foreach (var calibration in calibrations)
            {
                Calibrations.Add(new CalibrationViewModel(calibration, CalibrationMethods));
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
            var setups = await databaseService.GetSetupsAsync();

            foreach (var setup in setups)
            {
                var board = Boards.FirstOrDefault(b => b?.SetupId == setup.Id, null);
                var svm = new SetupViewModel(
                    setup,
                    board?.Id,
                    Linkages,
                    Calibrations);
                svm.PropertyChanged += (sender, args) =>
                {
                    if (sender is null ||
                        args.PropertyName != nameof(SetupViewModel.IsDirty) ||
                        ((SetupViewModel)sender).IsDirty) return;
                    DeleteCalibrationCommand.NotifyCanExecuteChanged();
                    DeleteLinkageCommand.NotifyCanExecuteChanged();
                };
                Setups.Add(svm);
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
            var sessions = await databaseService.GetSessionsAsync();

            foreach (var session in sessions)
            {
                Sessions.Add(new SessionViewModel(session));
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Sessions: {e.Message}");
        }
    }

    private async Task LoadDatabaseContent()
    {
        Linkages.Clear();
        CalibrationMethods.Clear();
        Calibrations.Clear();
        Setups.Clear();
        Boards.Clear();
        Sessions.Clear();
        await LoadLinkagesAsync();
        await LoadCalibrationMethodsAsync();
        await LoadCalibrationsAsync();
        await LoadBoardsAsync();
        await LoadSetupsAsync();
        await LoadSessionsAsync();
    }
    
    #endregion

    #region Commands

    private bool CanUploadSessions()
    {
        return SettingsPage.IsRegistered;
    }
    
    [RelayCommand(CanExecute = nameof(CanUploadSessions))]
    private void UploadSessions()
    {
        // TODO: implement
        ErrorMessages.Add("Session upload not yet implemented!");
    }

    [RelayCommand]
    private void AddLinkage()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var linkage = new Linkage(null, "new linkage", 65, 180, 65, "");
            var lvm = new LinkageViewModel(linkage)
            {
                IsDirty = true
            };
            Linkages.Add(lvm);
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
            if (linkage.Id is null)
            {
                Linkages.Remove(linkage);
                return;
            }
            
            await databaseService.DeleteLinkageAsync(linkage.Id.Value);
            Linkages.Remove(linkage);
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
            var calibration = new Calibration(null, "new calibration", methodId, inputs);

            var cvm = new CalibrationViewModel(calibration, CalibrationMethods)
            {
                IsDirty = true
            };
            Calibrations.Add(cvm);
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
            if (calibration.Id is null)
            {
                Calibrations.Remove(calibration);
                return;
            }
            
            await databaseService.DeleteCalibrationAsync(calibration.Id.Value);
            Calibrations.Remove(calibration);
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
                null,
                "new setup",
                Linkages[0].Id!.Value,
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
            
            var svm = new SetupViewModel(setup, newSetupsBoardId, Linkages, Calibrations)
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
            Setups.Add(svm);
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
            if (setup.Id is null)
            {
                Setups.Remove(setup);
                return;
            }

            // If this setup is associated with a board ID, clear that association.
            if (setup.BoardId is not null)
            {
                await databaseService.PutBoardAsync(new Board(setup.BoardId, null));
            }
            
            await databaseService.DeleteSetupAsync(setup.Id.Value);
            Setups.Remove(setup);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Setup: {e.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSession(int id)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        try
        {
            await databaseService.DeleteSessionAsync(id);
            var toDelete = Sessions.First(s => s.Id == id);
            Sessions.Remove(toDelete);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Session: {e.Message}");
        }
    }
    
    #endregion
}