using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.ViewModels;

public partial class LinkageViewModel : ViewModelBase
{
    #region Observable properties

    [ObservableProperty] private Linkage linkage;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isDirty;
    
    #endregion

    #region Property change handlers
    #endregion

    #region Constructors

    public LinkageViewModel(Linkage linkage)
    {
        Linkage = linkage;
    }

    #endregion

    #region Commands

    private bool CanSave()
    {
        return IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        
    }

    #endregion
}