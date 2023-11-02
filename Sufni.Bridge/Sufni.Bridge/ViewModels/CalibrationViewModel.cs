using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sufni.Bridge.Models;

namespace Sufni.Bridge.ViewModels;

public partial class CalibrationInputViewModel : ViewModelBase
{
    private readonly double originalValue;
    
    #region Observable properties
    
    [ObservableProperty] private string name;
    [ObservableProperty] private double value;
    [ObservableProperty] private bool isDirty;
    
    #endregion

    #region Constructors

    public CalibrationInputViewModel(string name)
    {    
        originalValue = value;
        Name = name;
        Value = 0.0;
        IsDirty = true;
    }
    
    public CalibrationInputViewModel(string name, double value)
    {    
        originalValue = value;
        Name = name;
        Value = value;
    }

    #endregion

    #region Property change handlers

    partial void OnValueChanged(double value)
    {
        IsDirty = Math.Abs(value - originalValue) > 0.00001;
    }

    #endregion
}

public partial class CalibrationViewModel : ViewModelBase
{
    private readonly Calibration calibration;
    
    #region Observable properties
    
    [ObservableProperty] private int? id;
    [ObservableProperty] private string name;
    [ObservableProperty] private CalibrationMethod? selectedCalibrationMethod;

    public ObservableCollection<CalibrationMethod> CalibrationMethods { get; }
    public ObservableCollection<CalibrationInputViewModel> Inputs { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isDirty;
    
    #endregion

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Name != calibration.Name ||
            SelectedCalibrationMethod == null || SelectedCalibrationMethod.Id != calibration.MethodId ||
            Inputs.Any(i => i.IsDirty);
    }

    #endregion
    
    #region Property change handlers
    
    partial void OnNameChanged(string value)
    {
        EvaluateDirtiness();
    }

    partial void OnSelectedCalibrationMethodChanged(CalibrationMethod? value)
    {
        if (value == null)
        {
            IsDirty = true;
            return;
        }
        
        Inputs.Clear();
        if (value.Id == calibration.MethodId)
        {
            foreach (var input in calibration.Inputs)
            {
                var ivm = new CalibrationInputViewModel(input.Key, input.Value);
                ivm.PropertyChanged += (sender, args) => EvaluateDirtiness();
                Inputs.Add(ivm);
            }
        }
        else
        {
            foreach (var input in value.Properties.Inputs)
            {
                var ivm = new CalibrationInputViewModel(input);
                ivm.PropertyChanged += (sender, args) => EvaluateDirtiness();
                Inputs.Add(ivm);
            }
        }
        
        EvaluateDirtiness();
    }

    #endregion

    #region Constructors

    public CalibrationViewModel(Calibration calibration, ObservableCollection<CalibrationMethod> calibrationMethods)
    {
        this.calibration = calibration;
        Id = calibration.Id;
        CalibrationMethods = calibrationMethods;
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
        
    }

    [RelayCommand]
    private void Reset()
    {
        Name = calibration.Name;
        SelectedCalibrationMethod = null; // so that OnSelectedCalibrationMethodChanged kicks in, resetting inputs too
        SelectedCalibrationMethod = CalibrationMethods.First(m => m.Id == calibration.MethodId);
    }

    #endregion
}