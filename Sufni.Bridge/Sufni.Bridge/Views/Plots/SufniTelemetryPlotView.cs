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

    protected enum LabelLinePosition
    {
        Below,
        Above
    }

    protected void AddLabel(string content, double x, double y, int xoffset, int yoffset, Alignment alignment = Alignment.LowerLeft)
    {
        var text = Plot!.Plot.Add.Text(content, x, y);
        text.Label.ForeColor = Color.FromHex("#fefefe");
        text.Label.FontSize = 13;
        text.Label.Alignment = alignment;
        text.Label.OffsetX = xoffset;
        text.Label.OffsetY = yoffset;
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
        text.Label.ForeColor = Color.FromHex("#fefefe");
        text.Label.FontSize = 13;
        text.Label.Alignment = linePosition == LabelLinePosition.Above ? Alignment.UpperRight : Alignment.LowerRight;
        text.Label.OffsetX = -10;
        text.Label.OffsetY = yoffset;

        var line = Plot!.Plot.Add.Crosshair(0, position);
        line.VerticalLineIsVisible = false;
        line.LineStyle.Pattern = LinePattern.Dotted;
        line.LineStyle.Color = Color.FromHex("#dddddd");
    }

    protected virtual void OnTelemetryChanged(TelemetryData telemetryData) { }
}