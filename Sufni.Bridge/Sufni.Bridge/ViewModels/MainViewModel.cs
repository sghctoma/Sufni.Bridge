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
    public ObservableCollection<CalibrationMethod> CalibrationMethods { get; } = new();
    
    #endregion

    #region Property change handlers

    partial void OnSelectedIndexChanged(int value)
    {
        IsImportSessionsPageSelected = value == (int)PageIndices.ImportSessions;
    }

    #endregion
    
    #region Private members
    
    private enum PageIndices
    {
        ImportSessions = 0,
        Settings = 1,
        /*
        Linkages = 2,
        Calibrations = 3,
        BikeSetups = 4,
        */
    }

    private readonly IHttpApiService? httpApiService;

    #endregion
    
    #region Constructors

    public MainViewModel()
    {
        httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        
        SelectedIndex = SettingsPage.IsRegistered ? (int)PageIndices.ImportSessions : (int)PageIndices.Settings;
        SettingsPage.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(SettingsPage.IsRegistered))
            {
                return;
            }
            SelectedIndex = SettingsPage.IsRegistered ? (int)PageIndices.ImportSessions : (int)PageIndices.Settings;
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
        
        _ = LoadLinkagesAsync();
        _ = LoadCalibrationMethodsAsync();
        _ = LoadCalibrationsAsync();
        _ = LoadSetupsAsync();
    }

    #endregion

    #region Private methods

    private async Task LoadLinkagesAsync()
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        try
        {
            var linkages = await httpApiService.GetLinkages();

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
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        try
        {
            var methods = await httpApiService.GetCalibrationMethods();

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
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        try
        {
            var calibrations = await httpApiService.GetCalibrations();

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
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        try
        {
            var setups = await httpApiService.GetSetups();
            var boards = await httpApiService.GetBoards();

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

    #endregion

    #region Commands
    
    [RelayCommand]
    private void Reload()
    {
        Linkages.Clear();
        CalibrationMethods.Clear();
        Calibrations.Clear();
        Setups.Clear();
        _ = LoadLinkagesAsync();
        _ = LoadCalibrationMethodsAsync();
        _ = LoadCalibrationsAsync();
        _ = LoadSetupsAsync();
    }

    [RelayCommand]
    private async Task AddLinkage()
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        try
        {
            var linkage = new Linkage(null, "new linkage", 65, 180, 65, "");
            var id = await httpApiService.PutLinkage(linkage);
            Linkages.Add(new LinkageViewModel(linkage with { Id = id }));
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
    private void DeleteLinkage(int id)
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        try
        {
            httpApiService.DeleteLinkage(id);
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
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        try
        {
            var methodId = CalibrationMethods[0].Id;
            var inputs = new Dictionary<string, double>();
            foreach (var input in CalibrationMethods[0].Properties.Inputs)
            {
                inputs.Add(input, 0.0);
            }
            var calibration = new Calibration(null, "new calibration", methodId, inputs);
        
            var id = await httpApiService.PutCalibration(calibration);
            Calibrations.Add(new CalibrationViewModel(calibration with { Id = id }, CalibrationMethods));
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
    private void DeleteCalibration(int id)
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        try
        {
            httpApiService.DeleteCalibration(id);
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
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        try
        {
            var setup = new Setup(
                null,
                "new setup",
                Linkages[0].Id!.Value,
                null,
                null);
        
            var id = await httpApiService.PutSetup(setup);

            var svm = new SetupViewModel(
                setup with { Id = id },
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
    private void DeleteSetup(int id)
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        try
        {
            httpApiService.DeleteSetup(id);
            var toDelete = Setups.First(s => s.Id == id);
            Setups.Remove(toDelete);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not delete Setup: {e.Message}");
        }
    }

    #endregion
}