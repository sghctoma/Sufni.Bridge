using System;
using System.Diagnostics;
using System.Linq;
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
    private const double HighSpeedThreshold = 200;
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

    [ObservableProperty] private string? frontTravelAverageString;
    [ObservableProperty] private string? frontTravelMaxString;
    [ObservableProperty] private string? rearTravelAverageString;
    [ObservableProperty] private string? rearTravelMaxString;
    
    [ObservableProperty] private TravelStatistics? frontTravelStatistics;
    [ObservableProperty] private VelocityStatistics? frontVelocityStatistics;
    [ObservableProperty] private VelocityBands? frontVelocityBands;
    [ObservableProperty] private TravelStatistics? rearTravelStatistics;
    [ObservableProperty] private VelocityStatistics? rearVelocityStatistics;
    [ObservableProperty] private VelocityBands? rearVelocityBands;
    
    [ObservableProperty] private double? compressionBalanceMsd;
    [ObservableProperty] private double? reboundBalanceMsd;

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
        
        if (psst.Front.Present)
        {
            FrontTravelStatistics = psst.CalculateTravelStatistics(SuspensionType.Front);
            FrontVelocityStatistics = psst.CalculateVelocityStatistics(SuspensionType.Front);
            FrontVelocityBands = psst.CalculateVelocityBands(SuspensionType.Front, HighSpeedThreshold);

            var avgPercentage = FrontTravelStatistics.Average / psst.Linkage.MaxFrontTravel * 100.0;
            FrontTravelAverageString = $"{FrontTravelStatistics.Average:F2} mm ({avgPercentage:F2}%)";
            var maxPercentage = FrontTravelStatistics.Max / psst.Linkage.MaxFrontTravel * 100.0;
            FrontTravelMaxString = $"{FrontTravelStatistics.Max:F2} mm ({maxPercentage:F2}%) / {FrontTravelStatistics.Bottomouts} bottom outs";
        }

        if (psst.Rear.Present)
        {
            RearTravelStatistics = psst.CalculateTravelStatistics(SuspensionType.Rear);
            RearVelocityStatistics = psst.CalculateVelocityStatistics(SuspensionType.Rear);
            RearVelocityBands = psst.CalculateVelocityBands(SuspensionType.Rear, HighSpeedThreshold);
            
            var avgPercentage = RearTravelStatistics.Average / psst.Linkage.MaxRearTravel * 100.0;
            RearTravelAverageString = $"{RearTravelStatistics.Average:F2} mm ({avgPercentage:F2}%)";
            var maxPercentage = RearTravelStatistics.Max / psst.Linkage.MaxRearTravel * 100.0;
            RearTravelMaxString = $"{RearTravelStatistics.Max:F2} mm ({maxPercentage:F2}%) / {RearTravelStatistics.Bottomouts} bottom outs";
        }

        if (psst.Front.Present && psst.Rear.Present)
        {
            var compressionBalance = psst.CalculateBalance(BalanceType.Compression);
            var reboundBalance = psst.CalculateBalance(BalanceType.Rebound);

            var compressionMax = Math.Max(
                compressionBalance.FrontVelocity.Max(),
                compressionBalance.RearVelocity.Max());
            var reboundMax = Math.Abs(Math.Max(
                reboundBalance.FrontVelocity.Max(),
                reboundBalance.RearVelocity.Max()));
            
            CompressionBalanceMsd = compressionBalance.MeanSignedDeviation / compressionMax * 100.0;
            ReboundBalanceMsd = reboundBalance.MeanSignedDeviation / reboundMax * 100.0;
        }
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