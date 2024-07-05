using System;
using System.Collections;
using System.Globalization;
using System.Threading;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Labs.Controls.Base.Pan;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using HapticFeedback;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.ViewModels;

namespace Sufni.Bridge.Views.Controls;

public class MenuItemSelectedConverter : IValueConverter
{
    public static readonly MenuItemSelectedConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool selected)
        {
            return selected ? FontWeight.DemiBold : FontWeight.Regular;
        }

        return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public partial class PullableMenuScrollViewer : UserControl
{
    private static readonly double pullRatio = 3;
    private static readonly double menuItemHeight = 30;
    private readonly DoubleTransition heightTransition = new()
    {
        Duration = TimeSpan.FromSeconds(0.2),
        Property = UserControl.HeightProperty,
        Easing = new CubicEaseOut(),
    };
    private readonly ThicknessTransition marginTransition = new()
    {
        Duration = TimeSpan.FromSeconds(0.2),
        Property = UserControl.MarginProperty,
        Easing = new CubicEaseOut(),
    };
    private double totalPulled;
    private int? selectedIndex;
    private readonly IHapticFeedback? hapticFeedback = App.Current?.Services?.GetService<IHapticFeedback>();
    public PullableMenuScrollViewer()
    {
        InitializeComponent();
        Scroll.Transitions = [];
        Container.Transitions = [];

        // PanGestures are not recognized on a ScrollViewer if it contains labs:Swipe
        // items, so we handle the pull with ScrollGestures, ...
        AddHandlersForTouchPull();

        // ... but pulling down with the mouse is not recognized as a ScrollGesture, so
        // we also handle PanGestures.
        AddHandlersForMousePull();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Container.Margin = new Thickness(0, -PullMenu.Bounds.Height - 20, 0, 0);
    }

    #region Styled properties
    public static readonly StyledProperty<IEnumerable> ItemsProperty =
        AvaloniaProperty.Register<PullableMenuScrollViewer, IEnumerable>(nameof(Items));

    public IEnumerable Items
    {
        get => this.GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }
    public static readonly StyledProperty<IEnumerable> MenuItemsProperty =
            AvaloniaProperty.Register<PullableMenuScrollViewer, IEnumerable>(nameof(MenuItems));

    public IEnumerable MenuItems
    {
        get => this.GetValue(MenuItemsProperty);
        set => SetValue(MenuItemsProperty, value);
    }

    #endregion

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        Scroll.Height = Bounds.Height;
    }
    private void AddHandlersForTouchPull()
    {
        // When we scroll down while being at the top of the scroll viewer, we
        // move the whole control down.
        Scroll.AddHandler(Gestures.ScrollGestureEvent, (s, e) =>
        {
            if (Scroll.Offset.Y == 0)
            {
                Scroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
                totalPulled += -e.Delta.Y / pullRatio;
                PullUpdate();
            }
        });

        // Move the control back to its original place when we stopped pulling.
        Scroll.AddHandler(Gestures.ScrollGestureEndedEvent, (s, e) =>
        {
            if (totalPulled != 0)
            {
                Scroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
                PullFinished();
            }
        });
    }

    private void AddHandlersForMousePull()
    {
        var panGestureRecognizer = new PanGestureRecognizer
        {
            Direction = PanDirection.Down | PanDirection.Up,
            Threshold = 10,
        };

        panGestureRecognizer.OnPan += PanGestureRecognizer_OnPan;
        Scroll.GestureRecognizers.Add(panGestureRecognizer);
    }

    private void PanGestureRecognizer_OnPan(object? sender, PanUpdatedEventArgs e)
    {
        if (Scroll.Offset.Y == 0)
        {
            if (e.StatusType == PanGestureStatus.Completed)
            {
                PullFinished();
            }
            else
            {
                totalPulled = e.TotalY / pullRatio;
                PullUpdate();
            }
        }
    }

    private void PullUpdate()
    {
        Container.Margin = new Thickness(0, -PullMenu.Bounds.Height - 20 + totalPulled, 0, 0);
        Scroll.Height = Bounds.Height - totalPulled;

        int? index = (int)(totalPulled - 15) / (int)menuItemHeight - 1;
        index = index < 0 ? null : index;
        if (index != selectedIndex)
        {
            if (selectedIndex is not null &&
                selectedIndex < PullMenu.ItemsSourceView!.Count &&
                PullMenu.ItemsSourceView![PullMenu.ItemsSourceView.Count - selectedIndex.Value - 1] is PullMenuItemViewModel prev)
            {
                prev.Selected = false;
            }

            if (index is not null &&
                index < PullMenu.ItemsSourceView!.Count &&
                PullMenu.ItemsSourceView![PullMenu.ItemsSourceView.Count - index.Value - 1] is PullMenuItemViewModel curr &&
                curr.Command is not null &&
                curr.Command.CanExecute(curr.CommandParameter))
            {
                hapticFeedback?.LongPress();
                curr.Selected = true;
            }

            selectedIndex = index;
        }
    }

    private void PullFinished()
    {
        if (selectedIndex is not null &&
            selectedIndex < PullMenu.ItemsSourceView!.Count &&
            PullMenu.ItemsSourceView![PullMenu.ItemsSourceView.Count - selectedIndex.Value - 1] is PullMenuItemViewModel selected &&
            selected.Command is not null &&
            selected.Command.CanExecute(selected.CommandParameter))
        {
            selected.Command.Execute(selected.CommandParameter);
            selected.Selected = false;
        }

        totalPulled = 0;
        selectedIndex = null;

        Scroll.Transitions!.Clear();
        Container.Transitions!.Clear();
        Container.Transitions!.Add(marginTransition);
        Scroll.Transitions!.Add(heightTransition);

        Container.Margin = new Thickness(0, -PullMenu.Bounds.Height - 20, 0, 0);
        Scroll.Height = Bounds.Height;

        new Thread(() => 
        {
            Thread.Sleep(200);
            Dispatcher.UIThread.Post(new Action(() => {
                Scroll.Transitions!.Clear();
                Container.Transitions!.Clear();
            }));
        }).Start();
    }
}