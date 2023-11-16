using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Models;
using Sufni.Bridge.Services;

namespace Sufni.Bridge.ViewModels;

public partial class LinkageViewModel : ViewModelBase
{
    private Linkage linkage;
    private string? linkageData;

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Name != linkage.Name ||
            Math.Abs(HeadAngle - linkage.HeadAngle) > 0.00001 ||
            Math.Abs((FrontStroke ?? 0.0) - (linkage.FrontStroke ?? 0.0)) > 0.00001 ||
            Math.Abs((RearStroke ?? 0.0) - (linkage.RearStroke ?? 0.0)) > 0.00001 ||
            LinkageDataFile != null;
    }

    #endregion
    
    #region Observable properties

    [ObservableProperty] private int? id;
    [ObservableProperty] private string? name;
    [ObservableProperty] private double headAngle;
    [ObservableProperty] private double? frontStroke;
    [ObservableProperty] private double? rearStroke;
    [ObservableProperty] private string? linkageDataFile;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool isDirty;
    
    #endregion

    #region Property change handlers
    
    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnNameChanged(string? value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnHeadAngleChanged(double value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnFrontStrokeChanged(double? value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnRearStrokeChanged(double? value)
    {
        EvaluateDirtiness();
    }

    // ReSharper disable once UnusedParameterInPartialMethod
    partial void OnLinkageDataFileChanged(string? value)
    {
        EvaluateDirtiness();
    }

    #endregion

    #region Constructors

    public LinkageViewModel(Linkage linkage)
    {
        this.linkage = linkage;
        Id = linkage.Id;
        Reset();
    }

    #endregion

    #region Commands

    private bool CanSave()
    {
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
                linkageData);
            await databaseService.PutLinkageAsync(newLinkage);
            linkage = newLinkage;
            IsDirty = false;
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Linkage could not be saved: {e.Message}");
        }
    }

    private bool CanReset()
    {
        return IsDirty;
    }
    
    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        Name = linkage.Name;
        HeadAngle = linkage.HeadAngle;
        FrontStroke = linkage.FrontStroke;
        RearStroke = linkage.RearStroke;
        LinkageDataFile = null;
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
                LinkageDataFile = file.Name;
                await using var readStream = await file.OpenReadAsync();
                using var reader = new StreamReader(readStream);
                linkageData = await reader.ReadToEndAsync(token);
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