using System.Linq;
using Avalonia;
using ScottPlot;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

public class LeverageRatioPlotView : SufniPlotView
{
    #region Styled properties

    public static readonly StyledProperty<LeverageRatioData> LevarageRatioDataProperty =
        AvaloniaProperty.Register<LeverageRatioPlotView, LeverageRatioData>("LevarageRatioData");

    public LeverageRatioData LevarageRatioData
    {
        get => GetValue(LevarageRatioDataProperty);
        set => SetValue(LevarageRatioDataProperty, value);
    }

    #endregion

    private void OnLevarageRatioDataChanged(LeverageRatioData data)
    {
        Plot!.Plot.Layout.Fixed(new PixelPadding(60, 3, 40, 10));
        Plot!.Plot.Axes.Bottom.Label.Text = "Rear Wheel Travel (mm)";
        Plot!.Plot.Axes.Left.Label.Text = "Leverage Ratio";
        Plot!.Plot.Axes.Left.Label.OffsetX = -10;

        if (data.WheelTravel.Count == 0)
        {
            return;
        }
        
        var lr = Plot!.Plot.Add.Scatter(data.WheelTravel, data.LeverageRatio);
        lr.MarkerStyle.IsVisible = false;
        lr.LineStyle.Color = Color.FromHex("#ffffbf");
        lr.LineStyle.Width = 2;
        
        Plot!.Plot.Axes.SetLimits(data.WheelTravel.Min(), data.WheelTravel.Max(),
            data.LeverageRatio.Min(), data.LeverageRatio.Max());
    }
    
    public LeverageRatioPlotView()
    {
        PropertyChanged += (_, e) =>
        {
            if (e.NewValue is null || Plot is null) return;
            var plot = Plot.Plot;
            
            switch (e.Property.Name)
            {
                case nameof(LevarageRatioData):
                    plot.Clear();
                    OnLevarageRatioDataChanged((LeverageRatioData)e.NewValue);
                    break;
            }

            Plot.Refresh();
        };
    }
}