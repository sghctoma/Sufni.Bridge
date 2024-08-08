namespace HapticFeedback;

public class HapticFeedback : IHapticFeedback
{
    public void Click()
    {
        using var generator = new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Light);
        generator.Prepare();
        generator.ImpactOccurred();
    }

    public void LongPress()
    {
        using var generator = new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Medium);
        generator.Prepare();
        generator.ImpactOccurred();
    }
}
