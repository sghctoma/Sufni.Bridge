using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureStorage;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Sufni.Bridge.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    public ObservableCollection<TelemetrySession>? TelemetryFiles { get; set; }

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

    public MainViewModel()
    {
        _secureStorage = this.GetServiceOrCreateInstance<ISecureStorage>();
        _httpApiService = this.GetServiceOrCreateInstance<IHttpApiService>();

        ServerUrl = _secureStorage.GetString("ServerUrl");
        Username = _secureStorage.GetString("Username");
        var refreshToken = _secureStorage.GetString("RefreshToken");
        _ = RefreshTokensAsync(ServerUrl, refreshToken);

        var drives = DriveInfo.GetDrives()
            .Where(drive => drive is
            {
                IsReady: true,
                DriveType: DriveType.Removable,
                DriveFormat: "FAT32"
            } && File.Exists($"{drive.RootDirectory}/.boardid"))
            .Select(d => new TelemetryDataStore(d.VolumeLabel, d.RootDirectory))
            .ToList();

        List<TelemetrySession> sessions = new List<TelemetrySession>();
        foreach (var drive in drives)
        {
            sessions.AddRange(drive.Files);
        }
        TelemetryFiles = new ObservableCollection<TelemetrySession>(sessions);
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
}