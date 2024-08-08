using Avalonia;
using Avalonia.iOS;
using Foundation;
using HapticFeedback;
using Microsoft.Extensions.DependencyInjection;
using SecureStorage;
using ServiceDiscovery;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : AvaloniaAppDelegate<App>
    {
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            RegisteredServices.Collection.AddSingleton<ISecureStorage, SecureStorage.SecureStorage>();
            RegisteredServices.Collection.AddSingleton<IServiceDiscovery, ServiceDiscovery.ServiceDiscovery>();
            RegisteredServices.Collection.AddSingleton<IHapticFeedback, HapticFeedback.HapticFeedback>();
            return base.CustomizeAppBuilder(builder)
                .WithInterFont()
                .With(new SkiaOptions { UseOpacitySaveLayer = true });
        }
    }
}