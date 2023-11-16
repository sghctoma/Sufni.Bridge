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
    
    [ObservableProperty] private ImportSessionsViewModel importSessionsPage = new();
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
    public ObservableCollection<CalibrationMethod> CalibrationMethods { get; } = new();

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
        Settings = 2,
        /*
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

        _ = Reload();
    }

    #endregion

    #region Private methods

    private void SelectPage()
    {
        if (SettingsPage.IsRegistered)
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
        else
        {
            SelectedIndex = (int)PageIndices.Settings;
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

    private async Task LoadSetupsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        try
        {
            var setups = await databaseService.GetSetupsAsync();
            var boards = await databaseService.GetBoardsAsync();

            foreach (var setup in setups)
            {
                var board = boards.FirstOrDefault(b => b?.SetupId == setup.Id, null);
                var svm = new SetupViewModel(
                    setup,
                    board?.Id,
                    Linkages,
                    Calibrations);
                svm.PropertyChanged += (sender, args) =>
                {
                    if (sender is not null &&
                        args.PropertyName == nameof(SetupViewModel.IsDirty) &&
                        !((SetupViewModel)sender).IsDirty)
                    {
                        DeleteCalibrationCommand.NotifyCanExecuteChanged();
                        DeleteLinkageCommand.NotifyCanExecuteChanged();
                    }
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

    #endregion

    #region Commands
    
    [RelayCommand]
    private async Task Reload()
    {
        Linkages.Clear();
        CalibrationMethods.Clear();
        Calibrations.Clear();
        Setups.Clear();
        Sessions.Clear();
        await LoadLinkagesAsync();
        await LoadCalibrationMethodsAsync();
        await LoadCalibrationsAsync();
        await LoadSetupsAsync();
        await LoadSessionsAsync();
    }

    [RelayCommand]
    private async Task AddLinkage()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var linkage = new Linkage(null, "new linkage", 65, 180, 65, "");
            await databaseService.PutLinkageAsync(linkage);
            Linkages.Add(new LinkageViewModel(linkage));
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Linkage: {e.Message}");
        }
    }

    private bool CanDeleteLinkage(int id)
    {
        return !Setups.Any(s => s.SelectedLinkage != null && s.SelectedLinkage.Id == id);
    }
    
    [RelayCommand(CanExecute = nameof(CanDeleteLinkage))]
    private async Task DeleteLinkage(int id)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        try
        {
            await databaseService.DeleteLinkageAsync(id);
            var toDelete = Linkages.First(l => l.Id == id);
            Linkages.Remove(toDelete);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Linkage: {e.Message}");
        }
    }

    [RelayCommand]
    private async Task AddCalibration()
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
        
            await databaseService.PutCalibrationAsync(calibration);
            Calibrations.Add(new CalibrationViewModel(calibration, CalibrationMethods));
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Calibration: {e.Message}");
        }
    }
    
    private bool CanDeleteCalibration(int id)
    {
        return !Setups.Any(s => 
            (s.SelectedFrontCalibration != null && s.SelectedFrontCalibration.Id == id) ||
            (s.SelectedRearCalibration != null && s.SelectedRearCalibration.Id == id));
    }
    
    [RelayCommand(CanExecute = nameof(CanDeleteCalibration))]
    private async Task DeleteCalibration(int id)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        try
        {
            await databaseService.DeleteCalibrationAsync(id);
            var toDelete = Calibrations.First(c => c.Id == id);
            Calibrations.Remove(toDelete);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Calibration: {e.Message}");
        }
    }
    
    [RelayCommand]
    private async Task AddSetup()
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
        
            await databaseService.PutSetupAsync(setup);
            await databaseService.PutBoardAsync(new Board(
                ImportSessionsPage.SelectedDataStore?.BoardId!,
                setup.Id));

            var svm = new SetupViewModel(
                setup,
                ImportSessionsPage.SelectedDataStore?.BoardId,
                Linkages,
                Calibrations);
            svm.PropertyChanged += (sender, args) =>
            {
                if (sender is not null &&
                    args.PropertyName == nameof(SetupViewModel.IsDirty) &&
                    !((SetupViewModel)sender).IsDirty)
                {
                    DeleteCalibrationCommand.NotifyCanExecuteChanged();
                    DeleteLinkageCommand.NotifyCanExecuteChanged();
                }
            };
            Setups.Add(svm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Setup: {e.Message}");
        }
    }
    
    [RelayCommand]
    private async Task DeleteSetup(int id)
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        try
        {
            var toDelete = Setups.First(s => s.Id == id);

            // If this setup is associated with a board ID, clear the setup ID from that board.
            var boards = await databaseService.GetBoardsAsync();
            var associatedBoard = boards.FirstOrDefault(b => b!.Id == toDelete.BoardId, null);
            if (associatedBoard is not null)
            {
                associatedBoard.SetupId = null;
                await databaseService.PutBoardAsync(associatedBoard);
            }
            
            await databaseService.DeleteSetupAsync(id);
            Setups.Remove(toDelete);
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