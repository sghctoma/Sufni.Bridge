using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Sufni.Bridge.Views
{
    public class HeaderColorConverter : IValueConverter
    {
        public static readonly HeaderColorConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isDirty)
            {
                return isDirty ? Colors.RosyBrown : Colors.Transparent;
            }
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            
            var rm = BikeSetupsTabItem.Bounds.Width / 2 - 10;
            if (double.IsNaN(rm)) return;
            MissingSetupNotification.Margin = new Thickness(0, 0, rm, 40);
        }
    }
}