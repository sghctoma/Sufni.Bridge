using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
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
    
    private readonly IHttpApiService _httpApiService;

    #endregion
    
    #region Constructors

    public MainViewModel()
    {
        SelectedIndex = SettingsPage.IsRegistered ? (int)PageIndices.ImportSessions : (int)PageIndices.Settings;
        _httpApiService = this.GetServiceOrCreateInstance<IHttpApiService>();
        _ = LoadLinkagesAsync();
        _ = LoadCalibrationsAsync();
        _ = LoadSetupsAsync();
    }

    #endregion

    #region Private methods

    private async Task LoadLinkagesAsync()
    {
        var linkages = await _httpApiService.GetLinkages();

        foreach (var linkage in linkages)
        {
            Linkages.Add(new LinkageViewModel(linkage));
        }
    }

    private async Task LoadCalibrationsAsync()
    {
        var calibrations = await _httpApiService.GetCalibrations();

        foreach (var calibration in calibrations)
        {
            Calibrations.Add(new CalibrationViewModel(calibration));
        }
    }

    private async Task LoadSetupsAsync()
    {
        var setups = await _httpApiService.GetSetups();

        foreach (var setup in setups)
        {
            Setups.Add(new SetupViewModel(setup, Linkages, Calibrations));
        }
    }

    #endregion
}