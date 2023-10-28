using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    [ObservableProperty] private string? name;
    public ObservableCollection<Linkage> Linkages { get; }
    public ObservableCollection<Calibration> Calibrations { get; }
    [ObservableProperty] private Linkage? selectedLinkage;
    [ObservableProperty] private Calibration? selectedFrontCalibration;
    [ObservableProperty] private Calibration? selectedRearCalibration;
    
    public SetupViewModel(Setup setup, ObservableCollection<Linkage> linkages, ObservableCollection<Calibration> calibrations)
    {
        Name = setup.Name;
        Linkages = linkages;
        Calibrations = calibrations;

        SelectedLinkage = Linkages.First(l => l.Id == setup.LinkageId);
        SelectedFrontCalibration = Calibrations.First(c => c.Id == setup.FrontCalibrationId);
        SelectedRearCalibration = Calibrations.First(c => c.Id == setup.RearCalibrationId);
    }
}