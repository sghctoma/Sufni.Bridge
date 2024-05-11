using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Sufni.Bridge.Views;

public partial class MainPagesView : UserControl
{
    public MainPagesView()
    {
        InitializeComponent();
    }

    private void MenuTabItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control ctl)
        {
            FlyoutBase.ShowAttachedFlyout(ctl);
        }

        e.Handled = true;
    }

    private void MenuTabItem_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Handled = true;
    }
}