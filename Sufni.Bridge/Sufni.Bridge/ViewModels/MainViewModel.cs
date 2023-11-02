using System.Collections.ObjectModel;
using System.Diagnostics;
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
            Setups.Add(new SetupViewModel(setup, Linkages, Calibrations));
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void Reload()
    {
        
    }

    [RelayCommand]
    private void AddLinkage()
    {
        
    }
    
    [RelayCommand]
    private void AddCalibration()
    {
        
    }
    
    [RelayCommand]
    private void AddSetup()
    {
        
    }

    #endregion
}