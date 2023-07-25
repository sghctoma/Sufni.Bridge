using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace Sufni.Bridge.Views
{
    public class SessionWidget : TemplatedControl
    {
        public static readonly StyledProperty<string> PathProperty = AvaloniaProperty.Register<SessionWidget, string>(
        "Path");

        public string Path
        {
            get => GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        public static readonly StyledProperty<string> TimestampProperty = AvaloniaProperty.Register<SessionWidget, string>(
            "Timestamp");

        public string Timestamp
        {
            get => GetValue(TimestampProperty);
            set => SetValue(TimestampProperty, value);
        }

        public static readonly StyledProperty<string> DurationProperty = AvaloniaProperty.Register<SessionWidget, string>(
            "Duration");

        public string Duration
        {
            get => GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly StyledProperty<string> SessionNameProperty = AvaloniaProperty.Register<SessionWidget, string>(
            "Name");

        public string SessionName
        {
            get => GetValue(SessionNameProperty);
            set => SetValue(SessionNameProperty, value);
        }

        public static readonly StyledProperty<string> DescriptionProperty = AvaloniaProperty.Register<SessionWidget, string>(
            "Description");

        public string Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly StyledProperty<bool> ShouldImportProperty = AvaloniaProperty.Register<SessionWidget, bool>(
            "ShouldImport");

        public bool ShouldImport
        {
            get => GetValue(ShouldImportProperty);
            set => SetValue(ShouldImportProperty, value);
        }
    }
}
