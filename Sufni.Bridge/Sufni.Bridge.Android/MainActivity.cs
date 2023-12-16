using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using SecureStorage;
using ServiceDiscovery;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.Android
{
    [Activity(
        Label = "Sufni Telemetry",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            RegisteredServices.Collection.AddSingleton<ISecureStorage, SecureStorage.SecureStorage>();
            RegisteredServices.Collection.AddSingleton<IServiceDiscovery, ServiceDiscovery.ServiceDiscovery>();
            
            return base.CustomizeAppBuilder(builder)
                .UseAndroid()
                .WithInterFont()
                .With(new SkiaOptions { UseOpacitySaveLayer = true });
        }
    }
}