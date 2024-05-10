using System.Diagnostics;
using Avalonia;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Plots;

namespace Sufni.Bridge.Views.Plots;

public abstract class SufniTelemetryPlotView : SufniPlotView
{
    protected TelemetryPlot? Plot;
    
    public static readonly StyledProperty<TelemetryData> TelemetryProperty =
        AvaloniaProperty.Register<SufniTelemetryPlotView, TelemetryData>(nameof(Telemetry));
    
    public TelemetryData Telemetry
    {
        get => GetValue(TelemetryProperty);
        set => SetValue(TelemetryProperty, value);
    }
    
    protected SufniTelemetryPlotView()
    {
        // Populate the ScottPlot plot when the Telemetry property is set.
        PropertyChanged += (_, e) =>
        {
            if (e.NewValue is null || AvaPlot is null || Plot is null) return;
            
            switch (e.Property.Name)
            {
                case nameof(Telemetry):
                    Plot.Plot.Clear();
                    Plot.LoadTelemetryData((TelemetryData)e.NewValue);
                    break;
            }

            AvaPlot.Refresh();
        };
        
        // When previously not displayed plots are loaded (e.g. when they are on a TabItem
        // we have just switched to), the Telemetry property is already set (it's static),
        // but we still need to populate the ScottPlot plot.
        Loaded += (sender, _) =>
        {
            Debug.Assert(Plot != null, nameof(Plot) + " != null");

            if (sender is not SufniTelemetryPlotView { Telemetry: not null } sufniTelemetryPlotView) return;

            Plot.Plot.Clear();
            Plot.LoadTelemetryData(sufniTelemetryPlotView.Telemetry);
        };
    }
}