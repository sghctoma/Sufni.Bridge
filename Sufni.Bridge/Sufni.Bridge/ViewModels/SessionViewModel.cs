using System;
using System.Collections.ObjectModel;
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
using Sufni.Bridge.ViewModels.SessionPages;

namespace Sufni.Bridge.ViewModels;

public partial class SessionViewModel : ItemViewModelBase
{
    private Session session;
    public bool IsInDatabase;
    private SpringPageViewModel SpringPage { get; } = new();
    private DamperPageViewModel DamperPage { get; } = new();
    private BalancePageViewModel BalancePage { get; } = new();
    private NotesPageViewModel NotesPage { get; } = new();
    public ObservableCollection<PageViewModelBase> Pages {get; }
    public string Description => NotesPage.Description ?? "";

    #region Private methods

    private async Task<bool> LoadCache()
    {
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        var cache = await databaseService.GetSessionCacheAsync(Id);
        if (cache is null)
        {
            return false;
        }

        SpringPage.FrontTravelHistogram = cache.FrontTravelHistogram;
        SpringPage.RearTravelHistogram = cache.RearTravelHistogram;

        DamperPage.FrontVelocityHistogram = cache.FrontVelocityHistogram;
        DamperPage.RearVelocityHistogram = cache.RearVelocityHistogram;
        DamperPage.FrontHscPercentage = cache.FrontHscPercentage;
        DamperPage.RearHscPercentage = cache.RearHscPercentage;
        DamperPage.FrontLscPercentage = cache.FrontLscPercentage;
        DamperPage.RearLscPercentage = cache.RearLscPercentage;
        DamperPage.FrontLsrPercentage = cache.FrontLsrPercentage;
        DamperPage.RearLsrPercentage = cache.RearLsrPercentage;
        DamperPage.FrontHsrPercentage = cache.FrontHsrPercentage;
        DamperPage.RearHsrPercentage = cache.RearHsrPercentage;

        if (cache.CompressionBalance is not null) 
        {
            BalancePage.CompressionBalance = cache.CompressionBalance;
            BalancePage.ReboundBalance = cache.ReboundBalance;
        }
        else 
        {
            Pages.Remove(BalancePage);
        }

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
        var sessionCache = new SessionCache
        {
            SessionId = Id
        };

        if (telemetryData.Front.Present)
        {
            var fth = new TravelHistogramPlot(new Plot(), SuspensionType.Front);
            fth.LoadTelemetryData(telemetryData);
            sessionCache.FrontTravelHistogram = fth.Plot.GetSvgXml(width, height);
            Dispatcher.UIThread.Post(() => { SpringPage.FrontTravelHistogram = sessionCache.FrontTravelHistogram; });

            var fvh = new VelocityHistogramPlot(new Plot(), SuspensionType.Front);
            fvh.LoadTelemetryData(telemetryData);
            sessionCache.FrontVelocityHistogram = fvh.Plot.GetSvgXml(width - 64, 478);
            Dispatcher.UIThread.Post(() => { DamperPage.FrontVelocityHistogram = sessionCache.FrontVelocityHistogram; });

            var fvb = telemetryData.CalculateVelocityBands(SuspensionType.Front, 200);
            sessionCache.FrontHsrPercentage = fvb.HighSpeedRebound;
            sessionCache.FrontLsrPercentage = fvb.LowSpeedRebound;
            sessionCache.FrontLscPercentage = fvb.LowSpeedCompression;
            sessionCache.FrontHscPercentage = fvb.HighSpeedCompression;
            Dispatcher.UIThread.Post(() =>
            {
                DamperPage.FrontHsrPercentage = fvb.HighSpeedRebound;
                DamperPage.FrontLsrPercentage = fvb.LowSpeedRebound;
                DamperPage.FrontLscPercentage = fvb.LowSpeedCompression;
                DamperPage.FrontHscPercentage = fvb.HighSpeedCompression;
            });
        }
        
        if (telemetryData.Rear.Present) 
        {
            var rth = new TravelHistogramPlot(new Plot(), SuspensionType.Rear);
            rth.LoadTelemetryData(telemetryData);
            sessionCache.RearTravelHistogram = rth.Plot.GetSvgXml(width, height);
            Dispatcher.UIThread.Post(() => { SpringPage.RearTravelHistogram = sessionCache.RearTravelHistogram; });

            var rvh = new VelocityHistogramPlot(new Plot(), SuspensionType.Rear);
            rvh.LoadTelemetryData(telemetryData);
            sessionCache.RearVelocityHistogram = rvh.Plot.GetSvgXml(width - 64, 478);
            Dispatcher.UIThread.Post(() => { DamperPage.RearVelocityHistogram = sessionCache.RearVelocityHistogram; });

            var rvb = telemetryData.CalculateVelocityBands(SuspensionType.Rear, 200);
            sessionCache.RearHsrPercentage = rvb.HighSpeedRebound;
            sessionCache.RearLsrPercentage = rvb.LowSpeedRebound;
            sessionCache.RearLscPercentage = rvb.LowSpeedCompression;
            sessionCache.RearHscPercentage = rvb.HighSpeedCompression;
            Dispatcher.UIThread.Post(() =>
            {
                DamperPage.RearHsrPercentage = rvb.HighSpeedRebound;
                DamperPage.RearLsrPercentage = rvb.LowSpeedRebound;
                DamperPage.RearLscPercentage = rvb.LowSpeedCompression;
                DamperPage.RearHscPercentage = rvb.HighSpeedCompression;
            });
        }

        if (telemetryData.Front.Present && telemetryData.Rear.Present)
        {

            var cb = new BalancePlot(new Plot(), BalanceType.Compression);
            cb.LoadTelemetryData(telemetryData);
            sessionCache.CompressionBalance = cb.Plot.GetSvgXml(width, height);
            Dispatcher.UIThread.Post(() => { BalancePage.CompressionBalance = sessionCache.CompressionBalance; });

            var rb = new BalancePlot(new Plot(), BalanceType.Rebound);
            rb.LoadTelemetryData(telemetryData);
            sessionCache.ReboundBalance = rb.Plot.GetSvgXml(width, height);
            Dispatcher.UIThread.Post(() => { BalancePage.ReboundBalance = sessionCache.ReboundBalance; });
        }
        else
        {
            Dispatcher.UIThread.Post(() => { Pages.Remove(BalancePage); });
        }
        
        await databaseService.PutSessionCacheAsync(sessionCache);
    }

    #endregion
    
    #region Constructors

    public SessionViewModel()
    {
        session = new Session();
        IsInDatabase = false;
        Pages = [SpringPage, DamperPage, BalancePage, NotesPage];
    }
    
    public SessionViewModel(Session session, bool fromDatabase)
    {
        this.session = session;
        IsInDatabase = fromDatabase;
        Pages = [SpringPage, DamperPage, BalancePage, NotesPage];

        NotesPage.ForkSettings.PropertyChanged += (_, _) => EvaluateDirtiness();
        NotesPage.ShockSettings.PropertyChanged += (_, _) => EvaluateDirtiness();
        NotesPage.PropertyChanged += (_, _) => EvaluateDirtiness();

        ResetImplementation();
    }

    #endregion

    #region ItemViewModelBase overrides
    protected override void EvaluateDirtiness()
    {
        IsDirty =
            !IsInDatabase ||
            Name != session.Name ||
            NotesPage.IsDirty(session);
    }

    protected override async Task SaveImplementation()
    {
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var newSession = new Session(
                id: session.Id,
                name: Name ?? $"session #{session.Id}",
                description: NotesPage.Description ?? $"session #{session.Id}",
                setup: session.Setup)
            {
                FrontSpringRate = NotesPage.ForkSettings.SpringRate,
                FrontHighSpeedCompression = NotesPage.ForkSettings.HighSpeedCompression,
                FrontLowSpeedCompression = NotesPage.ForkSettings.LowSpeedCompression,
                FrontLowSpeedRebound = NotesPage.ForkSettings.LowSpeedRebound,
                FrontHighSpeedRebound = NotesPage.ForkSettings.HighSpeedRebound,
                RearSpringRate = NotesPage.ShockSettings.SpringRate,
                RearHighSpeedCompression = NotesPage.ShockSettings.HighSpeedCompression,
                RearLowSpeedCompression = NotesPage.ShockSettings.LowSpeedCompression,
                RearLowSpeedRebound = NotesPage.ShockSettings.LowSpeedRebound,
                RearHighSpeedRebound = NotesPage.ShockSettings.HighSpeedRebound,
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

    protected override Task ResetImplementation()
    {
        Id = session.Id;
        Name = session.Name;

        NotesPage.Description = session.Description;
        NotesPage.ForkSettings.SpringRate = session.FrontSpringRate;
        NotesPage.ForkSettings.HighSpeedCompression = session.FrontHighSpeedCompression;
        NotesPage.ForkSettings.LowSpeedCompression = session.FrontLowSpeedCompression;
        NotesPage.ForkSettings.LowSpeedRebound = session.FrontLowSpeedRebound;
        NotesPage.ForkSettings.HighSpeedRebound = session.FrontHighSpeedRebound;

        NotesPage.ShockSettings.SpringRate = session.RearSpringRate;
        NotesPage.ShockSettings.HighSpeedCompression = session.RearHighSpeedCompression;
        NotesPage.ShockSettings.LowSpeedCompression = session.RearLowSpeedCompression;
        NotesPage.ShockSettings.LowSpeedRebound = session.RearLowSpeedRebound;
        NotesPage.ShockSettings.HighSpeedRebound = session.RearHighSpeedRebound;

        Timestamp = DateTimeOffset.FromUnixTimeSeconds(session.Timestamp ?? 0).DateTime;

        return Task.CompletedTask;
    }

    protected override async Task DeleteImplementation()
    {
        var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
        Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

        await mainPagesViewModel.DeleteSessionCommand.ExecuteAsync(this);
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

    #endregion
}