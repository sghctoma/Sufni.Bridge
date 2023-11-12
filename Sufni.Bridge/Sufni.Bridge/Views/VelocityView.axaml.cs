using Avalonia;
using Avalonia.Controls.Primitives;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views;

public class VelocityView : TemplatedControl
{
    private const double HighSpeedThreshold = 200;
    
    #region Styled properties
    
    public static readonly StyledProperty<string> AverageReboundVelocityProperty = AvaloniaProperty.Register<VelocityView, string>(
        "AverageReboundVelocity");
    
    public string AverageReboundVelocity
    {
        get => GetValue(AverageReboundVelocityProperty);
        set => SetValue(AverageReboundVelocityProperty, value);
    }
    
    public static readonly StyledProperty<string> MaximumReboundVelocityProperty = AvaloniaProperty.Register<VelocityView, string>(
        "MaximumReboundVelocity");
    public string MaximumReboundVelocity
    {
        get => GetValue(MaximumReboundVelocityProperty);
        set => SetValue(MaximumReboundVelocityProperty, value);
    }

    public static readonly StyledProperty<string> AverageCompressionVelocityProperty = AvaloniaProperty.Register<VelocityView, string>(
        "AverageCompressionVelocity");
    
    public string AverageCompressionVelocity
    {
        get => GetValue(AverageCompressionVelocityProperty);
        set => SetValue(AverageCompressionVelocityProperty, value);
    }
    
    public static readonly StyledProperty<string> MaximumCompressionVelocityProperty = AvaloniaProperty.Register<VelocityView, string>(
        "MaximumCompressionVelocity");
    
    public string MaximumCompressionVelocity
    {
        get => GetValue(MaximumCompressionVelocityProperty);
        set => SetValue(MaximumCompressionVelocityProperty, value);
    }
    
    public static readonly StyledProperty<SuspensionType> SuspensionTypeProperty = AvaloniaProperty.Register<VelocityView, SuspensionType>(
        "SuspensionType");
    
    public SuspensionType SuspensionType
    {
        get => GetValue(SuspensionTypeProperty);
        set => SetValue(SuspensionTypeProperty, value);
    }
    
    public static readonly StyledProperty<TelemetryData> TelemetryProperty = AvaloniaProperty.Register<VelocityView, TelemetryData>(
        "Telemetry");

    public TelemetryData Telemetry
    {
        get => GetValue(TelemetryProperty);
        set => SetValue(TelemetryProperty, value);
    }
    
    #endregion

    private void CalculateStatistics()
    {
        var velocityStatistics = Telemetry.CalculateVelocityStatistics(SuspensionType);
        //TODO: var velocityBands = Telemetry.CalculateVelocityBands(SuspensionType, HighSpeedThreshold);
        
        AverageCompressionVelocity = $"{velocityStatistics.AverageCompression:0.00} mm/s";
        MaximumCompressionVelocity = $"{velocityStatistics.MaxCompression:0.00} mm/s";
        AverageReboundVelocity = $"{velocityStatistics.AverageRebound:0.00} mm/s";
        MaximumReboundVelocity = $"{velocityStatistics.MaxRebound:0.00} mm/s";
    }

    public VelocityView()
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