using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.ViewModels;

public partial class CalibrationViewModel : ViewModelBase
{
    #region Observable properties

    [ObservableProperty] private Calibration calibration;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isDirty;
    
    #endregion

    #region Property change handlers
    #endregion

    #region Constructors

    public CalibrationViewModel(Calibration calibration)
    {
        Calibration = calibration;
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