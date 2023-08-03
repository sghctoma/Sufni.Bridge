using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Sufni.Bridge.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            
            var rm = BikeSetupsTabItem.Bounds.Width / 2 - 20;
            if (double.IsNaN(rm)) return;
            MissingSetupNotification.Margin = new Thickness(0, 0, rm, 35);
        }
    }
}