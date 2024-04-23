using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Sufni.Bridge.ViewModels;

public partial class MainViewModel : ViewModelBase
{
   [ObservableProperty] private ViewModelBase currentView = new MainPagesViewModel();

   public MainViewModel()
   {
      var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
      Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

      CurrentView = mainPagesViewModel;
   }
}