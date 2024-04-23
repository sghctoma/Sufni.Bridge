using Avalonia;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;

namespace Sufni.Bridge.Views;

public class CommonButtonLine : TemplatedControl
{
    public static readonly StyledProperty<IRelayCommand> OpenMainMenuCommandProperty = AvaloniaProperty.Register<CommonButtonLine, IRelayCommand>(
        "OpenMainMenuCommand");

    public IRelayCommand OpenMainMenuCommand
    {
        get => GetValue(OpenMainMenuCommandProperty);
        set => SetValue(OpenMainMenuCommandProperty, value);
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
}