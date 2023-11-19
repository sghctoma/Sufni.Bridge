using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

public class BalanceView : TemplatedControl
{
    #region Styled properties

    public static readonly StyledProperty<string> MeanSignedDeviationProperty = AvaloniaProperty.Register<BalanceView, string>(
        "MeanSignedDeviation");

    public string MeanSignedDeviation
    {
        get => GetValue(MeanSignedDeviationProperty);
        set => SetValue(MeanSignedDeviationProperty, value);
    }
    
    public static readonly StyledProperty<BalanceType> BalanceTypeProperty = AvaloniaProperty.Register<BalanceView, BalanceType>(
        "BalanceType");
    
    public BalanceType BalanceType
    {
        get => GetValue(BalanceTypeProperty);
        set => SetValue(BalanceTypeProperty, value);
    }
    
    public static readonly StyledProperty<TelemetryData> TelemetryProperty = AvaloniaProperty.Register<BalanceView, TelemetryData>(
        "Telemetry");
    
    public TelemetryData Telemetry
    {
        get => GetValue(TelemetryProperty);
        set => SetValue(TelemetryProperty, value);
    }
    
    #endregion

    private void CalculateStatistics()
    {
        if (!Telemetry.Front.Present || !Telemetry.Rear.Present) return;
        var balance = Telemetry.CalculateBalance(BalanceType);

        var max = Math.Max(
            balance.FrontVelocity.Max(),
            balance.RearVelocity.Max());
            
        var msd = balance.MeanSignedDeviation / max * 100.0;
        MeanSignedDeviation = $"{msd:+0.00;-#.00} %";
    }

    public BalanceView()
    {  
        PropertyChanged += (_, e) =>
        {
            if (e.NewValue is null) return;
            
            switch (e.Property.Name)
            {
                case nameof(Telemetry):
                    CalculateStatistics();
                    break;
            }
        };
    }
}