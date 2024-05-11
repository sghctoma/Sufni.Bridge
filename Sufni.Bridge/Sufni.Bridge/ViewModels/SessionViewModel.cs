using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using ScottPlot;
using Sufni.Bridge.Models;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Plots;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class SuspensionSettings : ObservableObject
{
    [ObservableProperty] private string? springRate;
    [ObservableProperty] private uint? highSpeedCompression;
    [ObservableProperty] private uint? lowSpeedCompression;
    [ObservableProperty] private uint? lowSpeedRebound;
    [ObservableProperty] private uint? highSpeedRebound;
}

public partial class SessionViewModel : ViewModelBase
{
    private Session session;
    public bool IsInDatabase;
    public SuspensionSettings ForkSettings { get; } = new();
    public SuspensionSettings ShockSettings { get; } = new();

    #region Private methods
    
    private void EvaluateDirtiness()
    {
        IsDirty =
            !IsInDatabase ||
            Name != session.Name ||
            Description != session.Description ||
            (!(ForkSettings.SpringRate is null && session.FrontSpringRate is null) && ForkSettings.SpringRate != session.FrontSpringRate) ||
            (!(ForkSettings.HighSpeedCompression is null && session.FrontHighSpeedCompression is null) && ForkSettings.HighSpeedCompression != session.FrontHighSpeedCompression) ||
            (!(ForkSettings.LowSpeedCompression is null && session.FrontLowSpeedCompression is null) && ForkSettings.LowSpeedCompression != session.FrontLowSpeedCompression) ||
            (!(ForkSettings.LowSpeedRebound is null && session.FrontLowSpeedRebound is null) && ForkSettings.LowSpeedRebound != session.FrontLowSpeedRebound) ||
            (!(ForkSettings.HighSpeedRebound is null && session.FrontHighSpeedRebound is null) && ForkSettings.HighSpeedRebound != session.FrontHighSpeedRebound) ||
            (!(ShockSettings.SpringRate is null && session.RearSpringRate is null) && ShockSettings.SpringRate != session.RearSpringRate) ||
            (!(ShockSettings.HighSpeedCompression is null && session.RearHighSpeedCompression is null) && ShockSettings.HighSpeedCompression != session.RearHighSpeedCompression) ||
            (!(ShockSettings.LowSpeedCompression is null && session.RearLowSpeedCompression is null) && ShockSettings.LowSpeedCompression != session.RearLowSpeedCompression) ||
            (!(ShockSettings.LowSpeedRebound is null && session.RearLowSpeedRebound is null) && ShockSettings.LowSpeedRebound != session.RearLowSpeedRebound) ||
            (!(ShockSettings.HighSpeedRebound is null && session.RearHighSpeedRebound is null) && ShockSettings.HighSpeedRebound != session.RearHighSpeedRebound);
    }

    private async Task<bool> LoadCache()
    {
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        var cache = await databaseService.GetSessionCacheAsync(Id);
        if (cache is null)
        {
            return false;
        }

        FrontTravelHistogram = cache.FrontTravelHistogram;
        RearTravelHistogram = cache.RearTravelHistogram;
        FrontVelocityHistogram = cache.FrontVelocityHistogram;
        RearVelocityHistogram = cache.RearVelocityHistogram;
        CompressionBalance = cache.CompressionBalance;
        ReboundBalance = cache.ReboundBalance;
        FrontHscPercentage = cache.FrontHscPercentage;
        RearHscPercentage = cache.RearHscPercentage;
        FrontLscPercentage = cache.FrontLscPercentage;
        RearLscPercentage = cache.RearLscPercentage;
        FrontLsrPercentage = cache.FrontLsrPercentage;
        RearLsrPercentage = cache.RearLsrPercentage;
        FrontHsrPercentage = cache.FrontHsrPercentage;
        RearHsrPercentage = cache.RearHsrPercentage;

        return true;
    }

    private async void CreateCache(object? bounds)
    {
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        var telemetryData = await databaseService.GetSessionPsstAsync(Id);
        if (telemetryData is null)
        {
            throw new Exception("Database error");
        }

        var b = (Rect)bounds!;
        var (width, height) = ((int)b.Width, (int)(b.Height / 2.0));
        var sessionCache = new SessionCache();

        var fth = new TravelHistogramPlot(new Plot(), SuspensionType.Front);
        fth.LoadTelemetryData(telemetryData);
        sessionCache.FrontTravelHistogram = fth.Plot.GetSvgXml(width, height);
        Dispatcher.UIThread.Post(() => { FrontTravelHistogram = sessionCache.FrontTravelHistogram; });

        var rth = new TravelHistogramPlot(new Plot(), SuspensionType.Rear);
        rth.LoadTelemetryData(telemetryData);
        sessionCache.RearTravelHistogram = rth.Plot.GetSvgXml(width, height);
        Dispatcher.UIThread.Post(() => { RearTravelHistogram = sessionCache.RearTravelHistogram; });

        var fvh = new VelocityHistogramPlot(new Plot(), SuspensionType.Front);
        fvh.LoadTelemetryData(telemetryData);
        sessionCache.FrontVelocityHistogram = fvh.Plot.GetSvgXml(width - 64, 478);
        Dispatcher.UIThread.Post(() => { FrontVelocityHistogram = sessionCache.FrontVelocityHistogram; });

        var rvh = new VelocityHistogramPlot(new Plot(), SuspensionType.Rear);
        rvh.LoadTelemetryData(telemetryData);
        sessionCache.RearVelocityHistogram = rvh.Plot.GetSvgXml(width - 64, 478);
        Dispatcher.UIThread.Post(() => { RearVelocityHistogram = sessionCache.RearVelocityHistogram; });

        var cb = new BalancePlot(new Plot(), BalanceType.Compression);
        cb.LoadTelemetryData(telemetryData);
        sessionCache.CompressionBalance = cb.Plot.GetSvgXml(width, height);
        Dispatcher.UIThread.Post(() => { CompressionBalance = sessionCache.CompressionBalance; });

        var rb = new BalancePlot(new Plot(), BalanceType.Rebound);
        rb.LoadTelemetryData(telemetryData);
        sessionCache.ReboundBalance = rb.Plot.GetSvgXml(width, height);
        Dispatcher.UIThread.Post(() => { ReboundBalance = sessionCache.ReboundBalance; });

        var fvb = telemetryData.CalculateVelocityBands(SuspensionType.Front, 200);
        sessionCache.FrontHsrPercentage = fvb.HighSpeedRebound;
        sessionCache.FrontLsrPercentage = fvb.LowSpeedRebound;
        sessionCache.FrontLscPercentage = fvb.LowSpeedCompression;
        sessionCache.FrontHscPercentage = fvb.HighSpeedCompression;
        Dispatcher.UIThread.Post(() =>
        {
            FrontHsrPercentage = fvb.HighSpeedRebound;
            FrontLsrPercentage = fvb.LowSpeedRebound;
            FrontLscPercentage = fvb.LowSpeedCompression;
            FrontHscPercentage = fvb.HighSpeedCompression;
        });

        var rvb = telemetryData.CalculateVelocityBands(SuspensionType.Rear, 200);
        sessionCache.RearHsrPercentage = rvb.HighSpeedRebound;
        sessionCache.RearLsrPercentage = rvb.LowSpeedRebound;
        sessionCache.RearLscPercentage = rvb.LowSpeedCompression;
        sessionCache.RearHscPercentage = rvb.HighSpeedCompression;
        Dispatcher.UIThread.Post(() =>
        {
            RearHsrPercentage = rvb.HighSpeedRebound;
            RearLsrPercentage = rvb.LowSpeedRebound;
            RearLscPercentage = rvb.LowSpeedCompression;
            RearHscPercentage = rvb.HighSpeedCompression;
        });
        
        await databaseService.PutSessionCacheAsync(sessionCache);
    }

    #endregion

    #region Observable properties

    [ObservableProperty] private Guid id;
    [ObservableProperty] private string? name;
    [ObservableProperty] private string? description;
    [ObservableProperty] private DateTime? timestamp;

    [ObservableProperty] private string? frontTravelHistogram;
    [ObservableProperty] private string? rearTravelHistogram;
    [ObservableProperty] private string? frontVelocityHistogram;
    [ObservableProperty] private string? rearVelocityHistogram;
    [ObservableProperty] private string? compressionBalance;
    [ObservableProperty] private string? reboundBalance;
    [ObservableProperty] private double? frontHscPercentage;
    [ObservableProperty] private double? rearHscPercentage;
    [ObservableProperty] private double? frontLscPercentage;
    [ObservableProperty] private double? rearLscPercentage;
    [ObservableProperty] private double? frontLsrPercentage;
    [ObservableProperty] private double? rearLsrPercentage;
    [ObservableProperty] private double? frontHsrPercentage;
    [ObservableProperty] private double? rearHsrPercentage;
    
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

    public SessionViewModel()
    {
        session = new Session();
        IsInDatabase = false;
    }
    
    public SessionViewModel(Session session, bool fromDatabase)
    {
        this.session = session;
        IsInDatabase = fromDatabase;
        
        ForkSettings.PropertyChanged += (_, _) => EvaluateDirtiness();
        ShockSettings.PropertyChanged += (_, _) => EvaluateDirtiness();
        
        Reset();
    }

    #endregion

    #region Commands

    [RelayCommand]
    private async Task Loaded(Rect bounds)
    {
        try
        {
            if (!await LoadCache())
            {
                new Thread(CreateCache).Start(bounds);
            }
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
                setup: session.Setup)
            {
                FrontSpringRate = ForkSettings.SpringRate,
                FrontHighSpeedCompression = ForkSettings.HighSpeedCompression,
                FrontLowSpeedCompression = ForkSettings.LowSpeedCompression,
                FrontLowSpeedRebound = ForkSettings.LowSpeedRebound,
                FrontHighSpeedRebound = ForkSettings.HighSpeedRebound,
                RearSpringRate = ShockSettings.SpringRate,
                RearHighSpeedCompression = ShockSettings.HighSpeedCompression,
                RearLowSpeedCompression = ShockSettings.LowSpeedCompression,
                RearLowSpeedRebound = ShockSettings.LowSpeedRebound,
                RearHighSpeedRebound = ShockSettings.HighSpeedRebound,
            };


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
        
        ForkSettings.SpringRate = session.FrontSpringRate;
        ForkSettings.HighSpeedCompression = session.FrontHighSpeedCompression;
        ForkSettings.LowSpeedCompression = session.FrontLowSpeedCompression;
        ForkSettings.LowSpeedRebound = session.FrontLowSpeedRebound;
        ForkSettings.HighSpeedRebound = session.FrontHighSpeedRebound;
        
        ShockSettings.SpringRate = session.RearSpringRate;
        ShockSettings.HighSpeedCompression = session.RearHighSpeedCompression;
        ShockSettings.LowSpeedCompression = session.RearLowSpeedCompression;
        ShockSettings.LowSpeedRebound = session.RearLowSpeedRebound;
        ShockSettings.HighSpeedRebound = session.RearHighSpeedRebound;
        
        Timestamp = DateTimeOffset.FromUnixTimeSeconds(session.Timestamp ?? 0).DateTime;
    }

    [RelayCommand]
    private async Task Delete()
    {
        var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
        Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

        await mainPagesViewModel.DeleteSessionCommand.ExecuteAsync(Id);
        
        OpenPreviousPage();
    }
    
    #endregion
}