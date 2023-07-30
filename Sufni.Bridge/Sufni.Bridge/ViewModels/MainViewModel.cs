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
    public ObservableCollection<TelemetryFile> TelemetryFiles { get; }

    public string ImportLabel { get; } = "Import Selected";

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

    private ISecureStorage _secureStorage;
    private IHttpApiService _httpApiService;
    private ITelemetryFileService _telemetryFileService;

    public MainViewModel()
    {
        _secureStorage = this.GetServiceOrCreateInstance<ISecureStorage>();
        _httpApiService = this.GetServiceOrCreateInstance<IHttpApiService>();
        _telemetryFileService = this.GetServiceOrCreateInstance<ITelemetryFileService>();

        ServerUrl = _secureStorage.GetString("ServerUrl");
        Username = _secureStorage.GetString("Username");
        var refreshToken = _secureStorage.GetString("RefreshToken");
        _ = RefreshTokensAsync(ServerUrl, refreshToken);

        var files = _telemetryFileService.GetTelemetryFiles();
        TelemetryFiles = new ObservableCollection<TelemetryFile>(files);
    }

    private async Task RefreshTokensAsync(string? url, string? refreshToken)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(refreshToken)) return;

        var newRefreshToken = await _httpApiService.InitAsync(url, refreshToken);
        _secureStorage.SetString("RefreshToken", newRefreshToken);
        IsRegistered = true;
        RegisterLabel = "Unregister";
    }

    private async Task register()
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

    private async Task unregister()
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
        if (IsRegistered) await unregister();
        else await register();
    }

    [RelayCommand]
    private async Task ImportSessions()
    {
        
    }

    [RelayCommand]
    private void ReloadTelemetryFiles()
    {
        var files = _telemetryFileService.GetTelemetryFiles();
        TelemetryFiles.Clear();
        foreach (var file in files)
        {
            TelemetryFiles.Add(file);
        }
    }
}