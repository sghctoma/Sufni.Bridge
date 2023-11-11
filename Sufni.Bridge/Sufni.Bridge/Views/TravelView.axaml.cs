using Avalonia;
using Avalonia.Controls.Primitives;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views;

public class TravelView : TemplatedControl
{
    public static readonly StyledProperty<string> TravelAverageStringProperty = AvaloniaProperty.Register<TravelView, string>(
        "TravelAverageString");
    
    public string TravelAverageString
    {
        get => GetValue(TravelAverageStringProperty);
        set => SetValue(TravelAverageStringProperty, value);
    }

    public static readonly StyledProperty<string> TravelMaxStringProperty = AvaloniaProperty.Register<TravelView, string>(
        "TravelMaxString");
    
    public string TravelMaxString
    {
        get => GetValue(TravelMaxStringProperty);
        set => SetValue(TravelMaxStringProperty, value);
    }
    
    public static readonly StyledProperty<SuspensionType> SuspensionTypeProperty = AvaloniaProperty.Register<TravelView, SuspensionType>(
        "SuspensionType");
    
    public SuspensionType SuspensionType
    {
        get => GetValue(SuspensionTypeProperty);
        set => SetValue(SuspensionTypeProperty, value);
    }
    
    public static readonly StyledProperty<TelemetryData> TelemetryProperty = AvaloniaProperty.Register<TravelView, TelemetryData>(
        "Telemetry");

    public TelemetryData Telemetry
    {
        get => GetValue(TelemetryProperty);
        set => SetValue(TelemetryProperty, value);
    }
}