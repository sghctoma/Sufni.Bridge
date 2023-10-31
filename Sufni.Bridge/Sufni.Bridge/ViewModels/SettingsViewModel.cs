using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SecureStorage;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    #region Observable properties

    [ObservableProperty] private string registerLabel = "Register";

    [ObservableProperty] private string? serverUrl;

    [ObservableProperty] private string? username;

    [ObservableProperty] private string? password;

    [ObservableProperty] private bool isRegistered;

    [ObservableProperty] private string? registrationError;
    
    #endregion
    
    #region Property change handlers

    partial void OnIsRegisteredChanged(bool value)
    {
        RegisterLabel = value ? "Unregister" : "Register";
    }

    #endregion
    
    #region Private members
    
    private readonly ISecureStorage? _secureStorage;
    private readonly IHttpApiService? _httpApiService;
    
    #endregion Private members
    
    #region Constructors

    public SettingsViewModel()
    {
        _secureStorage = App.Current?.Services?.GetService<ISecureStorage>();
        _httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        Debug.Assert(_secureStorage != null, nameof(_secureStorage) + " != null");
        
        ServerUrl = _secureStorage.GetString("ServerUrl");
        Username = _secureStorage.GetString("Username");
        var refreshToken = _secureStorage.GetString("RefreshToken");
        _ = RefreshTokensAsync(ServerUrl, refreshToken);
    }

    #endregion

    #region Private methods

    private async Task RefreshTokensAsync(string? url, string? refreshToken)
    {
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        Debug.Assert(_secureStorage != null, nameof(_secureStorage) + " != null");

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(refreshToken))
        {
            IsRegistered = false;
            return;
        }

        // Set the new token's value to the old one, so that in case of e.g. a network error,
        // we don't indicate "unregistered" state.
        // NOTE: We could also check if token is still valid based on time (this would of course
        //       not consider revoked keys, etc.)
        var newRefreshToken = refreshToken;

        try
        {
            newRefreshToken = await _httpApiService.RefreshTokensAsync(url, refreshToken);
            IsRegistered = true;
        }
        catch (HttpRequestException ex)
        {
            // Null out the refresh token if we explicitly receive a 401 - Unauthorized HTTP response.
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
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        Debug.Assert(_secureStorage != null, nameof(_secureStorage) + " != null");
        
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
        Debug.Assert(_httpApiService != null, nameof(_httpApiService) + " != null");
        Debug.Assert(_secureStorage != null, nameof(_secureStorage) + " != null");
        
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

    #endregion Commands
}
