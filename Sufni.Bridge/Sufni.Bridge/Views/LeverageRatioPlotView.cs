using System.Linq;
using Avalonia;
using ScottPlot;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.Views;

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
        Plot!.Plot.BottomAxis.Label.Text = "Rear Wheel Travel (mm)";
        Plot!.Plot.LeftAxis.Label.Text = "Leverage Ratio";

        if (data.WheelTravel.Count == 0)
        {
            return;
        }
        
        var lr = Plot!.Plot.Add.Scatter(data.WheelTravel, data.LeverageRatio);
        lr.MarkerStyle.IsVisible = false;
        lr.LineStyle.Color = Color.FromHex("#ffffbf");
        lr.LineStyle.Width = 2;
        
        Plot!.Plot.SetAxisLimits(data.WheelTravel.Min(), data.WheelTravel.Max(),
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