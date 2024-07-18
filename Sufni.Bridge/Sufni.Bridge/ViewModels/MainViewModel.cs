﻿using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Sufni.Bridge.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty] private Thickness contentControlMargin;
    [ObservableProperty] private Thickness outerPanelMargin;
    [ObservableProperty] private Thickness safeAreaPadding;
    [ObservableProperty] private ViewModelBase currentView;
    private readonly Stack<ViewModelBase> viewHistory = new();
    private readonly MainPagesViewModel? mainPagesViewModel;

    public MainViewModel()
    {
        mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
        Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

        CurrentView = mainPagesViewModel;
    }

    private void SetMargins()
    {
        if (CurrentView == mainPagesViewModel)
        {
            OuterPanelMargin = new(0, 0, 0, SafeAreaPadding.Bottom);
            ContentControlMargin = new(0, SafeAreaPadding.Top, 0, 0);
        }
        else
        {
            OuterPanelMargin = new(0, 0, 0, 0);
            ContentControlMargin = SafeAreaPadding;
        }
    }

    partial void OnSafeAreaPaddingChanged(Thickness value)
    {
        SetMargins();
    }

    partial void OnCurrentViewChanged(ViewModelBase value)
    {
        SetMargins();
    }

    public void OpenView(ViewModelBase view)
    {
        viewHistory.Push(CurrentView);
        CurrentView = view;
    }

    public void OpenPreviousView()
    {
        CurrentView = viewHistory.Pop();
        Debug.Assert(CurrentView != null, nameof(CurrentView) + " != null");
    }
}