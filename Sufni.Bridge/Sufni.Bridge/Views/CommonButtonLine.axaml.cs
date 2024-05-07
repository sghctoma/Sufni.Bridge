using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;

namespace Sufni.Bridge.Views;

public class CommonButtonLine : TemplatedControl
{
    public static readonly StyledProperty<IRelayCommand> OpenPreviousPageCommandProperty = AvaloniaProperty.Register<CommonButtonLine, IRelayCommand>(
        "OpenPreviousPageCommand");

    public IRelayCommand OpenPreviousPageCommand
    {
        get => GetValue(OpenPreviousPageCommandProperty);
        set => SetValue(OpenPreviousPageCommandProperty, value);
    }
    
    public static readonly StyledProperty<IAsyncRelayCommand> SaveCommandProperty = AvaloniaProperty.Register<CommonButtonLine, IAsyncRelayCommand>(
        "SaveCommand");

    public IAsyncRelayCommand SaveCommand
    {
        get => GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public static readonly StyledProperty<IRelayCommand> ResetCommandProperty = AvaloniaProperty.Register<CommonButtonLine, IRelayCommand>(
        "ResetCommand");

    public IRelayCommand ResetCommand
    {
        get => GetValue(ResetCommandProperty);
        set => SetValue(ResetCommandProperty, value);
    }

    public static readonly StyledProperty<IAsyncRelayCommand> DeleteCommandProperty = AvaloniaProperty.Register<CommonButtonLine, IAsyncRelayCommand>(
        "DeleteCommand");

    public IAsyncRelayCommand DeleteCommand
    {
        get => GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }
    
    private Button? DeleteButton { get; set; }
    private Button? CancelButton { get; set; }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        DeleteButton = e.NameScope.Find<Button>("DeleteButton");
        CancelButton = e.NameScope.Find<Button>("CancelButton");
        Debug.Assert(DeleteButton != null, nameof(DeleteButton) + " != null");
        Debug.Assert(CancelButton != null, nameof(CancelButton) + " != null");

        DeleteButton.IsEnabled = DeleteCommand.CanExecute(null);
        CancelButton.Click += (_, _) => DeleteButton?.Flyout?.Hide();
    }
}