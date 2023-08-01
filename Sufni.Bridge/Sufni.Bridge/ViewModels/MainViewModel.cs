using CommunityToolkit.Mvvm.ComponentModel;

namespace Sufni.Bridge.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Observable properties

    [ObservableProperty] private ViewModelBase importSessionsPage = new ImportSessionsViewModel();
    [ObservableProperty] private ViewModelBase settingsPage = new SettingsViewModel();

    #endregion
}