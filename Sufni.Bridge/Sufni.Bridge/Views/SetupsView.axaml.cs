using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.ViewModels;

namespace Sufni.Bridge.Views;

public partial class SetupsView : UserControl
{
    public SetupsView()
    {
        InitializeComponent();
    }

    public async void SwipePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "SwipeState" && sender is Swipe swipe && e.NewValue is SwipeState.LeftVisible)
        {
            var vm = swipe.DataContext as SetupViewModel;
            Debug.Assert(vm != null, nameof(vm) + " != null");

            var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
            Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

            swipe.SwipeState = SwipeState.Hidden;
            await mainPagesViewModel.DeleteSessionCommand.ExecuteAsync(vm.Id);
        }
    }
}