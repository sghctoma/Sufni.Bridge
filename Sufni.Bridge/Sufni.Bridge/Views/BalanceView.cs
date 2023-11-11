using System;
using System.Linq;
using Avalonia;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views;

public class BalanceView : SufniPlotView
{
    public static readonly StyledProperty<BalanceType> TypeProperty = AvaloniaProperty.Register<BalanceView, BalanceType>(
        "Type");

    public BalanceType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Title(Type == BalanceType.Compression ? "Compression balance" : "Rebound balance");
        Plot!.Plot.BottomAxis.Label.Text = "Travel (%)";
        Plot!.Plot.LeftAxis.Label.Text = "Velocity (mm/s)";

        var balance = telemetryData.CalculateBalance(Type);

        var maxTravel = Math.Max(balance.FrontTravel.Max(), balance.RearTravel.Max());
        var maxVelocity = Math.Max(balance.FrontVelocity.Max(), balance.RearVelocity.Max());
        Plot!.Plot.SetAxisLimits(0, maxTravel, 0, maxVelocity);
        
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
    }
}