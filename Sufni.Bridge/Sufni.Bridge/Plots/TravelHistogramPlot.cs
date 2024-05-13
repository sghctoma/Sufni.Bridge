using System.Linq;
using ScottPlot;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Plots;

public class TravelHistogramPlot(Plot plot, SuspensionType type) : TelemetryPlot(plot)
{
    private void AddStatistics(TelemetryData telemetryData)
    {
        var statistics = telemetryData.CalculateTravelStatistics(type);

        var mx = type == SuspensionType.Front
            ? telemetryData.Linkage.MaxFrontTravel
            : telemetryData.Linkage.MaxRearTravel;
        var avgPercentage = statistics.Average / mx * 100.0;
        var maxPercentage = statistics.Max / mx * 100.0;
        
        var avgString = $"{statistics.Average:F2} mm ({avgPercentage:F2}%)";
        var maxString = $"{statistics.Max:F2} mm ({maxPercentage:F2}%) / {statistics.Bottomouts} bottom outs";

        AddLabelWithHorizontalLine(avgString, statistics.Average, LabelLinePosition.Above);
        AddLabelWithHorizontalLine(maxString, statistics.Max, LabelLinePosition.Below);
    }
    
    public override void LoadTelemetryData(TelemetryData telemetryData)
    {
        base.LoadTelemetryData(telemetryData);

        Plot.Axes.Title.Label.Text = type == SuspensionType.Front
            ? "Front travel (time% / mm)"
            : "Rear travel (time% / mm)";
        Plot.Layout.Fixed(new PixelPadding(40, 10, 40, 40));

        var data = telemetryData.CalculateTravelHistogram(type);
        var step = data.Bins[1] - data.Bins[0];
        var color = type == SuspensionType.Front ? FrontColor : RearColor;
        var bars = data.Bins.Zip(data.Values)
            .Select(tuple => new Bar
            {
                Position = tuple.First,
                Value = tuple.Second,
                FillColor = color.WithOpacity(),
                BorderColor = color,
                BorderLineWidth = 1.5f,
                Orientation = Orientation.Horizontal,
                Size = step * 0.65f,
            })
            .ToList();

        Plot.Add.Bars(bars);
        Plot.Axes.AutoScale(invertY: true);

        // Set to 0.05 to hide the border line at 0 values. Otherwise it would
        // seem that there are actual measure travel data there too.
        Plot.Axes.SetLimits(left: 0.05);
        
        AddStatistics(telemetryData);
    }
}