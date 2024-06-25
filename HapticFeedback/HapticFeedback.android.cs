using Android.Views;

namespace HapticFeedback;

public class HapticFeedback(Window window) : IHapticFeedback
{
    public void Click()
    {
        var activity = window.Context as Activity;
#pragma warning disable CA1416 // Validate platform compatibility 
                               // Mmininum SDK is 23, so this is not an issue here.
        activity?.Window?.DecorView?.PerformHapticFeedback(FeedbackConstants.ContextClick);
#pragma warning restore CA1416 // Validate platform compatibility
    }

    public void LongPress()
    {
        var activity = window.Context as Activity;
        activity?.Window?.DecorView?.PerformHapticFeedback(FeedbackConstants.LongPress);
    }
}
