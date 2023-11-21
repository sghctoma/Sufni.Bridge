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
        Text text = new()
        {
            Font =
            {
                Color = Color.FromHex("#fefefe"),
                Size = 13
            },
            Content = content,
            Alignment = alignment,
            XOffset = xoffset,
            YOffset = yoffset,
            Position = new Coordinates(x, y)
        };
        Plot!.Plot.Add.Plottable(text);
    }
    
    protected void AddLabelWithHorizontalLine(string content, double position, LabelLinePosition linePosition)
    {
        var yoffset = linePosition switch
        {
            LabelLinePosition.Above => -5,
            LabelLinePosition.Below => 5,
            _ => 0
        };

        Text text = new()
        {
            Font =
            {
                Color = Color.FromHex("#fefefe"),
                Size = 13
            },
            Content = content,
            Alignment = linePosition == LabelLinePosition.Above ? Alignment.UpperRight : Alignment.LowerRight,
            XOffset = -10,
            YOffset = yoffset,
            Position = new Coordinates(Plot!.Plot.GetAxisLimits().Right, position)
        };
        Plot!.Plot.Add.Plottable(text);

        var line = Plot!.Plot.Add.Crosshair(0, position);
        line.VerticalLineIsVisible = false;
        line.LineStyle.Pattern = LinePattern.Dot;
        line.LineStyle.Color = Color.FromHex("#dddddd");
    }

    protected virtual void OnTelemetryChanged(TelemetryData telemetryData) { }
}