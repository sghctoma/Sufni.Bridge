using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Sufni.Bridge.Views.Controls;

public partial class CommonButtonLine : UserControl
{
    public CommonButtonLine()
    {
        InitializeComponent();
    }

    public void CancelButton_Click(object? sender, RoutedEventArgs args)
    {
        DeleteButton?.Flyout?.Hide();
    }
}
