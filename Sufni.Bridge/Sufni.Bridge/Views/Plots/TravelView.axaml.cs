using Avalonia;
using Avalonia.Controls.Primitives;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

public class TravelView : TemplatedControl
{
    #region Styled properties
    
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
    
    #endregion

    private void CalculateStatistics()
    {
        var statistics = Telemetry.CalculateTravelStatistics(SuspensionType);
        
        var avgPercentage = statistics.Average / Telemetry.Linkage.MaxFrontTravel * 100.0;
        var maxPercentage = statistics.Max / Telemetry.Linkage.MaxFrontTravel * 100.0;
        
        TravelAverageString = $"{statistics.Average:F2} mm ({avgPercentage:F2}%)";
        TravelMaxString = $"{statistics.Max:F2} mm ({maxPercentage:F2}%) / {statistics.Bottomouts} bottom outs";
    }

    public TravelView()
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