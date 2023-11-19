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

        Plot!.Plot.Title(Type == SuspensionType.Front ? "Front travel histogram" : "Rear travel histogram");
        Plot!.Plot.LeftAxis.Label.Text = "Travel (mm)";
        Plot!.Plot.BottomAxis.Label.Text = "Time (%)";

        var data = telemetryData.CalculateTravelHistogram(Type);
        
        // Bar width is assumed to be 1 in ScottPlot 5, so scale the bins to accomodate this.
        // Also, flip the bin values manually, since there is no way to flip the axis itself.
        var maxTravel = Type == SuspensionType.Front ? 
            telemetryData.Linkage.MaxFrontTravel : 
            telemetryData.Linkage.MaxRearTravel;
        var step = data.Bins[1] - data.Bins[0];
        
        var bars = data.Bins.Zip(data.Values)
            .Select(tuple => new Bar((maxTravel - tuple.First) / step, tuple.Second))
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
        
        // Set to 0.1 to hide the border line at 0 values. Otherwise it would
        // seem that there are actual measure travel data there too.
        Plot!.Plot.SetAxisLimits(left: 0.1);
        
        // Fix the tick values, since they are scaled and flipped.
        var ticks = Enumerable.Range(0, ((int)maxTravel + 50 - 1) / 50)
            .Select(i => i * 50)
            .TakeWhile(value => value <= maxTravel)
            .ToArray();
        Plot!.Plot.LeftAxis.TickGenerator = new NumericManual(
            ticks.Select(b => (maxTravel - b) / step).ToArray(),
            ticks.Select(b => $"{b:0}").ToArray());
    }
}