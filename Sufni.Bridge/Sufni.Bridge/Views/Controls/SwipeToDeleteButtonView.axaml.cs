using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using Sufni.Bridge.ViewModels.Items;

namespace Sufni.Bridge.Views.Controls;

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
