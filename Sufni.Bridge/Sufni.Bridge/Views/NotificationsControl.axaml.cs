using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.Input;

namespace Sufni.Bridge.Views;

public class NotificationsControl : TemplatedControl
{
    public static readonly StyledProperty<Collection<string>> NotificationsProperty = AvaloniaProperty.Register<NotificationsControl, Collection<string>>(
        "Notifications", new Collection<string>{"test notification 1", "test notification 2", "test notification 3"});

    public static readonly StyledProperty<IRelayCommand> ClearNotificationsCommandProperty = AvaloniaProperty.Register<NotificationsControl, IRelayCommand>(
        "ClearNotificationsCommand");

    public IRelayCommand ClearNotificationsCommand
    {
        get => GetValue(ClearNotificationsCommandProperty);
        set => SetValue(ClearNotificationsCommandProperty, value);
    }

    public Collection<string> Notifications
    {
        get => GetValue(NotificationsProperty);
        set => SetValue(NotificationsProperty, value);
    }
}