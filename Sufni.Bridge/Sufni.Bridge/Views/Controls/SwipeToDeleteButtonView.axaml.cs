using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Labs.Controls;
using Avalonia.Media;
using Sufni.Bridge.ViewModels.Items;

namespace Sufni.Bridge.Views.Controls;

public class SwipeColorConverter : IValueConverter
{
    public static readonly SwipeColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ItemViewModelBase vm)
        {
            return vm.DeleteCommand.CanExecute(false) ? 
                Brush.Parse("#6f312d") : 
                Brush.Parse("#505050");
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public partial class SwipeToDeleteButtonView : UserControl
{
    public SwipeToDeleteButtonView()
    {
        InitializeComponent();
    }

    public async void SwipePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "SwipeState" && sender is Swipe swipe && e.NewValue is SwipeState.LeftVisible)
        {
            var vm = swipe.DataContext as ItemViewModelBase;
            if (vm is not null && vm.DeleteCommand.CanExecute(false))
            {
                await vm.DeleteCommand.ExecuteAsync(false);
            }

            swipe.SwipeState = SwipeState.Hidden;
        }
    }
}
