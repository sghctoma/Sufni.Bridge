using System.Diagnostics;
using Avalonia;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Plots;

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
    
    protected override void CreatePlot()
    {
        Debug.Assert(AvaPlot != null, nameof(AvaPlot) + " != null");
        Plot = new TravelHistogramPlot(AvaPlot.Plot, SuspensionType);
    }
}