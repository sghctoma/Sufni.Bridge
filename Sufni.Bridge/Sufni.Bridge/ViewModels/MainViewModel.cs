using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SecureStorage;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using System.IO;

namespace Sufni.Bridge.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    #region Public properties

    public string ImportLabel { get; } = "Import Selected";

    #endregion Public properties

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

    #region Property change handlers

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

    partial void OnIsRegisteredChanged(bool value)
    {
        RegisterLabel = value ? "Unregister" : "Register";
    }

    #endregion Property change handlers

    #region Private members

    private ISecureStorage _secureStorage;
    private IHttpApiService _httpApiService;
    private ITelemetryDataStoreService _telemetryDataStoreService;

    #endregion Private members

    #region Constructors

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

    #endregion Constructors

    #region Private methods

    private async Task RefreshTokensAsync(string? url, string? refreshToken)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(refreshToken)) return;

        // Set the new token's value to the old one, so that in case of e.g. a network error,
        // we don't indicate "unregistered" state.
        // NOTE: We could also check if token is still valid based on time (this would of course
        //       not consider revoked keys, etc.)
        var newRefreshToken = refreshToken;
        IsRegistered = newRefreshToken != null;

        try
        {
            newRefreshToken = await _httpApiService.RefreshTokensAsync(url, refreshToken);
        }
        catch (HttpRequestException ex)
        {
            // Null out the refreshtoken if we explicitly receive a 401 - Unauthorized HTTP response.
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                newRefreshToken = null;
                IsRegistered = false;
            }
        }
        finally
        {
            _secureStorage.SetString("RefreshToken", newRefreshToken);
        }
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
    }

    #endregion Private methods

    #region Commands

    [RelayCommand]
    private async Task RegisterUnregister()
    {
        if (IsRegistered) await Unregister();
        else await Register();
    }

    [RelayCommand]
    private async Task ImportSessions()
    {
        Debug.Assert(SelectedDataStore != null);

        var boards = await _httpApiService.GetBoards();
        var boardsDict = boards.ToDictionary(b => b.Id!, b => b.SetupId);
        var boardId = SelectedDataStore.BoardId;
        var setupId = boardsDict[boardId];
        
        Debug.Assert(setupId != null);

        foreach (var telemetryFile in TelemetryFiles.Where(f => f.ShouldBeImported))
        {
            await _httpApiService.ImportSession(telemetryFile, setupId.Value);
            File.Move(telemetryFile.FullName,
                $"{Path.GetDirectoryName(telemetryFile.FullName)}/uploaded/{telemetryFile.FileName}");
        }

        ReloadTelemetryDataStores();
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

    #endregion Commands
}