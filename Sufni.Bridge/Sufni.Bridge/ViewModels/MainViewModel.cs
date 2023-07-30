using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureStorage;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sufni.Bridge.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    public string ImportLabel { get; } = "Import Selected";

    #region Observable properties
    public ObservableCollection<TelemetryDataStore> TelemetryDataStores { get; }
    public ObservableCollection<TelemetryFile> TelemetryFiles { get; } = new ObservableCollection<TelemetryFile>();

    [ObservableProperty]
    private TelemetryDataStore? selectedDataStore;

    [ObservableProperty]
    private string registerLabel = "Register";

    [ObservableProperty]
    private string? serverUrl;

    [ObservableProperty]
    private string? username;

    [ObservableProperty]
    private string? password;

    [ObservableProperty]
    private bool isRegistered;

    [ObservableProperty]
    private string? registrationError;
    #endregion Observable properties

    #region Private members
    private ISecureStorage _secureStorage;
    private IHttpApiService _httpApiService;
    private ITelemetryDataStoreService _telemetryDataStoreService;
    #endregion Private members

    partial void OnSelectedDataStoreChanged(TelemetryDataStore? value)
    {
        TelemetryFiles.Clear();
        if (value != null)
        {
            var files = value.Files;
            foreach (var file in files)
            {
                TelemetryFiles.Add(file);
            }
        }
    }

    public MainViewModel()
    {
        _secureStorage = this.GetServiceOrCreateInstance<ISecureStorage>();
        _httpApiService = this.GetServiceOrCreateInstance<IHttpApiService>();
        _telemetryDataStoreService = this.GetServiceOrCreateInstance<ITelemetryDataStoreService>();

        ServerUrl = _secureStorage.GetString("ServerUrl");
        Username = _secureStorage.GetString("Username");
        var refreshToken = _secureStorage.GetString("RefreshToken");
        _ = RefreshTokensAsync(ServerUrl, refreshToken);

        var ds = _telemetryDataStoreService.GetTelemetryDataStores();
        TelemetryDataStores = new ObservableCollection<TelemetryDataStore>(ds);

        if (TelemetryDataStores.Count > 0)
        {
            SelectedDataStore = TelemetryDataStores[0];
        }
    }

    private async Task RefreshTokensAsync(string? url, string? refreshToken)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(refreshToken)) return;

        var newRefreshToken = await _httpApiService.InitAsync(url, refreshToken);
        _secureStorage.SetString("RefreshToken", newRefreshToken);
        IsRegistered = true;
        RegisterLabel = "Unregister";
    }

    private async Task Register()
    {
        if (string.IsNullOrEmpty(ServerUrl) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)) return;

        try
        {
            var refreshToken = await _httpApiService.RegisterAsync(ServerUrl, Username, Password);
            _secureStorage.SetString("Username", Username);
            _secureStorage.SetString("ServerUrl", ServerUrl);
            _secureStorage.SetString("RefreshToken", refreshToken);
            IsRegistered = true;
            RegisterLabel = "Unregister";
            RegistrationError = null;
        }
        catch(Exception ex)
        {
            RegistrationError = $"Registration failed: {ex.Message}";
        }
    }

    private async Task Unregister()
    {
        var refreshToken = _secureStorage.GetString("RefreshToken");
        await _httpApiService.Unregister(refreshToken!);
        _secureStorage.Remove("Username");
        _secureStorage.Remove("ServerUrl");
        _secureStorage.Remove("RefreshToken");
        Username = null;
        ServerUrl = null;
        Password = null;
        IsRegistered = false;
        RegisterLabel = "Register";
    }

    [RelayCommand]
    private async Task RegisterUnregister()
    {
        if (IsRegistered) await Unregister();
        else await Register();
    }

    [RelayCommand]
    private async Task ImportSessions()
    {
        
    }

    [RelayCommand]
    private void ReloadTelemetryDataStores()
    {
        TelemetryDataStores.Clear();
        var ds = _telemetryDataStoreService.GetTelemetryDataStores();
        foreach (var store in ds)
        {
            TelemetryDataStores.Add(store);
        }

        if (TelemetryDataStores.Count > 0)
        {
            SelectedDataStore = TelemetryDataStores[0];
        }
    }
}