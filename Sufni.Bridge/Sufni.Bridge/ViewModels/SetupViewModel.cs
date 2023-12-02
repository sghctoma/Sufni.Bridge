using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class SetupViewModel : ViewModelBase
{
    private Setup setup;
    private string? originalBoardId;
    public Guid Guid { get; } = Guid.NewGuid();

    #region Observable properties
    
    [ObservableProperty] private int? id;
    [ObservableProperty] private bool isDirty;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private string? name;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private string? boardId;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private LinkageViewModel? selectedLinkage;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private CalibrationViewModel? selectedFrontCalibration;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private CalibrationViewModel? selectedRearCalibration;

    public ReadOnlyObservableCollection<LinkageViewModel> Linkages => linkages;
    private readonly ReadOnlyObservableCollection<LinkageViewModel> linkages;
    
    public ReadOnlyObservableCollection<CalibrationViewModel> Calibrations => calibrations;
    private readonly ReadOnlyObservableCollection<CalibrationViewModel> calibrations;
    
    #endregion

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Id == null ||
            Name != setup.Name ||
            BoardId != originalBoardId ||
            SelectedLinkage == null || SelectedLinkage.Id != setup.LinkageId ||
            SelectedFrontCalibration?.Id != setup.FrontCalibrationId ||
            SelectedRearCalibration?.Id != setup.RearCalibrationId;
    }

    #endregion
    
    #region Constructors
    
    public SetupViewModel(Setup setup, string? boardId,
        SourceCache<LinkageViewModel, Guid> linkagesSourceCache,
        SourceCache<CalibrationViewModel, Guid> calibrationsSourceCache)
    {
        this.setup = setup;
        Id = setup.Id;
        BoardId = originalBoardId = boardId;
        
        linkagesSourceCache.Connect()
            .Bind(out linkages)
            .DisposeMany()
            .Subscribe();
        
        calibrationsSourceCache.Connect()
            .Bind(out calibrations)
            .DisposeMany()
            .Subscribe();
        
        Reset();
    }

    #endregion

    #region Commands

    private bool CanSave()
    {
        EvaluateDirtiness();
        return IsDirty && !(SelectedFrontCalibration == null && SelectedRearCalibration == null);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        Debug.Assert(SelectedLinkage != null, nameof(SelectedLinkage) + " != null");
        Debug.Assert(SelectedLinkage.Id != null, "SelectedLinkage.Id != null");
        Debug.Assert(!(SelectedFrontCalibration == null && SelectedRearCalibration == null), 
            nameof(SelectedFrontCalibration) + " and " + nameof(SelectedRearCalibration) + " can't be both null");
        
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var newSetup = new Setup(
                Id,
                Name ?? $"setup #{Id}",
                SelectedLinkage.Id.Value,
                SelectedFrontCalibration?.Id,
                SelectedRearCalibration?.Id);
            Id = await databaseService.PutSetupAsync(newSetup);
            
            // If this setup was already associated with another board, clear that association.
            // Do not delete the board though, it might be picked up later.
            if (!string.IsNullOrEmpty(originalBoardId) && originalBoardId != BoardId)
            {
                await databaseService.PutBoardAsync(new Board(originalBoardId, null));
            }
            
            // If the board ID changed, associate this setup with the new ID.
            if (!string.IsNullOrEmpty(BoardId) && originalBoardId != BoardId)
            {
                await databaseService.PutBoardAsync(new Board(BoardId!, Id));
            }
            
            setup = newSetup;
            originalBoardId = BoardId;
            
            SaveCommand.NotifyCanExecuteChanged();
            ResetCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Setup could not be saved: {e.Message}");
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