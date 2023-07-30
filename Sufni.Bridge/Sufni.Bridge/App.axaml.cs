using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Services;
using Sufni.Bridge.ViewModels;
using Sufni.Bridge.Views;
using System;

namespace Sufni.Bridge;

public partial class App : Application
{
    public readonly IServiceProvider AppServiceProvider;

    public App(IServiceCollection services)
    {
        services.AddSingleton<IHttpApiService, HttpApiService>();
        services.AddSingleton<ITelemetryDataStoreService, TelemetryDataStoreService>();
        AppServiceProvider = services.BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var viewModel = ActivatorUtilities.CreateInstance<MainViewModel>(AppServiceProvider);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = viewModel
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}