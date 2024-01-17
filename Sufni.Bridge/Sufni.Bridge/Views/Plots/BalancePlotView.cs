using System;
using System.Linq;
using Avalonia;
using ScottPlot;
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
        
        var maxTravel = Math.Max(
            balance.FrontTravel.Max(),
            balance.RearTravel.Max());
            
        var msd = balance.MeanSignedDeviation / maxVelocity * 100.0;
        var msdString = $"MSD: {msd:+0.00;-#.00} %";

        AddLabel(msdString, maxTravel, 0, -10, -5, Alignment.LowerRight);
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Axes.Title.Label.Text = BalanceType == BalanceType.Compression 
            ? "Compression balance" 
            : "Rebound balance";
        Plot!.Plot.Layout.Fixed(new PixelPadding(60, 10, 40, 40));
        Plot!.Plot.Axes.Bottom.Label.Text = "Travel (%)";
        Plot!.Plot.Axes.Left.Label.Text = "Velocity (mm/s)";

        var balance = telemetryData.CalculateBalance(BalanceType);

        var maxTravel = Math.Max(balance.FrontTravel.Max(), balance.RearTravel.Max());
        var maxVelocity = Math.Max(balance.FrontVelocity.Max(), balance.RearVelocity.Max());
        Plot!.Plot.Axes.SetLimits(0, maxTravel, 0, maxVelocity);
        
        var front = Plot!.Plot.Add.Scatter(balance.FrontTravel, balance.FrontVelocity);
        front.LineStyle.IsVisible = false;
        front.MarkerStyle.Fill.Color = FrontColor.WithOpacity();
        front.MarkerStyle.Size = 5;

        var frontTrend = Plot!.Plot.Add.Scatter(balance.FrontTravel, balance.FrontTrend);
        frontTrend.MarkerStyle.IsVisible = false;
        frontTrend.LineStyle.Color = FrontColor;
        frontTrend.LineStyle.Width = 2;
        
        var rear = Plot!.Plot.Add.Scatter(balance.RearTravel, balance.RearVelocity);
        rear.LineStyle.IsVisible = false;
        rear.MarkerStyle.Fill.Color = RearColor.WithOpacity();
        rear.MarkerStyle.Size = 5;
        
        var rearTrend = Plot!.Plot.Add.Scatter(balance.RearTravel, balance.RearTrend);
        rearTrend.MarkerStyle.IsVisible = false;
        rearTrend.LineStyle.Color = RearColor;
        rearTrend.LineStyle.Width = 2;
        
        AddStatistics();
    }
}