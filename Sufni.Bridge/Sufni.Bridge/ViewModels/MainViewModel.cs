using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Sufni.Bridge.ViewModels;

public partial class MainViewModel : ViewModelBase
{
   [ObservableProperty] private ViewModelBase currentView;
   private readonly Stack<ViewModelBase> viewHistory = new();

   public MainViewModel()
   {
      var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
      Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

      CurrentView = mainPagesViewModel;
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