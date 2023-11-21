using System.Collections.Generic;
using System.Linq;
using Avalonia;
using ScottPlot;
using ScottPlot.Plottables;
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

    private void AddStatistics(double maxTravel, double step)
    {
        var statistics = Telemetry.CalculateTravelStatistics(SuspensionType);
        
        var avgPercentage = statistics.Average / Telemetry.Linkage.MaxFrontTravel * 100.0;
        var maxPercentage = statistics.Max / Telemetry.Linkage.MaxFrontTravel * 100.0;
        
        var avgString = $"{statistics.Average:F2} mm ({avgPercentage:F2}%)";
        var maxString = $"{statistics.Max:F2} mm ({maxPercentage:F2}%) / {statistics.Bottomouts} bottom outs";

        AddLabelWithHorizontalLine(avgString, (maxTravel - statistics.Average) / step, LabelLinePosition.Above);
        AddLabelWithHorizontalLine(maxString, (maxTravel - statistics.Max) / step, LabelLinePosition.Below);
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Title(SuspensionType == SuspensionType.Front ? "Front travel histogram" : "Rear travel histogram");
        Plot!.Plot.LeftAxis.Label.Text = "Travel (mm)";
        Plot!.Plot.BottomAxis.Label.Text = "Time (%)";

        var data = telemetryData.CalculateTravelHistogram(SuspensionType);
        
        // Bar width is assumed to be 1 in ScottPlot 5, so scale the bins to accomodate this.
        // Also, flip the bin values manually, since there is no way to flip the axis itself.
        var maxTravel = SuspensionType == SuspensionType.Front ? 
            telemetryData.Linkage.MaxFrontTravel : 
            telemetryData.Linkage.MaxRearTravel;
        var step = data.Bins[1] - data.Bins[0];
        
        var bars = data.Bins.Zip(data.Values)
            .Select(tuple => new Bar((maxTravel - tuple.First) / step, tuple.Second))
            .ToList();
        
        BarSeries bs = new()
        {
            Bars = bars,
            Color = (SuspensionType == SuspensionType.Front ? FrontColor : RearColor).WithOpacity()
        };
        var histogram = Plot!.Plot.Add.Bar(new List<BarSeries> { bs });
        histogram.Orientation = Orientation.Horizontal;
        histogram.LineStyle.Width = 2.0f;
        histogram.LineStyle.Color = SuspensionType == SuspensionType.Front ? FrontColor : RearColor;
        histogram.Padding = 0.15;
        
        Plot!.Plot.AutoScale();
        
        // Set to 0.01 to hide the border line at 0 values. Otherwise it would
        // seem that there are actual measure travel data there too.
        Plot!.Plot.SetAxisLimits(left: 0.05);
        
        // Fix the tick values, since they are scaled and flipped.
        var ticks = Enumerable.Range(0, ((int)maxTravel + 50 - 1) / 50)
            .Select(i => i * 50)
            .TakeWhile(value => value <= maxTravel)
            .ToArray();
        Plot!.Plot.LeftAxis.TickGenerator = new NumericManual(
            ticks.Select(b => (maxTravel - b) / step).ToArray(),
            ticks.Select(b => $"{b:0}").ToArray());
        
        AddStatistics(maxTravel, step);
    }
}