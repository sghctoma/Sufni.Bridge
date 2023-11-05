using Android.App;
using Android.Content.PM;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using SecureStorage;

namespace Sufni.Bridge.Android
{
    [Activity(
        Label = "Sufni.Bridge.Android",
        Theme = "@style/MyTheme.NoActionBar",
        Icon = "@drawable/icon",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
    public class MainActivity : AvaloniaMainActivity<App>
    {
        private static readonly IServiceCollection Services;

        static MainActivity()
        {
            Services = new ServiceCollection();
            Services.AddSingleton<ISecureStorage, SecureStorage.SecureStorage>();
        }
        
        protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
        {
            var realBuilder = AppBuilder.Configure( () => new App(Services) );
            
            return base.CustomizeAppBuilder(realBuilder)
                .WithInterFont()
                .With(new SkiaOptions { UseOpacitySaveLayer = true });
        }
    }
}