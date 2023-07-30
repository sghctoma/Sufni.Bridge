using Avalonia;
using Avalonia.Browser;
using Sufni.Bridge;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    private static async Task Main(string[] _) => await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>();
}