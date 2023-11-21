using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Sufni.Bridge.Models.Telemetry;

namespace Sufni.Bridge.Views.Plots;

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

    public VelocityView()
    {  
        PropertyChanged += (_, e) =>
        {
            if (e.NewValue is null) return;
            
            switch (e.Property.Name)
            {
                case nameof(Telemetry):
                    VelocityBands = Telemetry.CalculateVelocityBands(SuspensionType, HighSpeedThreshold);
                    break;
            }
        };
    }
}