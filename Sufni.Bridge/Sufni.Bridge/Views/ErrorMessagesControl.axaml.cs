using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;

namespace Sufni.Bridge.Views;

public class ErrorMessagesControl : TemplatedControl
{
    public static readonly StyledProperty<Collection<string>> ErrorMessagesProperty = AvaloniaProperty.Register<ErrorMessagesControl, Collection<string>>(
        "ErrorMessages", new Collection<string>{"test error 1", "test error 2", "test error 3"});

    public static readonly StyledProperty<IRelayCommand> ClearErrorsCommandProperty = AvaloniaProperty.Register<ErrorMessagesControl, IRelayCommand>(
        "DeleteCommand");

    public IRelayCommand ClearErrorsCommand
    {
        get => GetValue(ClearErrorsCommandProperty);
        set => SetValue(ClearErrorsCommandProperty, value);
    }

    public Collection<string> ErrorMessages
    {
        get => GetValue(ErrorMessagesProperty);
        set => SetValue(ErrorMessagesProperty, value);
    }
}