using Avalonia.Controls;
using Avalonia.VisualTree;
using System.Linq;

namespace Sufni.Bridge.Views;

public partial class MainPagesView : UserControl
{
    public MainPagesView()
    {
        InitializeComponent();
        ContentOverlay.PointerPressed += (s, e) =>
        {
            MainSplitView.IsPaneOpen = false;
        };
        MenuPanel.Loaded += (s, e) =>
        {
            var menuItems = MenuPanel.GetVisualDescendants().OfType<MenuItem>();
            foreach (var menuItem in menuItems)
            {
                menuItem.PointerPressed += (s, e) =>
                {
                    MainSplitView.IsPaneOpen = false;
                };
            }
        };
    }
}