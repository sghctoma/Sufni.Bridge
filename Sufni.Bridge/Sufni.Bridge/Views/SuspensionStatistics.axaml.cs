using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Sufni.Bridge.Views;

public class CustomGridLengthConverter : IValueConverter
{
    public static readonly CustomGridLengthConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double widthPercentage)
        {
            return new GridLength(widthPercentage, GridUnitType.Star);
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class SuspensionStatistics : TemplatedControl
{
    public static readonly StyledProperty<string> TravelAverageStringProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "TravelAverageString");

    public static readonly StyledProperty<string> TravelMaxStringProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "TravelMaxString");
    
    public static readonly StyledProperty<string> SuspensionTypeProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "SuspensionType");

    public static readonly StyledProperty<string> AverageReboundVelocityProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "AverageReboundVelocity");
    
    public static readonly StyledProperty<string> MaximumReboundVelocityProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "MaximumReboundVelocity");
    
    public static readonly StyledProperty<string> AverageCompressionVelocityProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "AverageCompressionVelocity");
    
    public static readonly StyledProperty<string> MaximumCompressionVelocityProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "MaximumCompressionVelocity");

    public static readonly StyledProperty<string> HighSpeedReboundProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "HighSpeedRebound");
    
    public static readonly StyledProperty<string> LowSpeedReboundProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "LowSpeedRebound");
    
    public static readonly StyledProperty<string> HighSpeedCompressionProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "HighSpeedCompression");
    
    public static readonly StyledProperty<string> LowSpeedCompressionProperty = AvaloniaProperty.Register<SuspensionStatistics, string>(
        "LowSpeedCompression");
    
    /*
    public static readonly StyledProperty<GridLength> HighSpeedReboundColumnDefinitionProperty = AvaloniaProperty.Register<SuspensionStatistics, GridLength>(
        "HighSpeedReboundColumnDefinition");
    
    public static readonly StyledProperty<GridLength> LowSpeedReboundColumnDefinitionProperty = AvaloniaProperty.Register<SuspensionStatistics, GridLength>(
        "LowSpeedReboundColumnDefinition");
    
    public static readonly StyledProperty<GridLength> HighSpeedCompressionColumnDefinitionProperty = AvaloniaProperty.Register<SuspensionStatistics, GridLength>(
        "HighSpeedCompressionColumnDefinition");
    
    public static readonly StyledProperty<GridLength> LowSpeedCompressionColumnDefinitionProperty = AvaloniaProperty.Register<SuspensionStatistics, GridLength>(
        "LowSpeedCompressionColumnDefinition");
    */

    public string HighSpeedRebound
    {
        get => GetValue(HighSpeedReboundProperty);
        set => SetValue(HighSpeedReboundProperty, value);
    }
    
    public string LowSpeedRebound
    {
        get => GetValue(LowSpeedReboundProperty);
        set => SetValue(LowSpeedReboundProperty, value);
    }
    
    public string HighSpeedCompression
    {
        get => GetValue(HighSpeedCompressionProperty);
        set => SetValue(HighSpeedCompressionProperty, value);
    }
    
    public string LowSpeedCompression
    {
        get => GetValue(LowSpeedCompressionProperty);
        set => SetValue(LowSpeedCompressionProperty, value);
    }
    
    /*
    public GridLength HighSpeedReboundColumnDefinition
    {
        get => GetValue(HighSpeedReboundColumnDefinitionProperty);
        set => SetValue(HighSpeedReboundColumnDefinitionProperty, value);
    }
    
    public GridLength LowSpeedReboundColumnDefinition
    {
        get => GetValue(LowSpeedReboundColumnDefinitionProperty);
        set => SetValue(LowSpeedReboundColumnDefinitionProperty, value);
    }
    
    public GridLength HighSpeedCompressionColumnDefinition
    {
        get => GetValue(HighSpeedCompressionColumnDefinitionProperty);
        set => SetValue(HighSpeedCompressionColumnDefinitionProperty, value);
    }
    
    public GridLength LowSpeedCompressionColumnDefinition
    {
        get => GetValue(LowSpeedCompressionColumnDefinitionProperty);
        set => SetValue(LowSpeedCompressionColumnDefinitionProperty, value);
    }
    */
    
    public string TravelMaxString
    {
        get => GetValue(TravelMaxStringProperty);
        set => SetValue(TravelMaxStringProperty, value);
    }

    public string AverageReboundVelocity
    {
        get => GetValue(AverageReboundVelocityProperty);
        set => SetValue(AverageReboundVelocityProperty, value);
    }
    
    public string MaximumReboundVelocity
    {
        get => GetValue(MaximumReboundVelocityProperty);
        set => SetValue(MaximumReboundVelocityProperty, value);
    }
    
    public string AverageCompressionVelocity
    {
        get => GetValue(AverageCompressionVelocityProperty);
        set => SetValue(AverageCompressionVelocityProperty, value);
    }
    
    public string MaximumCompressionVelocity
    {
        get => GetValue(MaximumCompressionVelocityProperty);
        set => SetValue(MaximumCompressionVelocityProperty, value);
    }
    
    public string SuspensionType
    {
        get => GetValue(SuspensionTypeProperty);
        set => SetValue(SuspensionTypeProperty, value);
    }

    public string TravelAverageString
    {
        get => GetValue(TravelAverageStringProperty);
        set => SetValue(TravelAverageStringProperty, value);
    }
}