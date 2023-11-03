using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    private Setup setup;

    #region Observable properties

    [ObservableProperty] private int? id;
    [ObservableProperty] private string name;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
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
        Id = setup.Id;
        Linkages = linkages;
        Calibrations = calibrations;
        Reset();
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
        Debug.Assert(SelectedLinkage != null, nameof(SelectedLinkage) + " != null");
        Debug.Assert(SelectedLinkage.Id != null, "SelectedLinkage.Id != null");
        Debug.Assert(SelectedFrontCalibration != null, nameof(SelectedFrontCalibration) + " != null");
        Debug.Assert(SelectedRearCalibration != null, nameof(SelectedRearCalibration) + " != null");
        
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        var newSetup = new Setup(
            Id,
            Name,
            SelectedLinkage.Id.Value,
            SelectedFrontCalibration.Id,
            SelectedRearCalibration.Id);
        httpApiService.PutSetup(newSetup);
        setup = newSetup;
        IsDirty = false;
    }

    private bool CanReset()
    {
        return IsDirty;
    }
    
    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        Name = setup.Name;
        SelectedLinkage = Linkages.First(l => l.Id == setup.LinkageId);
        SelectedFrontCalibration = Calibrations.FirstOrDefault(c => c.Id == setup.FrontCalibrationId, null);
        SelectedRearCalibration = Calibrations.FirstOrDefault(c => c.Id == setup.RearCalibrationId, null);
    }

    #endregion
}