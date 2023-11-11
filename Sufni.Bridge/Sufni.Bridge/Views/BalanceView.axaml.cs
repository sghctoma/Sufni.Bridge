using Avalonia;
using Avalonia.Controls.Primitives;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views;

public class BalanceView : TemplatedControl
{
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
}