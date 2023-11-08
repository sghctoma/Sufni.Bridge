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

public partial class SetupViewModel : ViewModelBase
{
    private Setup setup;
    private string? originalBoardId;

    #region Observable properties
    [ObservableProperty] private int? id;
    [ObservableProperty] private string? name;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool isDirty;

    public ObservableCollection<LinkageViewModel> Linkages { get; }
    public ObservableCollection<CalibrationViewModel> Calibrations { get; }

    [ObservableProperty] private string? boardId;
    [ObservableProperty] private LinkageViewModel? selectedLinkage;
    [ObservableProperty] private CalibrationViewModel? selectedFrontCalibration;
    [ObservableProperty] private CalibrationViewModel? selectedRearCalibration;
    
    #endregion

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Name != setup.Name ||
            BoardId != originalBoardId ||
            SelectedLinkage == null || SelectedLinkage.Id != setup.LinkageId ||
            SelectedFrontCalibration?.Id != setup.FrontCalibrationId ||
            SelectedRearCalibration?.Id != setup.RearCalibrationId;
    }

    #endregion

    #region Property change handlers

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedLinkageChanged(LinkageViewModel? value)
    {
        EvaluateDirtiness();
    }
    
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedFrontCalibrationChanged(CalibrationViewModel? value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnSelectedRearCalibrationChanged(CalibrationViewModel? value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnNameChanged(string? value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnBoardIdChanged(string? value)
    {
        EvaluateDirtiness();
    }
    
    #endregion

    #region Constructors
    
    public SetupViewModel(Setup setup, string? boardId, ObservableCollection<LinkageViewModel> linkages, ObservableCollection<CalibrationViewModel> calibrations)
    {
        this.setup = setup;
        Id = setup.Id;
        BoardId = originalBoardId = boardId;
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
        Debug.Assert(!(SelectedFrontCalibration == null && SelectedRearCalibration == null), 
            nameof(SelectedFrontCalibration) + " and " + nameof(SelectedRearCalibration) + " can't be both null");
        
        var httpApiService = App.Current?.Services?.GetService<IHttpApiService>();
        Debug.Assert(httpApiService != null, nameof(httpApiService) + " != null");

        try
        {
            var newSetup = new Setup(
                Id,
                Name ?? $"setup #{Id}",
                SelectedLinkage.Id.Value,
                SelectedFrontCalibration?.Id,
                SelectedRearCalibration?.Id);
            httpApiService.PutSetupAsync(newSetup);
            setup = newSetup;
            originalBoardId = BoardId;
            IsDirty = false;
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Setup could not be saved: {e.Message}");
        }

        try
        {
            httpApiService.PutBoardAsync(new Board(BoardId!, Id));
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not associate Setup with Board: {e.Message}");
        }
    }

    private bool CanReset()
    {
        return IsDirty;
    }
    
    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        try
        {
            Name = setup.Name;
            BoardId = originalBoardId;
            SelectedLinkage = Linkages.First(l => l.Id == setup.LinkageId);
            SelectedFrontCalibration = Calibrations.FirstOrDefault(c => c?.Id == setup.FrontCalibrationId, null);
            SelectedRearCalibration = Calibrations.FirstOrDefault(c => c?.Id == setup.RearCalibrationId, null);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Setup could not be reset: {e.Message}");
        }
    }

    #endregion
}