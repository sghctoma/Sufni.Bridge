using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.ViewModels;
using System;

namespace Sufni.Bridge;

public static class ViewModelBaseExtensions
{
    public static IServiceProvider GetServiceProvider(this ViewModelBase vm)
    {
        if (Application.Current == null)
        {
            throw new Exception("Expected service provider missing");
        }
        return (Application.Current as App)!.AppServiceProvider;
    }

    public static T CreateInstance<T>(this ViewModelBase vm)
    {
        return ActivatorUtilities.CreateInstance<T>(vm.GetServiceProvider());
    }

    public static T GetServiceOrCreateInstance<T>(this ViewModelBase vm)
    {
        return ActivatorUtilities.GetServiceOrCreateInstance<T>(vm.GetServiceProvider());
    }
}
