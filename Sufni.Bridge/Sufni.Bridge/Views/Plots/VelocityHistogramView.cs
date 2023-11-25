using System.Collections.Generic;
using System.Linq;
using Avalonia;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

public class VelocityHistogramView : SufniTelemetryPlotView
{
    private const double VelocityLimit = 2000.0;
    
    public static readonly StyledProperty<SuspensionType> SuspensionTypeProperty = AvaloniaProperty.Register<TravelHistogramView, SuspensionType>(
        "SuspensionType");

    public SuspensionType SuspensionType
    {
        get => GetValue(SuspensionTypeProperty);
        set => SetValue(SuspensionTypeProperty, value);
    }

    private void AddStatistics(double maxPlusMinVelocity, double step)
    {
        var statistics = Telemetry.CalculateVelocityStatistics(SuspensionType);
        
        /*var maxReboundVelString = $"max. rebound. vel.: {statistics.MaxRebound:0.00} mm/s";
        var avgReboundVelString = $"avg. rebound. vel.: {statistics.AverageRebound:0.00} mm/s";
        var avgCompVelString = $"avg. comp. vel.: {statistics.AverageCompression:0.00} mm/s";
        var maxCompVelString = $"max. comp. vel.: {statistics.MaxCompression:0.00} mm/s";*/
        
        var maxReboundVelString = $"{statistics.MaxRebound:0.00} mm/s";
        var avgReboundVelString = $"{statistics.AverageRebound:0.00} mm/s";
        var avgCompVelString = $"{statistics.AverageCompression:0.00} mm/s";
        var maxCompVelString = $"{statistics.MaxCompression:0.00} mm/s";
        
        // If max rebound is lower than -VelocityLimit (which is the hardcoded axis limit),
        // we draw the the label at -VelocityLimit, and omit the line.
        if (statistics.MaxRebound < -VelocityLimit)
        {
            AddLabel(
                maxReboundVelString,
                Plot!.Plot.GetAxisLimits().Right,
                (maxPlusMinVelocity - -2000.0) / step,
                -10,
                -5,
                Alignment.UpperRight);
        }
        else
        {
            AddLabelWithHorizontalLine(maxReboundVelString,
                (maxPlusMinVelocity - statistics.MaxRebound) / step,
                LabelLinePosition.Above);
        }
        
        // Average values should be between the hardcoded limits, it's safe to draw them 
        // at their actual position.
        AddLabelWithHorizontalLine(avgReboundVelString, (maxPlusMinVelocity - statistics.AverageRebound) / step, LabelLinePosition.Below);
        AddLabelWithHorizontalLine(avgCompVelString, (maxPlusMinVelocity - statistics.AverageCompression) / step, LabelLinePosition.Above);
        
        // If max compression is more than VelocityLimit (which is the hardcoded axis limit),
        // we draw the the label at VelocityLimit, and omit the line.
        if (statistics.MaxCompression > VelocityLimit)
        {
            AddLabel(
                maxCompVelString,
                Plot!.Plot.GetAxisLimits().Right,
                (maxPlusMinVelocity - 2000.0) / step,
                -10,
                5,
                Alignment.LowerRight);
        }
        else
        {
            AddLabelWithHorizontalLine(maxCompVelString,
                (maxPlusMinVelocity - statistics.MaxCompression) / step,
                LabelLinePosition.Below);
        }
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Title(SuspensionType == SuspensionType.Front ? "Front velocity histogram" : "Rear velocity histogram");
        Plot!.Plot.LeftAxis.Label.Text = "Speed (mm/s)";
        Plot!.Plot.BottomAxis.Label.Text = "Time (%)";

        var data = telemetryData.CalculateVelocityHistogram(SuspensionType);

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
            Color = (SuspensionType == SuspensionType.Front ? FrontColor : RearColor).WithOpacity()
        };
        var histogram = Plot!.Plot.Add.Bar(new List<BarSeries> { bs });
        histogram.Orientation = Orientation.Horizontal;
        histogram.LineStyle.Width = 2.0f;
        histogram.LineStyle.Color = SuspensionType == SuspensionType.Front ? FrontColor : RearColor;
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
        
        var normalData = telemetryData.CalculateNormalDistribution(SuspensionType);
        var normal = Plot!.Plot.Add.Scatter(
            normalData.Pdf.ToArray(),
            normalData.Y.Select(y => (max - y + min) / step).ToArray());
        normal.Color = Color.FromHex("#d53e4f");
        normal.MarkerStyle.IsVisible = false;
        normal.LineStyle.Width = 3;
        normal.LineStyle.Pattern = LinePattern.Dot;

        AddStatistics(max + min, step);
    }
}