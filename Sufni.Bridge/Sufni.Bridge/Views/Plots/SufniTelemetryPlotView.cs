using Avalonia;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

public class SufniTelemetryPlotView : SufniPlotView
{
    #region Styled properties
    
    public static readonly StyledProperty<TelemetryData> TelemetryProperty =
        AvaloniaProperty.Register<SufniTelemetryPlotView, TelemetryData>(nameof(Telemetry));
    
    public TelemetryData Telemetry
    {
        get => GetValue(TelemetryProperty);
        set => SetValue(TelemetryProperty, value);
    }
    
    #endregion

    public SufniTelemetryPlotView()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.NewValue is null || Plot is null) return;
            var plot = Plot.Plot;
            
            switch (e.Property.Name)
            {
                case nameof(Telemetry):
                    plot.Clear();
                    OnTelemetryChanged((TelemetryData)e.NewValue);
                    break;
            }

            Plot.Refresh();
        };
    }
    
    protected virtual void OnTelemetryChanged(TelemetryData telemetryData) { }
}