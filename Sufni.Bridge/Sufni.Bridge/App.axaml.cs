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
    public new static App? Current => Application.Current as App;
    public IServiceProvider? Services { get; private set; }

    public App()
    {
#if DEBUG
        RegisteredServices.Collection.AddSingleton<IHttpApiService, HttpApiServiceStub>();
#else
        RegisteredServices.Collection.AddSingleton<IHttpApiService, HttpApiService>();
#endif
        RegisteredServices.Collection.AddSingleton<ITelemetryDataStoreService, TelemetryDataStoreService>();
        RegisteredServices.Collection.AddSingleton<IDatabaseService, SqLiteDatabaseService>();
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
            RegisteredServices.Collection.AddSingleton<IFilesService>(_ => new FilesService(topLevel));
        }

        Services = RegisteredServices.Collection.BuildServiceProvider();

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