using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class LinkageViewModel : ViewModelBase
{
    private Linkage linkage;
    public Guid Guid { get; } = Guid.NewGuid();

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Id == null ||
            Name != linkage.Name ||
            Math.Abs(HeadAngle - linkage.HeadAngle) > 0.00001 ||
            Math.Abs((FrontStroke ?? 0.0) - (linkage.MaxFrontStroke ?? 0.0)) > 0.00001 ||
            Math.Abs((RearStroke ?? 0.0) - (linkage.MaxRearStroke ?? 0.0)) > 0.00001 || 
            LeverageRatioData == null || !LeverageRatioData.Equals(linkage.LeverageRatioData);
    }

    #endregion
    
    #region Observable properties

    [ObservableProperty] private Guid? id;
    [ObservableProperty] private bool isDirty;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private string? name;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private double headAngle;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private double? frontStroke;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private double? rearStroke;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private LeverageRatioData? leverageRatioData;
    
    #endregion

    #region Constructors

    public LinkageViewModel(Linkage linkage)
    {
        this.linkage = linkage;
        Id = linkage.Id;
        LeverageRatioData = linkage.LeverageRatioData;
        Reset();
    }

    #endregion

    #region Commands
    
    [RelayCommand]
    private void SetLeverageRatioData()
    {
        // Trigger LeverageRatioData change, so the plot will pick the data up.
        LeverageRatioData = null;
        LeverageRatioData = linkage.LeverageRatioData;
    }

    private bool CanSave()
    {
        EvaluateDirtiness();
        return IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        var databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var newLinkage = new Linkage(
                Id,
                Name ?? $"linkage #{Id}",
                HeadAngle,
                FrontStroke,
                RearStroke,
                LeverageRatioData!.ToString());
            Id = await databaseService.PutLinkageAsync(newLinkage);
            linkage = newLinkage;
            
            SaveCommand.NotifyCanExecuteChanged();
            ResetCommand.NotifyCanExecuteChanged();
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Linkage could not be saved: {e.Message}");
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
        Name = linkage.Name;
        HeadAngle = linkage.HeadAngle;
        FrontStroke = linkage.MaxFrontStroke;
        RearStroke = linkage.MaxRearStroke;
        LeverageRatioData = linkage.LeverageRatioData;
    }

    [RelayCommand]
    private async Task OpenLeverageRatioFile(CancellationToken token)
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        Debug.Assert(filesService != null, nameof(filesService) + " != null");
        
        var file = await filesService.OpenLeverageRatioFileAsync();
        if (file is null) return;

        try
        {
            if ((await file.GetBasicPropertiesAsync()).Size <= 1024 * 1024 * 1)
            {
                await using var readStream = await file.OpenReadAsync();
                using var reader = new StreamReader(readStream);
                LeverageRatioData = new LeverageRatioData(reader);
            }
            else
            {
                ErrorMessages.Add("File is larger than 1 MB!");
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Leverage ratio file could not be read: {e.Message}");
        }
    }
    
    #endregion
}