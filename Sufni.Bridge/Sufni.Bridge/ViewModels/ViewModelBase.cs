using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sufni.Bridge.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        public ObservableCollection<string> ErrorMessages { get; } = new();
        
        [RelayCommand]
        private void ClearErrors(object? o)
        {
            ErrorMessages.Clear();
        }
    }
}