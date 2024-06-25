﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace Sufni.Bridge.ViewModels.Items;

public partial class ItemViewModelBase : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool isDirty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private string? name;

    [ObservableProperty] private Guid id;
    [ObservableProperty] private DateTime? timestamp;

    protected virtual void EvaluateDirtiness() { IsDirty = false; }
    protected virtual bool CanDelete() { return true; }
    protected virtual Task SaveImplementation() { return Task.CompletedTask; }
    protected virtual Task ResetImplementation() { return Task.CompletedTask; }
    protected virtual Task DeleteImplementation() { return Task.CompletedTask; }

    protected virtual bool CanSave()
    {
        EvaluateDirtiness();
        return IsDirty;
    }

    protected virtual bool CanReset()
    {
        EvaluateDirtiness();
        return IsDirty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        await SaveImplementation();
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private async Task Reset()
    {
        await ResetImplementation();
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task Delete(bool navigateBack)
    {
        await DeleteImplementation();
        if (navigateBack)
        {
            OpenPreviousPage();
        }
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void FakeDelete()
    {
        // This exists just so we can easily control the enabled/disabled
        // state of the Delete button on the CommonButtonLine.
    }
}