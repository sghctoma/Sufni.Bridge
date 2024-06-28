using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Labs.Controls.Base.Pan;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using HapticFeedback;
using Microsoft.Extensions.DependencyInjection;

namespace Sufni.Bridge.Views.Controls;

public partial class PullableScrollViewer : UserControl
{
    private static readonly double pullRatio = 3;

    private double totalPulled;
    private bool thresholdAlreadyReached;
    private readonly IHapticFeedback? hapticFeedback = App.Current?.Services?.GetService<IHapticFeedback>();

    #region Styled properties

    public PullableScrollViewer()
    {
        InitializeComponent();

        // PanGestures are not recognized on a ScrollViewer if it contains labs:Swipe
        // items, so we handle the pull with ScrollGestures, ...
        AddHandlersForTouchPull();

        // ... but pulling down with the mouse is not recognized as a ScrollGesture, so
        // we also handle PanGestures.
        AddHandlersForMousePull();
    }

    public static readonly StyledProperty<RelayCommand?> PullCommandProperty =
        AvaloniaProperty.Register<PullableScrollViewer, RelayCommand?>(nameof(PullCommandProperty));

    public RelayCommand? PullCommand
    {
        get => this.GetValue(PullCommandProperty);
        set => SetValue(PullCommandProperty, value);
    }

    public static readonly StyledProperty<double> PullThresholdProperty =
        AvaloniaProperty.Register<PullableScrollViewer, double>(nameof(PullThreshold), defaultValue: 80);

    public double PullThreshold
    {
        get => this.GetValue(PullThresholdProperty);
        set => SetValue(PullThresholdProperty, value);
    }

    public static readonly StyledProperty<StyledElement> ItemsContainerProperty =
        AvaloniaProperty.Register<PullableScrollViewer, StyledElement>(nameof(ItemsContainer));
    
    public StyledElement ItemsContainer
    {
       get => this.GetValue(ItemsContainerProperty);
       set => SetValue(ItemsContainerProperty, value);
    }

    #endregion

    private void AddHandlersForTouchPull()
    {
        // When we scroll down while being at the top of the scroll viewer, we
        // move the whole control down.
        Scroll.AddHandler(Gestures.ScrollGestureEvent, (s, e) =>
        {
            if (Scroll.Offset.Y == 0)
            {
                Scroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
                totalPulled += -e.Delta.Y;
                PullUpdate();
            }
        });

        // Move the control back to its original place when we stopped pulling.
        AddHandler(Gestures.ScrollGestureEndedEvent, (s, e) =>
        {
            Scroll.VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
            PullFinished();
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
                totalPulled = e.TotalY;
                PullUpdate();
            }
        }
    }

    private void PullUpdate()
    {
        var translate = new TranslateTransform(0, totalPulled / pullRatio);
        Scroll.SetValue(RenderTransformProperty, translate);

        if (!thresholdAlreadyReached)
        {
            var ratio = Math.Min(1.0, totalPulled / (PullThreshold * pullRatio));
            Progress.Opacity = ratio * 0.6;
            var rotate = new RotateTransform(360.0 * ratio);
            Progress.SetValue(RenderTransformProperty, rotate);
        }

        if (totalPulled > PullThreshold * pullRatio)
        {
            if (!thresholdAlreadyReached)
            {
                thresholdAlreadyReached = true;
                hapticFeedback?.LongPress();
                Progress.Opacity = 1.0;
            }
        }
        else
        {
            thresholdAlreadyReached = false;
        }
    }

    private void PullFinished()
    {
        if (totalPulled > PullThreshold * pullRatio)
        {
            PullCommand?.Execute(null);
        }

        totalPulled = 0;
        Progress.Opacity = 0;
        Scroll.SetValue(RenderTransformProperty, null);
    }
}