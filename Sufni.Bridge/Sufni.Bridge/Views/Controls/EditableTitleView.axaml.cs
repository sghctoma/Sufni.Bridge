using Avalonia.Controls;

namespace Sufni.Bridge.Views.Controls;

public partial class EditableTitleView : UserControl
{
    public EditableTitleView()
    {
        InitializeComponent();

        TitleTextBox.LostFocus += (_, _) =>
        {
            TitleTextBox.ClearSelection();
            TitleTextBox.IsEnabled = false;
        };

        EditButton.Click += (_, _) =>
        {
            if (TitleTextBox.IsEnabled)
            {
                TitleTextBox.ClearSelection();
                TitleTextBox.IsEnabled = false;
            }
            else
            {
                TitleTextBox.IsEnabled = true;
                TitleTextBox.Focus();
                TitleTextBox.SelectAll();
            }
        };
    }
}
