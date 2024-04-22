using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Services;
using Sufni.Bridge.ViewModels;
using Sufni.Bridge.Views;
using System;
using System.Diagnostics;
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
        RegisteredServices.Collection.AddSingleton<IFilesService>(_ => new FilesService());
        Services = RegisteredServices.Collection.BuildServiceProvider();
        
        var fileService = Services.GetService<IFilesService>();
        Debug.Assert(fileService != null, nameof(fileService) + " != null");

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                fileService.SetTarget(TopLevel.GetTopLevel(desktop.MainWindow));
                desktop.MainWindow.DataContext = new MainViewModel();
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView();
                singleViewPlatform.MainView.Loaded += (_, _) =>
                {
                    var topLevel = TopLevel.GetTopLevel(singleViewPlatform.MainView);
                    fileService.SetTarget(topLevel);
                    singleViewPlatform.MainView!.DataContext = new MainViewModel();
                };
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }
}