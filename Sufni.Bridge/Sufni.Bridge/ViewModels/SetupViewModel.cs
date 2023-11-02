using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    private readonly Setup setup;

    #region Observable properties

    [ObservableProperty] private string name;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isDirty;

    public ObservableCollection<LinkageViewModel> Linkages { get; }
    public ObservableCollection<CalibrationViewModel> Calibrations { get; }
    
    [ObservableProperty] private LinkageViewModel? selectedLinkage;
    [ObservableProperty] private CalibrationViewModel? selectedFrontCalibration;
    [ObservableProperty] private CalibrationViewModel? selectedRearCalibration;
    
    #endregion

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Name != setup.Name ||
            SelectedLinkage == null || SelectedLinkage.Id != setup.LinkageId ||
            SelectedFrontCalibration == null || SelectedFrontCalibration.Id != setup.FrontCalibrationId ||
            SelectedRearCalibration == null || SelectedRearCalibration.Id != setup.RearCalibrationId;
    }

    #endregion

    #region Property change handlers

    partial void OnSelectedLinkageChanged(LinkageViewModel? value)
    {
        EvaluateDirtiness();
    }

    partial void OnSelectedFrontCalibrationChanged(CalibrationViewModel? value)
    {
        EvaluateDirtiness();
    }

    partial void OnSelectedRearCalibrationChanged(CalibrationViewModel? value)
    {
        EvaluateDirtiness();
    }

    partial void OnNameChanged(string value)
    {
        EvaluateDirtiness();
    }

    #endregion

    #region Constructors

    public SetupViewModel(Setup setup, ObservableCollection<LinkageViewModel> linkages, ObservableCollection<CalibrationViewModel> calibrations)
    {
        this.setup = setup;
        Name = setup.Name;
        Linkages = linkages;
        Calibrations = calibrations;

        SelectedLinkage = Linkages.First(l => l.Id == setup.LinkageId);
        SelectedFrontCalibration = Calibrations.First(c => c.Id == setup.FrontCalibrationId);
        SelectedRearCalibration = Calibrations.First(c => c.Id == setup.RearCalibrationId);
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

    [RelayCommand]
    private void Reset()
    {
        
    }

    [RelayCommand]
    private void Delete()
    {
        
    }

    #endregion
}