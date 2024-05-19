using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sufni.Bridge.Views;

// ReSharper disable UnusedParameter.Local

public partial class SessionView : UserControl
{
    private bool sizeChanging;
    public SessionView()
    {
        InitializeComponent();
    }

    private void OnSpringSelected(object? sender, RoutedEventArgs e)
    {
        TabScrollViewer.Offset = new Vector(0, 0);
        
        Spring.IsEnabled = false;
        Damper.IsEnabled = true;
        Balance.IsEnabled = true;
        Notes.IsEnabled = true;
    }

    private void OnDamperSelected(object? sender, RoutedEventArgs e)
    {
        var x = TabScrollViewer.Viewport.Width;
        TabScrollViewer.Offset = new Vector(x, 0);
        
        Spring.IsEnabled = true;
        Damper.IsEnabled = false;
        Balance.IsEnabled = true;
        Notes.IsEnabled = true;
    }

    private void OnBalanceSelected(object? sender, RoutedEventArgs e)
    {
        var x = 2 * TabScrollViewer.Viewport.Width;
        TabScrollViewer.Offset = new Vector(x, 0);
        
        Spring.IsEnabled = true;
        Damper.IsEnabled = true;
        Balance.IsEnabled = false;
        Notes.IsEnabled = true;
    }

    private void OnNotesSelected(object? sender, RoutedEventArgs e)
    {
        var x = 3 * TabScrollViewer.Viewport.Width;
        TabScrollViewer.Offset = new Vector(x, 0);
        
        Spring.IsEnabled = true;
        Damper.IsEnabled = true;
        Balance.IsEnabled = true;
        Notes.IsEnabled = false;
    }

    private void TabScrollViewer_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(TabScrollViewer.Offset) || sizeChanging) return;

        Spring.IsEnabled = true;
        Damper.IsEnabled = true;
        Balance.IsEnabled = true;
        Notes.IsEnabled = true;
        
        var width = TabScrollViewer.Viewport.Width;
        var offset = TabScrollViewer.Offset.X;
        if (offset < 0.5 * width) Spring.IsEnabled = false;
        else if (offset < 1.5 * width) Damper.IsEnabled = false;
        else if (offset < 2.5 * width) Balance.IsEnabled = false;
        else Notes.IsEnabled = false;
    }

    private void TabScrollViewer_OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        sizeChanging = true;
        if (!Spring.IsEnabled) TabScrollViewer.Offset = new Vector(0,0);
        else if (!Damper.IsEnabled) TabScrollViewer.Offset = new Vector(e.NewSize.Width, 0);
        else if (!Balance.IsEnabled) TabScrollViewer.Offset = new Vector(2 * e.NewSize.Width, 0);
        else if (!Notes.IsEnabled) TabScrollViewer.Offset = new Vector(3 * e.NewSize.Width, 0);
        sizeChanging = false;
    }
}