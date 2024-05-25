using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Sufni.Bridge.Views;

public class EditableTitleControl : TemplatedControl
{
    public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<EditableTitleControl, string>(
        "Title", defaultBindingMode:BindingMode.TwoWay);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    private TextBox? TitleTextBox { get; set; }
    private Button? EditButton { get; set; }
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        TitleTextBox = e.NameScope.Find<TextBox>("TitleTextBox");
        EditButton = e.NameScope.Find<Button>("EditButton");
        Debug.Assert(TitleTextBox != null, nameof(TitleTextBox) + " != null");
        Debug.Assert(EditButton != null, nameof(EditButton) + " != null");

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