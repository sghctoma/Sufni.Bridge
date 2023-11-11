using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ScottPlot.Avalonia;
using Sufni.Bridge.Models.Telemetry;
using Color = ScottPlot.Color;

namespace Sufni.Bridge.Views;

public class SufniPlotView : TemplatedControl
{
    protected AvaPlot? Plot;
    protected Color FrontColor = Color.FromHex("#3288bd");
    protected Color RearColor = Color.FromHex("#66c2a5");

    #region Styled properties
    
    public static readonly StyledProperty<TelemetryData> TelemetryProperty =
        AvaloniaProperty.Register<SufniPlotView, TelemetryData>(nameof(Telemetry));
    
    public TelemetryData Telemetry
    {
        get => GetValue(TelemetryProperty);
        set => SetValue(TelemetryProperty, value);
    }
    
    #endregion
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        Plot = e.NameScope.Find<AvaPlot>("Plot");

        /*
         * XXX It's easier to just set IsHitTestVisible="False" on the control for now...
         * 
        Plot!.Interaction.Actions.ToggleBenchmark = _ => { };
        Plot!.Interaction.Actions.DragZoom = (_, _, _) => { };
        Plot!.Interaction.Actions.DragZoomRectangle = (_, _, _) => { };
        Plot!.Interaction.Actions.AutoScale = _ => { };
        Plot!.Interaction.Actions.ZoomIn = (_, _, _) => { };
        Plot!.Interaction.Actions.ZoomOut = (_, _, _) => { };
        Plot!.Interaction.Actions.PanUp = _ => { };
        Plot!.Interaction.Actions.PanDown = _ => { };
        Plot!.Interaction.Actions.PanLeft = _ => { };
        Plot!.Interaction.Actions.PanRight = _ => { };
        Plot!.Interaction.Actions.DragPan = (_, _, _) => { };
        */

        var plot = Plot!.Plot;
        
        plot.Style.Background(Color.FromHex("#15191C"), Color.FromHex("#20262B"));
        plot.Style.ColorGrids(Color.FromHex("#505558"), Color.FromHex("#505558"));
        plot.Style.ColorAxes(Color.FromHex("#505558"));
        
        plot.TitlePanel.Label.Font.Size = 14;
        plot.TitlePanel.Label.Font.Color = Color.FromHex("#D0D0D0");
        
        plot.LeftAxis.Label.Font.Color = Color.FromHex("#D0D0D0");
        plot.LeftAxis.MajorTickColor = Color.FromHex("#D0D0D0");
        plot.LeftAxis.Label.Font.Bold = false;
        plot.LeftAxis.Label.Font.Size = 14;

        plot.XAxis.TickFont.Bold = true;
        plot.XAxis.TickFont.Size = 12;
        plot.XAxis.MajorTickLength = 0;
        plot.XAxis.MinorTickLength = 0;
        plot.XAxis.MajorTickWidth = 0;
        plot.XAxis.MinorTickWidth = 0;
        
        plot.BottomAxis.Label.Font.Color = Color.FromHex("#D0D0D0");
        plot.BottomAxis.MajorTickColor = Color.FromHex("#D0D0D0");
        plot.BottomAxis.Label.Font.Bold = false;
        plot.BottomAxis.Label.Font.Size = 14;
        
        plot.YAxis.TickFont.Bold = true;
        plot.YAxis.TickFont.Size = 12;
        plot.YAxis.MajorTickLength = 0;
        plot.YAxis.MinorTickLength = 0;
        plot.YAxis.MajorTickWidth = 0;
        plot.YAxis.MinorTickWidth = 0;
    }
    
    public SufniPlotView()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.NewValue is null || Plot is null) return;
            var plot = Plot.Plot;
            
            switch (e.Property.Name)
            {
                case nameof(Telemetry):
                    plot.Clear();
                    plot.Clear();
                    OnTelemetryChanged((TelemetryData)e.NewValue);
                    break;
            }

            Plot.Refresh();
        };
    }

    protected virtual void OnTelemetryChanged(TelemetryData telemetryData) { }
}