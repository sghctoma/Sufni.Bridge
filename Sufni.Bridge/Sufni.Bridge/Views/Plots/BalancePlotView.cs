using System;
using System.Linq;
using Avalonia;
using ScottPlot;
using ScottPlot.TickGenerators;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

public class BalancePlotView : SufniTelemetryPlotView
{
    public static readonly StyledProperty<BalanceType> BalanceTypeProperty = AvaloniaProperty.Register<BalancePlotView, BalanceType>(
        "BalanceType");

    public BalanceType BalanceType
    {
        get => GetValue(BalanceTypeProperty);
        set => SetValue(BalanceTypeProperty, value);
    }
    
    private void AddStatistics()
    {
        var balance = Telemetry.CalculateBalance(BalanceType);

        var maxVelocity = Math.Max(
            balance.FrontVelocity.Max(),
            balance.RearVelocity.Max());

        var msd = balance.MeanSignedDeviation / maxVelocity * 100.0;
        var msdString = $"MSD: {msd:+0.00;-#.00} %";

        AddLabel(msdString, 100, 0, -10, -5, Alignment.LowerRight);
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Axes.Title.Label.Text = BalanceType == BalanceType.Compression 
            ? "Compression balance (mm/s / travel%)" 
            : "Rebound balance (mm/s / travel%)";
        Plot!.Plot.Layout.Fixed(new PixelPadding(40, 10, 40, 40));

        var balance = telemetryData.CalculateBalance(BalanceType);

        var maxVelocity = Math.Max(balance.FrontVelocity.Max(), balance.RearVelocity.Max());
        var roundedMaxVelocity = (int)Math.Ceiling(maxVelocity / 100.0) * 100;
        Plot!.Plot.Axes.SetLimits(0, 100, 0, roundedMaxVelocity);
        
        var tickInterval = (int)Math.Ceiling(maxVelocity / 5 / 100.0) * 100;
        Plot!.Plot.Axes.Left.TickGenerator = new NumericFixedInterval(tickInterval); 
        Plot!.Plot.Axes.Bottom.TickGenerator = new NumericManual(
            [0.0, 10.0, 20.0, 30.0, 40.0, 50.0, 60.0, 70.0, 80.0, 90.0, 100.0],
            ["0", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100"]);
        
        var front = Plot!.Plot.Add.Scatter(balance.FrontTravel, balance.FrontVelocity);
        front.LineStyle.IsVisible = false;
        front.MarkerStyle.LineColor = FrontColor.WithOpacity();
        front.MarkerStyle.FillColor = FrontColor.WithOpacity();
        front.MarkerStyle.Size = 5;

        var frontTrend = Plot!.Plot.Add.Scatter(balance.FrontTravel, balance.FrontTrend);
        frontTrend.MarkerStyle.IsVisible = false;
        frontTrend.LineStyle.Color = FrontColor;
        frontTrend.LineStyle.Width = 2;
        
        var rear = Plot!.Plot.Add.Scatter(balance.RearTravel, balance.RearVelocity);
        rear.LineStyle.IsVisible = false;
        rear.MarkerStyle.LineColor = RearColor.WithOpacity();
        rear.MarkerStyle.FillColor = RearColor.WithOpacity();
        rear.MarkerStyle.Size = 5;
        
        var rearTrend = Plot!.Plot.Add.Scatter(balance.RearTravel, balance.RearTrend);
        rearTrend.MarkerStyle.IsVisible = false;
        rearTrend.LineStyle.Color = RearColor;
        rearTrend.LineStyle.Width = 2;
        
        AddStatistics();
    }
}