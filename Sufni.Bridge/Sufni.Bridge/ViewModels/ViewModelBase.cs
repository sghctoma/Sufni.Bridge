using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Sufni.Bridge.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        public ObservableCollection<string> ErrorMessages { get; } = new();
        public ObservableCollection<string> Notifications { get; } = new();
        
        [RelayCommand]
        private void ClearErrors(object? o)
        {
            ErrorMessages.Clear();
        }
        
        [RelayCommand]
        private void ClearNotifications(object? o)
        {
            Notifications.Clear();
        }
        
        [RelayCommand]
        protected void OpenMainMenu()
        {
            var mainViewModel = App.Current?.Services?.GetService<MainViewModel>();
            var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
            Debug.Assert(mainViewModel != null, nameof(mainViewModel) + " != null");
            Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

            mainViewModel.CurrentView = mainPagesViewModel;
        }
    }
}