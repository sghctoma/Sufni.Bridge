using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ScottPlot;
using ScottPlot.Avalonia;
using Color = ScottPlot.Color;

namespace Sufni.Bridge.Views.Plots;

public class SufniPlotView : TemplatedControl
{
    protected AvaPlot? Plot;
    protected Color FrontColor = Color.FromHex("#3288bd");
    protected Color RearColor = Color.FromHex("#66c2a5");
    
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
        
        plot.FigureBackground.Color = Color.FromHex("#15191C");
        plot.DataBackground.Color = Color.FromHex("#20262B");
        plot.Grid.MajorLineColor = Color.FromHex("#505558");
        plot.Grid.MinorLineColor = Color.FromHex("#505558");
        plot.Axes.Color(Color.FromHex("#505558"));
        
        plot.Axes.Title.Label.FontSize = 14;
        plot.Axes.Title.Label.OffsetY = 5;
        plot.Axes.Title.Label.ForeColor = Color.FromHex("#D0D0D0");
        
        plot.Axes.Left.Label.ForeColor = Color.FromHex("#D0D0D0");
        plot.Axes.Left.Label.Bold = false;
        plot.Axes.Left.Label.FontSize = 14;
       
        plot.Axes.Left.TickLabelStyle.ForeColor = Color.FromHex("#D0D0D0");
        plot.Axes.Left.TickLabelStyle.Bold = true;
        plot.Axes.Left.TickLabelStyle.FontSize = 12;
        plot.Axes.Left.MajorTickStyle.Length = 0;
        plot.Axes.Left.MinorTickStyle.Length = 0;
        plot.Axes.Left.MajorTickStyle.Width = 0;
        plot.Axes.Left.MinorTickStyle.Width = 0;
        
        plot.Axes.Bottom.Label.ForeColor = Color.FromHex("#D0D0D0");
        plot.Axes.Bottom.Label.Bold = false;
        plot.Axes.Bottom.Label.FontSize = 14;
        
        plot.Axes.Bottom.TickLabelStyle.ForeColor = Color.FromHex("#D0D0D0");
        plot.Axes.Bottom.TickLabelStyle.Bold = true;
        plot.Axes.Bottom.TickLabelStyle.FontSize = 12;
        plot.Axes.Bottom.MajorTickStyle.Length = 0;
        plot.Axes.Bottom.MinorTickStyle.Length = 0;
        plot.Axes.Bottom.MajorTickStyle.Width = 0;
        plot.Axes.Bottom.MinorTickStyle.Width = 0;
    }
}