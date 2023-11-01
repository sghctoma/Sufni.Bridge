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
    private readonly Linkage linkage;
    private string? linkageData;

    #region Private methods

    private void EvaluateDirtiness()
    {
        IsDirty =
            Name == null || Name != linkage.Name ||
            HeadAngle == null || HeadAngle != linkage.HeadAngle ||
            FrontStroke == null || FrontStroke != linkage.FrontStroke ||
            RearStroke == null || RearStroke != linkage.RearStroke ||
            LinkageDataFile != null;
    }

    #endregion
    
    #region Observable properties

    [ObservableProperty] private int? id;
    [ObservableProperty] private string? name;
    [ObservableProperty] private double? headAngle;
    [ObservableProperty] private double? frontStroke;
    [ObservableProperty] private double? rearStroke;
    [ObservableProperty] private string? linkageDataFile;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool isDirty;
    
    #endregion

    #region Property change handlers
    
    partial void OnNameChanged(string? value)
    {
        EvaluateDirtiness();
    }

    partial void OnHeadAngleChanged(double? value)
    {
        EvaluateDirtiness();
    }

    partial void OnFrontStrokeChanged(double? value)
    {
        EvaluateDirtiness();
    }

    partial void OnRearStrokeChanged(double? value)
    {
        EvaluateDirtiness();
    }

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
        Name = linkage.Name;
        HeadAngle = linkage.HeadAngle;
        FrontStroke = linkage.FrontStroke;
        RearStroke = linkage.RearStroke;
        LinkageDataFile = null;
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
        
    }

    [RelayCommand]
    private void Delete()
    {
        
    }
    
    [RelayCommand]
    private async Task OpenLeverageRatioFile(CancellationToken token)
    {
        var filesService = App.Current?.Services?.GetService<IFilesService>();
        Debug.Assert(filesService != null, nameof(filesService) + " != null");
        
        var file = await filesService.OpenLeverageRatioFileAsync();
        if (file is null) return;

        if ((await file.GetBasicPropertiesAsync()).Size <= 1024 * 1024 * 1)
        {
            LinkageDataFile = file.Name;
            await using var readStream = await file.OpenReadAsync();
            using var reader = new StreamReader(readStream);
            linkageData = await reader.ReadToEndAsync(token);
        }
    }
    
    #endregion
}