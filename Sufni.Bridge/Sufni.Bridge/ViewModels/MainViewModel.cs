using CommunityToolkit.Mvvm.ComponentModel;

namespace Sufni.Bridge.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Observable properties
    
    [ObservableProperty] private ImportSessionsViewModel importSessionsPage = new();
    [ObservableProperty] private SettingsViewModel settingsPage = new();
    [ObservableProperty] private int selectedIndex;
    [ObservableProperty] private bool isImportSessionsPageSelected = true;
    
    #endregion

    #region Property change handlers

    partial void OnSelectedIndexChanged(int value)
    {
        IsImportSessionsPageSelected = value == (int)PageIndices.ImportSessions;
    }

    #endregion
    
    #region Private fields
    
    private enum PageIndices
    {
        ImportSessions = 0,
        Settings = 1,
        Linkages = 2,
        Calibrations = 3,
        BikeSetups = 4,
    }

    #endregion
    
    #region Constructors

    public MainViewModel()
    {
        SelectedIndex = SettingsPage.IsRegistered ? (int)PageIndices.ImportSessions : (int)PageIndices.Settings;
    }

    #endregion
}