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
    public bool IsInDatabase;

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            !IsInDatabase ||
            Name != session.Name ||
            Description != session.Description;
    }

    #endregion

    #region Observable properties

    [ObservableProperty] private Guid id;
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

    public SessionViewModel(Session session, bool fromDatabase)
    {
        this.session = session;
        IsInDatabase = fromDatabase;
        Reset();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task LoadPsst()
    {
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        
        try
        {
            TelemetryData = await databaseService.GetSessionPsstAsync(Id);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load session data: {e.Message}");
        }
    }

    private bool CanSave()
    {
        return IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var newSession = new Session(
                id: session.Id,
                name: Name ?? $"session #{session.Id}",
                description: Description ?? $"session #{session.Id}",
                setup: session.Setup);
            await databaseService.PutSessionAsync(newSession);
            session = newSession;
            IsDirty = false;
            IsInDatabase = true;
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