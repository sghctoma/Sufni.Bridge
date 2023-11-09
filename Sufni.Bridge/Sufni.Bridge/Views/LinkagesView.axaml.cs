using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace Sufni.Bridge.Views;

public class LinkageFileNameConverter : IValueConverter
{
    public static readonly LinkageFileNameConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            null => "...",
            string => value,
            _ => new BindingNotification(new InvalidCastException(), BindingErrorType.Error)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public partial class LinkagesView : UserControl
{
    public LinkagesView()
    {
        InitializeComponent();
    }
}