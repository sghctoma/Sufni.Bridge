﻿using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using Microsoft.Extensions.DependencyInjection;
using Sufni.Bridge.Services;
using Sufni.Bridge.ViewModels.Items;

namespace Sufni.Bridge.ViewModels.ItemLists;

public partial class ItemListViewModelBase : ViewModelBase
{
    protected readonly IDatabaseService? databaseService;

    [ObservableProperty] private string? searchText;
    public readonly SourceCache<ItemViewModelBase, Guid> Source = new(x => x.Id);
    public ReadOnlyObservableCollection<ItemViewModelBase> Items => items;
    protected ReadOnlyObservableCollection<ItemViewModelBase> items;
    public ObservableCollection<PullMenuItemViewModel> MenuItems { get; set; } = [];
    [ObservableProperty] private ItemViewModelBase? lastDeleted;

    public virtual Task LoadFromDatabase() { return Task.CompletedTask; }
    public virtual void ConnectSource()
    {
        Source.Connect()
            .Filter(vm => string.IsNullOrEmpty(SearchText) ||
                            (vm.Name is not null && vm.Name.Contains(SearchText,
                                StringComparison.CurrentCultureIgnoreCase)))
            .Bind(out items)
            .DisposeMany()
            .Subscribe();
    }

    protected virtual void AddImplementation() { }
    protected virtual Task DeleteImplementation(ItemViewModelBase vm) { return Task.CompletedTask; }

#pragma warning disable CS8618 // "items" is populated in the ConnectSource method
    public ItemListViewModelBase()
    {
        databaseService = App.Current?.Services?.GetService<IDatabaseService>();
        ConnectSource();
    }
#pragma warning restore CS8618

    partial void OnSearchTextChanged(string? value)
    {
        Source.Refresh();
    }

    public void OnAdded(ItemViewModelBase vm)
    {
        Source.AddOrUpdate(vm);
    }

    public async Task Delete(ItemViewModelBase vm)
    {
        Source.Remove(vm);
        await DeleteImplementation(vm);
    }

    public void UndoableDelete(ItemViewModelBase vm)
    {
        LastDeleted = vm;
        Source.Remove(vm);
    }

    [RelayCommand]
    private void Add()
    {
        AddImplementation();
    }

    [RelayCommand]
    private void ClearSearchText()
    {
        SearchText = null;
    }

    [RelayCommand]
    public async Task FinalizeDelete()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");
        Debug.Assert(LastDeleted != null, nameof(LastDeleted) + " != null");

        switch (LastDeleted)
        {
            case CalibrationViewModel:
                await databaseService.DeleteCalibrationAsync(LastDeleted.Id);
                break;
            case LinkageViewModel:
                await databaseService.DeleteLinkageAsync(LastDeleted.Id);
                break;
            case SetupViewModel:
                await databaseService.DeleteSetupAsync(LastDeleted.Id);
                break;
            case SessionViewModel:
                await databaseService.DeleteSessionAsync(LastDeleted.Id);
                break;
        }
        LastDeleted = null;
    }

    [RelayCommand]
    public void UndoDelete()
    {
        Debug.Assert(LastDeleted != null, nameof(LastDeleted) + " != null");

        Source.AddOrUpdate(LastDeleted);
        LastDeleted = null;
    }
}
