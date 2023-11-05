using Avalonia;
using Avalonia.Browser;
using Sufni.Bridge;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SecureStorage;
using Sufni.Bridge.Services;

[assembly: SupportedOSPlatform("browser")]

internal partial class Program
{
    private static async Task Main(string[] _) => await BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
    {
        RegisteredServices.Collection.AddSingleton<ISecureStorage, SecureStorage.SecureStorage>();
        return AppBuilder.Configure<App>();
    }
}