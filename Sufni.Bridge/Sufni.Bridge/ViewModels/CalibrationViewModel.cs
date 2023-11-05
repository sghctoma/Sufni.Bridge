using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;

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

    // ReSharper disable once ParameterHidesMember
    partial void OnValueChanged(double value)
    {
        IsDirty = Math.Abs(value - originalValue) > 0.00001;
    }

    #endregion
}

public partial class CalibrationViewModel : ViewModelBase
{
    private Calibration calibration;
    
    #region Observable properties
    
    [ObservableProperty] private int? id;
    [ObservableProperty] private string? name;
    [ObservableProperty] private CalibrationMethod? selectedCalibrationMethod;

    public ObservableCollection<CalibrationMethod> CalibrationMethods { get; }
    public ObservableCollection<CalibrationInputViewModel> Inputs { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
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
    
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnNameChanged(string? value)
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
                ivm.PropertyChanged += (_, _) => EvaluateDirtiness();
                Inputs.Add(ivm);
            }
        }
        else
        {
            foreach (var input in value.Properties.Inputs)
            {
                var ivm = new CalibrationInputViewModel(input);
                ivm.PropertyChanged += (_, _) => EvaluateDirtiness();
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
        Debug.Assert(SelectedCalibrationMethod != null, nameof(SelectedCalibrationMethod) + " != null");
        
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");
        
        var newCalibration = new Calibration(
            Id,
            Name ?? $"calibration #{Id}",
            SelectedCalibrationMethod.Id,
            Inputs.ToDictionary(input => input.Name, input => input.Value));
        httpApiService.PutCalibration(newCalibration);
        calibration = newCalibration;
        IsDirty = false;
    }

    private bool CanReset()
    {
        return IsDirty;
    }
    
    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        Name = calibration.Name;
        SelectedCalibrationMethod = null; // so that OnSelectedCalibrationMethodChanged kicks in, resetting inputs too
        SelectedCalibrationMethod = CalibrationMethods.First(m => m.Id == calibration.MethodId);
    }

    #endregion
}