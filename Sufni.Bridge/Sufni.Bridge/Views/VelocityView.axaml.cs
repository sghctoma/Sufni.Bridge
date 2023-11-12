using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views;

public class GridLengthConverter : IValueConverter
{
    public static readonly GridLengthConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double length)
        {
            return new GridLength(length, GridUnitType.Star);
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

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

    public static readonly StyledProperty<VelocityBands> VelocityBandsProperty = AvaloniaProperty.Register<VelocityView, VelocityBands>(
        "VelocityBands");

    public VelocityBands VelocityBands
    {
        get => GetValue(VelocityBandsProperty);
        set => SetValue(VelocityBandsProperty, value);
    }
    
    #endregion

    private void CalculateStatistics()
    {
        var velocityStatistics = Telemetry.CalculateVelocityStatistics(SuspensionType);
        
        AverageCompressionVelocity = $"{velocityStatistics.AverageCompression:0.00} mm/s";
        MaximumCompressionVelocity = $"{velocityStatistics.MaxCompression:0.00} mm/s";
        AverageReboundVelocity = $"{velocityStatistics.AverageRebound:0.00} mm/s";
        MaximumReboundVelocity = $"{velocityStatistics.MaxRebound:0.00} mm/s";

        VelocityBands = Telemetry.CalculateVelocityBands(SuspensionType, HighSpeedThreshold);
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