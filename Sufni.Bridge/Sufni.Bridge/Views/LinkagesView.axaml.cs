using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sufni.Bridge.Views;

public partial class LinkagesView : UserControl
{
    public LinkagesView()
    {
        InitializeComponent();
    }

    // ReSharper disable UnusedParameter.Local
    private void Expander_OnExpanded(object? sender, RoutedEventArgs e)
    {
        // Without this, Avalonia.Xaml.Behavior can't find the Expanded event on the Expander on iOS.
    }
}