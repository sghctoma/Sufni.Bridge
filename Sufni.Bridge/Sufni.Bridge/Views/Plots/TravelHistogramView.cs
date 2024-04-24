using System.Linq;
using Avalonia;
using ScottPlot;
using ScottPlot.TickGenerators;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

public class TravelHistogramView : SufniTelemetryPlotView
{
    public static readonly StyledProperty<SuspensionType> SuspensionTypeProperty = AvaloniaProperty.Register<TravelHistogramView, SuspensionType>(
        "SuspensionType");

    public SuspensionType SuspensionType
    {
        get => GetValue(SuspensionTypeProperty);
        set => SetValue(SuspensionTypeProperty, value);
    }

    private void AddStatistics(double maxTravel)
    {
        var statistics = Telemetry.CalculateTravelStatistics(SuspensionType);
        
        var avgPercentage = statistics.Average / Telemetry.Linkage.MaxFrontTravel * 100.0;
        var maxPercentage = statistics.Max / Telemetry.Linkage.MaxFrontTravel * 100.0;
        
        var avgString = $"{statistics.Average:F2} mm ({avgPercentage:F2}%)";
        var maxString = $"{statistics.Max:F2} mm ({maxPercentage:F2}%) / {statistics.Bottomouts} bottom outs";

        AddLabelWithHorizontalLine(avgString, maxTravel - statistics.Average, LabelLinePosition.Above);
        AddLabelWithHorizontalLine(maxString, maxTravel - statistics.Max, LabelLinePosition.Below);
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Axes.Title.Label.Text = SuspensionType == SuspensionType.Front
            ? "Front travel histogram (time% / mm)"
            : "Rear travel histogram (time% / mm)";
        Plot!.Plot.Layout.Fixed(new PixelPadding(40, 10, 40, 40));

        var data = telemetryData.CalculateTravelHistogram(SuspensionType);
        
        var maxTravel = SuspensionType == SuspensionType.Front ? 
            telemetryData.Linkage.MaxFrontTravel : 
            telemetryData.Linkage.MaxRearTravel;
        var step = data.Bins[1] - data.Bins[0];

        var color = SuspensionType == SuspensionType.Front ? FrontColor : RearColor;
        var bars = data.Bins.Zip(data.Values)
            .Select(tuple => new Bar
            {
                Position = maxTravel - tuple.First, // Flip bin values, since there is no way to flip the axis itself.
                Value = tuple.Second,
                FillColor = color.WithOpacity(),
                BorderColor = color,
                BorderLineWidth = 1.5f,
                Orientation = Orientation.Horizontal,
                Size = step * 0.65f,
            })
            .ToList();

        Plot!.Plot.Add.Bars(bars);
        Plot!.Plot.Axes.AutoScale();
        
        // Set to 0.05 to hide the border line at 0 values. Otherwise it would
        // seem that there are actual measure travel data there too.
        Plot!.Plot.Axes.SetLimits(left: 0.05);
        
        // Fix the tick values, since they are flipped.
        var ticks = Enumerable.Range(0, ((int)maxTravel + 50 - 1) / 50)
            .Select(i => i * 50)
            .TakeWhile(value => value <= maxTravel)
            .ToArray();
        Plot!.Plot.Axes.Left.TickGenerator = new NumericManual(
            ticks.Select(b => maxTravel - b).ToArray(),
            ticks.Select(b => $"{b:0}").ToArray());
        
        AddStatistics(maxTravel);
    }
}