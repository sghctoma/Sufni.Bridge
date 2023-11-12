using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class SessionViewModel : ViewModelBase
{
    private Session session;
    private TelemetryData? psst;

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Name != session.Name ||
            Description != session.Description;
    }

    #endregion

    #region Observable properties

    [ObservableProperty] private int? id;
    [ObservableProperty] private string? name;
    [ObservableProperty] private string? description;
    [ObservableProperty] private DateTime? timestamp;
    [ObservableProperty] private TelemetryData? telemetryData;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool isDirty;

    #endregion
    
    #region Property change handlers
    
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnNameChanged(string? value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnDescriptionChanged(string? value)
    {
        EvaluateDirtiness();
    }

    #endregion

    #region Constructors

    public SessionViewModel(Session session)
    {
        this.session = session;
        Reset();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private void SetTelemetryData()
    {
        TelemetryData = psst;
    }

    [RelayCommand]
    private async Task LoadPsst()
    {
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        psst = await httpApiService.GetSessionPsstAsync(Id ?? 0);
    }
    
    private bool CanSave()
    {
        return IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        try
        {
            var newSession = new Session(
                session.Id,
                Name ?? $"session #{session.Id}",
                Description ?? $"session #{session.Id}",
                session.Setup,
                null,
                null,
                null);
            httpApiService.PutSessionAsync(newSession);
            session = newSession;
            IsDirty = false;
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Session could not be saved: {e.Message}");
        }
    }

    private bool CanReset()
    {
        return IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        Id = session.Id;
        Name = session.Name;
        Description = session.Description;
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(session.Timestamp ?? 0).DateTime;
    }

    #endregion
}