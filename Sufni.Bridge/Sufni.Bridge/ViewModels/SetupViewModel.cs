using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    #region Observable properties

    [ObservableProperty] private Setup setup;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isDirty;

    public ObservableCollection<LinkageViewModel> Linkages { get; }
    public ObservableCollection<CalibrationViewModel> Calibrations { get; }
    
    [ObservableProperty] private LinkageViewModel? selectedLinkage;
    [ObservableProperty] private CalibrationViewModel? selectedFrontCalibration;
    [ObservableProperty] private CalibrationViewModel? selectedRearCalibration;
    
    #endregion

    #region Property change handlers

    partial void OnSelectedLinkageChanged(LinkageViewModel? value)
    {
        IsDirty = value == null || value.Linkage.Id != Setup.LinkageId;
    }

    partial void OnSelectedFrontCalibrationChanged(CalibrationViewModel? value)
    {
        IsDirty = value == null || value.Calibration.Id != Setup.FrontCalibrationId;
    }

    partial void OnSelectedRearCalibrationChanged(CalibrationViewModel? value)
    {
        IsDirty = value == null || value.Calibration.Id != Setup.RearCalibrationId;
    }

    #endregion

    #region Constructors

    public SetupViewModel(Setup setup, ObservableCollection<LinkageViewModel> linkages, ObservableCollection<CalibrationViewModel> calibrations)
    {
        Setup = setup;
        Linkages = linkages;
        Calibrations = calibrations;

        SelectedLinkage = Linkages.First(l => l.Linkage.Id == setup.LinkageId);
        SelectedFrontCalibration = Calibrations.First(c => c.Calibration.Id == setup.FrontCalibrationId);
        SelectedRearCalibration = Calibrations.First(c => c.Calibration.Id == setup.RearCalibrationId);
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