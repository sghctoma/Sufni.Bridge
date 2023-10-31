using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Services;
using Sufni.Bridge.ViewModels;
using Sufni.Bridge.Views;
using System;
using Avalonia.Controls;

namespace Sufni.Bridge;

public partial class App : Application
{
    private readonly IServiceCollection serviceCollection;
    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    // This is just here to suppress a warning about public constructor not being present
    public App()
    {
        serviceCollection = new ServiceCollection();
    }

    public App(IServiceCollection services)
    {
        services.AddSingleton<IHttpApiService, HttpApiServiceStub>();
        services.AddSingleton<ITelemetryDataStoreService, TelemetryDataStoreService>();
        serviceCollection = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    public override void OnFrameworkInitializationCompleted()
    {
        TopLevel? topLevel = null;
        
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                topLevel = TopLevel.GetTopLevel(desktop.MainWindow);
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView();
                topLevel = TopLevel.GetTopLevel(singleViewPlatform.MainView);
                break;
        }

        if (topLevel != null)
        {
            serviceCollection.AddSingleton<IFilesService>(x => new FilesService(topLevel));
        }
        
        Services = serviceCollection.BuildServiceProvider();
        
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow!.DataContext = new MainViewModel();
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView!.DataContext = new MainViewModel();
                break;
        }
        
        base.OnFrameworkInitializationCompleted();
    }
}