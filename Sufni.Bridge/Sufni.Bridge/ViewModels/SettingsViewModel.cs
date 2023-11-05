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
    
    #endregion
    
    #region Property change handlers

    partial void OnIsRegisteredChanged(bool value)
    {
        RegisterLabel = value ? "Unregister" : "Register";
    }

    #endregion
    
    #region Private members
    
    private readonly ISecureStorage? secureStorage;
    private readonly IHttpApiService? httpApiService;
    
    #endregion Private members
    
    #region Constructors

    public SettingsViewModel()
    {
        secureStorage = App.Current?.Services?.GetService<ISecureStorage>();
        httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        Debug.Assert(secureStorage != null, nameof(secureStorage) + " != null");
        
        ServerUrl = secureStorage.GetString("ServerUrl");
        Username = secureStorage.GetString("Username");
        var refreshToken = secureStorage.GetString("RefreshToken");
        _ = RefreshTokensAsync(ServerUrl, refreshToken);
    }

    #endregion

    #region Private methods

    private async Task RefreshTokensAsync(string? url, string? refreshToken)
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        Debug.Assert(secureStorage != null, nameof(secureStorage) + " != null");

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
            newRefreshToken = await httpApiService.RefreshTokensAsync(url, refreshToken);
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
            secureStorage.SetString("RefreshToken", newRefreshToken);
        }
    }

    private async Task Register()
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        Debug.Assert(secureStorage != null, nameof(secureStorage) + " != null");
        
        if (string.IsNullOrEmpty(ServerUrl) || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password)) return;

        try
        {
            var refreshToken = await httpApiService.RegisterAsync(ServerUrl, Username, Password);
            secureStorage.SetString("Username", Username);
            secureStorage.SetString("ServerUrl", ServerUrl);
            secureStorage.SetString("RefreshToken", refreshToken);
            IsRegistered = true;
            ErrorMessages.Clear();
        }
        catch(Exception ex)
        {
            ErrorMessages.Add($"Registration failed: {ex.Message}");
        }
    }

    private async Task Unregister()
    {
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        Debug.Assert(secureStorage != null, nameof(secureStorage) + " != null");
        
        var refreshToken = secureStorage.GetString("RefreshToken");
        await httpApiService.Unregister(refreshToken!);
        secureStorage.Remove("Username");
        secureStorage.Remove("ServerUrl");
        secureStorage.Remove("RefreshToken");
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
