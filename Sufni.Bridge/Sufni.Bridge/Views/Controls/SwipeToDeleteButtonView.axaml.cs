using System;
using System.Globalization;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Labs.Controls;
using Avalonia.Media.Transformation;
using HapticFeedback;
using Microsoft.Extensions.DependencyInjection;
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
                ".default { fill: #9f312d; }" :
                ".default { fill: #a0a0a0; }";
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
    private bool animationPlayed = false;
    private readonly IHapticFeedback? hapticFeedback = App.Current?.Services?.GetService<IHapticFeedback>();

    public SwipeToDeleteButtonView()
    {
        InitializeComponent();
        SwipeButton.Children[2].PropertyChanged += async (s, e) =>
        {
            var deleteButton = ((ContentPresenter)SwipeButton.Children[1]).Content as Button;
            if (e.Property.Name == "RenderTransform" &&
                e.NewValue is TransformOperations ops &&
                deleteButton!.Command is not null &&
                deleteButton!.Command!.CanExecute(false))
            {
                var offset = ops.Operations[0].Matrix.M31;
                var trashcan = deleteButton!.Content as Image;
                trashcan!.Opacity = Math.Min(1.0, offset / 64.0);

                if (offset > deleteButton.Width && !animationPlayed)
                {
                    hapticFeedback?.LongPress();

                    animationPlayed = true;
                    var animation = Resources["ImageSizeAnimation"] as Animation;
                    await animation!.RunAsync(trashcan);
                }
                if (offset < deleteButton.Width) 
                {
                    animationPlayed = false;
                }
            }
        };
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
