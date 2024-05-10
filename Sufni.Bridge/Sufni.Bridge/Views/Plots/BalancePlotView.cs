using System.Diagnostics;
using Avalonia;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Plots;

namespace Sufni.Bridge.Views.Plots;

public class BalancePlotView : SufniTelemetryPlotView
{
    public static readonly StyledProperty<BalanceType> BalanceTypeProperty = AvaloniaProperty.Register<BalancePlotView, BalanceType>(
        "BalanceType");

    public BalanceType BalanceType
    {
        get => GetValue(BalanceTypeProperty);
        set => SetValue(BalanceTypeProperty, value);
    }
    
    protected override void CreatePlot()
    {
        Debug.Assert(AvaPlot != null, nameof(AvaPlot) + " != null");
        Plot = new BalancePlot(AvaPlot.Plot, BalanceType);
    }
}