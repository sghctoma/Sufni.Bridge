using System.Collections.Generic;
using System.Linq;
using Avalonia;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views;

public class VelocityHistogramView : SufniPlotView
{
    private const double VelocityLimit = 2000.0;
    
    public static readonly StyledProperty<SuspensionType> TypeProperty = AvaloniaProperty.Register<TravelHistogramView, SuspensionType>(
        "Type");

    public SuspensionType Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Title(Type == SuspensionType.Front ? "Front velocity histogram" : "Rear velocity histogram");
        Plot!.Plot.LeftAxis.Label.Text = "Speed (mm/s)";
        Plot!.Plot.BottomAxis.Label.Text = "Time (%)";

        var data = telemetryData.CalculateVelocityHistogram(Type);

        // Bar width is assumed to be 1 in ScottPlot 5, so scale the bins to accomodate this.
        // Also, flip the bin values manually, since there is no way to flip the axis itself.
        var min = data.Bins.Min();
        var max = data.Bins.Max();
        var step = data.Bins[1] - data.Bins[0];
        
        var bars = data.Bins.Zip(data.Values)
            .Select(tuple => new Bar((max - tuple.First + min) / step, tuple.Second))
            .ToList();
        
        BarSeries bs = new()
        {
            Bars = bars,
            Color = (Type == SuspensionType.Front ? FrontColor : RearColor).WithOpacity()
        };
        var histogram = Plot!.Plot.Add.Bar(new List<BarSeries> { bs });
        histogram.Orientation = Orientation.Horizontal;
        histogram.LineStyle.Width = 2.0f;
        histogram.LineStyle.Color = Type == SuspensionType.Front ? FrontColor : RearColor;
        histogram.Padding = 0.15;
        
        Plot!.Plot.AutoScale();
        
        // Set left axis limit to 0.1 to hide the border line at 0 values. Otherwise
        // it would seem that there are actual measure travel data there too.
        // Also set a hardcoded limit for the velocity range.
        Plot!.Plot.SetAxisLimits(left: 0.1,
            bottom: (max - VelocityLimit + min) / step,
            top: (max - -VelocityLimit + min) / step);
        
        // Fix the tick values, since they are scaled and flipped.
        const int increment = 500;
        var ticks = Enumerable.Range(0, (2 * (int)VelocityLimit) / increment + 1)
            .Select(i => -VelocityLimit + i * increment)
            .TakeWhile(value => value <= VelocityLimit)
            .ToArray();
        
        Plot!.Plot.LeftAxis.TickGenerator = new NumericManual(
            ticks.Select(b => (max - b + min) / step).ToArray(),
            ticks.Select(b => $"{b:0}").ToArray());
    }
}