using System.Diagnostics;
using Avalonia;
using ScottPlot;
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
        // Populate the ScottPlot plot when the Telemetry property is set.
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
        
        // When previously not displayed plots are loaded (e.g. when they are on a TabItem
        // we have just switched to), the Telemetry property is already set (it's static),
        // but we still need to populate the ScottPlot plot.
        Loaded += (sender, _) =>
        {
            Debug.Assert(Plot != null, nameof(Plot) + " != null");

            if (sender is not SufniTelemetryPlotView { Telemetry: not null } sufniTelemetryPlotView) return;

            Plot.Plot.Clear();
            OnTelemetryChanged(sufniTelemetryPlotView.Telemetry);
        };
    }

    protected enum LabelLinePosition
    {
        Below,
        Above
    }

    protected void AddLabel(string content, double x, double y, int xoffset, int yoffset, Alignment alignment = Alignment.LowerLeft)
    {
        var text = Plot!.Plot.Add.Text(content, x, y);
        text.LabelFontColor = Color.FromHex("#fefefe");
        text.LabelFontSize = 13;
        text.LabelAlignment = alignment;
        text.LabelOffsetX = xoffset;
        text.LabelOffsetY = yoffset;
    }
    
    protected void AddLabelWithHorizontalLine(string content, double position, LabelLinePosition linePosition)
    {
        var yoffset = linePosition switch
        {
            LabelLinePosition.Above => 5,
            LabelLinePosition.Below => -5,
            _ => 0
        };
        
        var text = Plot!.Plot.Add.Text(content, Plot!.Plot.Axes.GetLimits().Right, position);
        text.LabelFontColor = Color.FromHex("#fefefe");
        text.LabelFontSize = 13;
        text.LabelAlignment = linePosition == LabelLinePosition.Above ? Alignment.UpperRight : Alignment.LowerRight;
        text.LabelOffsetX = -10;
        text.LabelOffsetY = yoffset;
        
        Plot!.Plot.Add.HorizontalLine(position, 1f, Color.FromHex("#dddddd"), LinePattern.Dotted);
    }

    protected virtual void OnTelemetryChanged(TelemetryData telemetryData) { }
}