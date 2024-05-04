using System.Linq;
using Avalonia;
using ScottPlot;
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

    private void AddStatistics()
    {
        var statistics = Telemetry.CalculateVelocityStatistics(SuspensionType);
        
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
                Plot!.Plot.Axes.GetLimits().Right,
                -2000.0,
                -10,
                5,
                Alignment.UpperRight);
        }
        else
        {
            AddLabelWithHorizontalLine(maxReboundVelString,
                statistics.MaxRebound,
                LabelLinePosition.Above);
        }
        
        // Average values should be between the hardcoded limits, it's safe to draw them 
        // at their actual position.
        AddLabelWithHorizontalLine(avgReboundVelString, statistics.AverageRebound, LabelLinePosition.Below);
        AddLabelWithHorizontalLine(avgCompVelString, statistics.AverageCompression, LabelLinePosition.Above);
        
        // If max compression is more than VelocityLimit (which is the hardcoded axis limit),
        // we draw the the label at VelocityLimit, and omit the line.
        if (statistics.MaxCompression > VelocityLimit)
        {
            AddLabel(
                maxCompVelString,
                Plot!.Plot.Axes.GetLimits().Right,
                2000.0,
                -10,
                -5,
                Alignment.LowerRight);
        }
        else
        {
            AddLabelWithHorizontalLine(maxCompVelString,
                statistics.MaxCompression,
                LabelLinePosition.Below);
        }
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Axes.Title.Label.Text = SuspensionType == SuspensionType.Front
            ? "Front velocity (time% / mm/s)"
            : "Rear velocity (time% / mm/s)";
        Plot!.Plot.Layout.Fixed(new PixelPadding(40, 5, 40, 40));

        var data = telemetryData.CalculateVelocityHistogram(SuspensionType);
        var step = data.Bins[1] - data.Bins[0];
        
        var color = SuspensionType == SuspensionType.Front ? FrontColor : RearColor;
        var bars = data.Bins.Zip(data.Values)
            .Select(tuple => new Bar
            {
                Position = tuple.First,
                Value = tuple.Second,
                FillColor = color.WithOpacity(),
                BorderColor = color,
                BorderLineWidth = 1.5f,
                Orientation = Orientation.Horizontal,
                Size = step * 0.65,
            })
            .ToList();
        
        Plot!.Plot.Add.Bars(bars);
        Plot!.Plot.Axes.AutoScale(invertY: true);
        
        // Set left axis limit to 0.1 to hide the border line at 0 values. Otherwise
        // it would seem that there are actual measure travel data there too.
        // Also set a hardcoded limit for the velocity range.
        Plot!.Plot.Axes.SetLimits(left: 0.1,
            bottom: VelocityLimit,
            top: -VelocityLimit);
        
        Plot!.Plot.Axes.Left.TickGenerator = new NumericFixedInterval(500);
        
        var normalData = telemetryData.CalculateNormalDistribution(SuspensionType);
        var normal = Plot!.Plot.Add.Scatter(
            normalData.Pdf.ToArray(),
            normalData.Y.ToArray());
        normal.Color = Color.FromHex("#d53e4f");
        normal.MarkerStyle.IsVisible = false;
        normal.LineStyle.Width = 3;
        normal.LineStyle.Pattern = LinePattern.Dotted;

        AddStatistics();
    }
}