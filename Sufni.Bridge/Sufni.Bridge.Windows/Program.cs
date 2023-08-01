using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using SecureStorage;
using System;

namespace Sufni.Bridge.Windows
{
    internal class Program
    {
        private static readonly IServiceCollection Services;

        static Program()
        {
            Services = new ServiceCollection();
            Services.AddSingleton<ISecureStorage, SecureStorage.SecureStorage>();
        }

        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure( () => new App(Services))
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}