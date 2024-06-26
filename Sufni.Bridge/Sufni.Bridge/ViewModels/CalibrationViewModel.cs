using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class CalibrationInputViewModel : ViewModelBase
{
    #region Observable properties
    
    [ObservableProperty] private string name;
    [ObservableProperty] private double value;
    [ObservableProperty] private bool isDirty;
    [ObservableProperty] private double originalValue;
    
    #endregion

    #region Constructors

    public CalibrationInputViewModel(string name)
    {    
        OriginalValue = value;
        Name = name;
        Value = 0.0;
        IsDirty = true;
    }
    
    public CalibrationInputViewModel(string name, double value)
    {    
        OriginalValue = value;
        Name = name;
        Value = value;
    }

    #endregion

    #region Property change handlers

    // ReSharper disable once ParameterHidesMember
    partial void OnValueChanged(double value)
    {
        IsDirty = Math.Abs(value - OriginalValue) > 0.00001;
    }

    #endregion
}

public partial class CalibrationViewModel : ViewModelBase
{
    private Calibration calibration;
    public bool IsInDatabase;

    #region Observable properties
    
    [ObservableProperty] private Guid id;
    [ObservableProperty] private bool isDirty;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private string? name;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private CalibrationMethod? selectedCalibrationMethod;

    public ObservableCollection<CalibrationMethod> CalibrationMethods { get; }
    public ObservableCollection<CalibrationInputViewModel> Inputs { get; } = new();
    
    #endregion

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            !IsInDatabase ||
            Name != calibration.Name ||
            SelectedCalibrationMethod == null || SelectedCalibrationMethod.Id != calibration.MethodId ||
            Inputs.Any(i => i.IsDirty);
    }

    #endregion
    
    #region Property change handlers

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
                ivm.PropertyChanged += (_, _) =>
                {
                    SaveCommand.NotifyCanExecuteChanged();
                    ResetCommand.NotifyCanExecuteChanged();
                };
                Inputs.Add(ivm);
            }
        }
        else
        {
            foreach (var input in value.Properties.Inputs)
            {
                var ivm = new CalibrationInputViewModel(input);
                ivm.PropertyChanged += (_, _) =>
                {
                    SaveCommand.NotifyCanExecuteChanged();
                    ResetCommand.NotifyCanExecuteChanged();
                };
                Inputs.Add(ivm);
            }
        }
    }

    #endregion

    #region Constructors

    public CalibrationViewModel()
    {
        CalibrationMethods = new ObservableCollection<CalibrationMethod>();
        calibration = new Calibration();
        Reset();
    }
    
    public CalibrationViewModel(Calibration calibration, ObservableCollection<CalibrationMethod> calibrationMethods, bool fromDatabase)
    {
        this.calibration = calibration;
        IsInDatabase = fromDatabase;
        Id = calibration.Id;
        CalibrationMethods = calibrationMethods;
        Reset();
    }

    #endregion

    #region Commands

    private bool CanSave()
    {
        EvaluateDirtiness();
        return IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        Debug.Assert(SelectedCalibrationMethod != null, nameof(SelectedCalibrationMethod) + " != null");
        
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var newCalibration = new Calibration(
                Id,
                Name ?? $"calibration #{Id}",
                SelectedCalibrationMethod.Id,
                Inputs.ToDictionary(input => input.Name, input => input.Value));
            Id = await databaseService.PutCalibrationAsync(newCalibration);
            
            calibration = newCalibration;
            foreach (var input in Inputs)
            {
                input.OriginalValue = input.Value;
                input.IsDirty = false;
            }

            if (!IsInDatabase)
            {
                var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>(); 
                Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");
                await mainPagesViewModel.OnEntityAdded(this);   
            }
            
            IsDirty = false;
            IsInDatabase = true;
            
            OpenPreviousPage();
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Calibration could not be saved: {e.Message}");
        }
    }

    private bool CanReset()
    {
        EvaluateDirtiness();
        return IsDirty;
    }
    
    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        try
        {
            Name = calibration.Name;
            SelectedCalibrationMethod = null; // so that OnSelectedCalibrationMethodChanged kicks in, resetting inputs too
            SelectedCalibrationMethod = CalibrationMethods.First(m => m.Id == calibration.MethodId);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Calibration could not be reset: {e.Message}");
        }
    }

    [RelayCommand]
    private void Select()
    {
        var mainViewModel = App.Current?.Services?.GetService<MainViewModel>();
        Debug.Assert(mainViewModel != null, nameof(mainViewModel) + " != null");

        mainViewModel.CurrentView = this;
    }

    private bool CanDelete()
    {
        var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
        Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");
        
        return !mainPagesViewModel.Setups.Any(s =>
            (s.SelectedFrontCalibration != null && s.SelectedFrontCalibration.Id == Id) ||
            (s.SelectedRearCalibration != null && s.SelectedRearCalibration.Id == Id));
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete()
    {
        var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
        Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

        await mainPagesViewModel.DeleteCalibrationCommand.ExecuteAsync(this);
        
        OpenPreviousPage();
    }

    #endregion
}