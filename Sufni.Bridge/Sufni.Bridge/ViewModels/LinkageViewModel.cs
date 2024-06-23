using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models.Telemetry;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class LinkageViewModel : ItemViewModelBase
{
    private Linkage linkage;
    public bool IsInDatabase;
    
    #region Observable properties

    [ObservableProperty] private Guid id;
    
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

    public LinkageViewModel()
    {
        linkage = new Linkage();
        IsInDatabase = false;
        ResetImplementation();
    }
    
    public LinkageViewModel(Linkage linkage, bool fromDatabase)
    {
        this.linkage = linkage;
        IsInDatabase = fromDatabase;
        Id = linkage.Id;
        LeverageRatioData = linkage.LeverageRatioData;
        ResetImplementation();
    }

    #endregion

    #region ItemViewModelBase overrides

    protected override void EvaluateDirtiness()
    {
        IsDirty =
            !IsInDatabase ||
            Name != linkage.Name ||
            Math.Abs(HeadAngle - linkage.HeadAngle) > 0.00001 ||
            Math.Abs((FrontStroke ?? 0.0) - (linkage.MaxFrontStroke ?? 0.0)) > 0.00001 ||
            Math.Abs((RearStroke ?? 0.0) - (linkage.MaxRearStroke ?? 0.0)) > 0.00001 ||
            LeverageRatioData == null || !LeverageRatioData.Equals(linkage.LeverageRatioData);
    }

    protected override async Task SaveImplementation()
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

            if (!IsInDatabase)
            {
                var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
                Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");
                await mainPagesViewModel.OnEntityAdded(this);
            }

            IsInDatabase = true;

            OpenPreviousPage();
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Linkage could not be saved: {e.Message}");
        }
    }

    protected override Task ResetImplementation()
    {
        Name = linkage.Name;
        HeadAngle = linkage.HeadAngle;
        FrontStroke = linkage.MaxFrontStroke;
        RearStroke = linkage.MaxRearStroke;
        LeverageRatioData = linkage.LeverageRatioData;

        return Task.CompletedTask;
    }

    protected override bool CanDelete()
    {
        var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
        Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

        return !mainPagesViewModel.Setups.Any(s => s.SelectedLinkage != null && s.SelectedLinkage.Id == Id);
    }

    protected override async Task DeleteImplementation()
    {
        var mainPagesViewModel = App.Current?.Services?.GetService<MainPagesViewModel>();
        Debug.Assert(mainPagesViewModel != null, nameof(mainPagesViewModel) + " != null");

        await mainPagesViewModel.DeleteLinkageCommand.ExecuteAsync(this);
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