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
        Linkages = 2,
        Calibrations = 3,
        BikeSetups = 4,
    }

    private readonly IHttpApiService? _httpApiService;

    #endregion
    
    #region Constructors

    public MainViewModel()
    {
        _httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        
        SelectedIndex = SettingsPage.IsRegistered ? (int)PageIndices.ImportSessions : (int)PageIndices.Settings;

        Linkages.CollectionChanged += (sender, args) =>
        {
            HasLinkages = Linkages.Count != 0;
        };
        CalibrationMethods.CollectionChanged += (sender, args) =>
        {
            HasCalibrationMethods = CalibrationMethods.Count != 0;
        };
        Calibrations.CollectionChanged += (sender, args) =>
        {
            HasCalibrations = Calibrations.Count != 0;
        };
        Setups.CollectionChanged += (sender, args) =>
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
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        
        var linkages = await _httpApiService.GetLinkages();

        foreach (var linkage in linkages)
        {
            Linkages.Add(new LinkageViewModel(linkage));
        }
    }
    private async Task LoadCalibrationMethodsAsync()
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        
        var methods = await _httpApiService.GetCalibrationMethods();

        foreach (var method in methods)
        {
            CalibrationMethods.Add(method);
        }
    }

    private async Task LoadCalibrationsAsync()
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        
        var calibrations = await _httpApiService.GetCalibrations();

        foreach (var calibration in calibrations)
        {
            Calibrations.Add(new CalibrationViewModel(calibration, CalibrationMethods));
        }
    }

    private async Task LoadSetupsAsync()
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        
        var setups = await _httpApiService.GetSetups();

        foreach (var setup in setups)
        {
            var svm = new SetupViewModel(setup, Linkages, Calibrations);
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
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");

        var linkage = new Linkage(null, "new linkage", 65, 180, 65, "");
        var id = await _httpApiService.PutLinkage(linkage);
        Linkages.Add(new LinkageViewModel(linkage with { Id = id }));
    }

    private bool CanDeleteLinkage(int id)
    {
        return !Setups.Any(s => s.SelectedLinkage != null && s.SelectedLinkage.Id == id);
    }
    
    [RelayCommand(CanExecute = nameof(CanDeleteLinkage))]
    private void DeleteLinkage(int id)
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        _httpApiService.DeleteLinkage(id);
        var toDelete = Linkages.First(l => l.Id == id);
        Linkages.Remove(toDelete);
    }

    [RelayCommand]
    private async Task AddCalibration()
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");

        var methodId = CalibrationMethods[0].Id;
        var inputs = new Dictionary<string, double>();
        foreach (var input in CalibrationMethods[0].Properties.Inputs)
        {
            inputs.Add(input, 0.0);
        }
        var calibration = new Calibration(null, "new calibration", methodId, inputs);
        
        var id = await _httpApiService.PutCalibration(calibration);
        Calibrations.Add(new CalibrationViewModel(calibration with { Id = id }, CalibrationMethods));
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
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        _httpApiService.DeleteCalibration(id);
        var toDelete = Calibrations.First(c => c.Id == id);
        Calibrations.Remove(toDelete);
    }
    
    [RelayCommand]
    private async Task AddSetup()
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");

        var setup = new Setup(
            null,
            "new setup",
            Linkages[0].Id!.Value,
            null,
            null);
        
        var id = await _httpApiService.PutSetup(setup);

        var svm = new SetupViewModel(setup with { Id = id }, Linkages, Calibrations);
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
    
    [RelayCommand]
    private void DeleteSetup(int id)
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        _httpApiService.DeleteSetup(id);
        var toDelete = Setups.First(s => s.Id == id);
        Setups.Remove(toDelete);
    }

    #endregion
}