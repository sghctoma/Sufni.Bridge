using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Labs.Controls;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.ViewModels;

namespace Sufni.Bridge.Views;

public partial class LinkagesView : UserControl
{
    public LinkagesView()
    {
        InitializeComponent();
    }

    public async void SwipePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == "SwipeState" && sender is Swipe swipe && e.NewValue is SwipeState.LeftVisible)
        {
            var vm = swipe.DataContext as LinkageViewModel;
            Debug.Assert(vm != null, nameof(vm) + " != null");

            var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
            Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

            swipe.SwipeState = SwipeState.Hidden;
            await mainPagesViewModel.DeleteLinkageCommand.ExecuteAsync(vm);
        }
    }
}