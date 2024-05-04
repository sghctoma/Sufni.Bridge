using System.Linq;
using Avalonia;
using ScottPlot;
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

    private void AddStatistics()
    {
        var statistics = Telemetry.CalculateTravelStatistics(SuspensionType);
        
        var avgPercentage = statistics.Average / Telemetry.Linkage.MaxFrontTravel * 100.0;
        var maxPercentage = statistics.Max / Telemetry.Linkage.MaxFrontTravel * 100.0;
        
        var avgString = $"{statistics.Average:F2} mm ({avgPercentage:F2}%)";
        var maxString = $"{statistics.Max:F2} mm ({maxPercentage:F2}%) / {statistics.Bottomouts} bottom outs";

        AddLabelWithHorizontalLine(avgString, statistics.Average, LabelLinePosition.Above);
        AddLabelWithHorizontalLine(maxString, statistics.Max, LabelLinePosition.Below);
    }
    
    protected override void OnTelemetryChanged(TelemetryData telemetryData)
    {
        base.OnTelemetryChanged(telemetryData);

        Plot!.Plot.Axes.Title.Label.Text = SuspensionType == SuspensionType.Front
            ? "Front travel (time% / mm)"
            : "Rear travel (time% / mm)";
        Plot!.Plot.Layout.Fixed(new PixelPadding(40, 10, 40, 40));

        var data = telemetryData.CalculateTravelHistogram(SuspensionType);
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
                Size = step * 0.65f,
            })
            .ToList();

        Plot!.Plot.Add.Bars(bars);
        Plot!.Plot.Axes.AutoScale(invertY: true);

        // Set to 0.05 to hide the border line at 0 values. Otherwise it would
        // seem that there are actual measure travel data there too.
        Plot!.Plot.Axes.SetLimits(left: 0.05);
        
        AddStatistics();
    }
}