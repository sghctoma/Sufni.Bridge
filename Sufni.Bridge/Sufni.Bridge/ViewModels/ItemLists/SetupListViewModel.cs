﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using Sufni.Bridge.Models;
using Sufni.Bridge.ViewModels.Items;

namespace Sufni.Bridge.ViewModels.ItemLists;

public class SetupListViewModel : ItemListViewModelBase
{
    private ObservableCollection<Board> Boards { get; } = [];
    private readonly LinkageListViewModel linkagesPage;
    private readonly CalibrationListViewModel calibrationsPage;
    private readonly ImportSessionsViewModel importSessionsPage;

    public SetupListViewModel()
    {
        linkagesPage = new();
        calibrationsPage = new();
        importSessionsPage = new();
    }

    public SetupListViewModel(LinkageListViewModel linkagesPage, CalibrationListViewModel calibrationsPage, ImportSessionsViewModel importSessionsPage)
    {
        this.linkagesPage = linkagesPage;
        this.calibrationsPage = calibrationsPage;
        this.importSessionsPage = importSessionsPage;
    }

    protected override async Task DeleteImplementation(ItemViewModelBase vm)
    {
        var svm = vm as SetupViewModel;
        Debug.Assert(svm != null, nameof(svm) + " != null");
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        // If this setup is associated with a board ID, clear that association.
        if (svm.BoardId is not null)
        {
            await databaseService.PutBoardAsync(new Board(svm.BoardId, null));
        }

        // Notify associated calibrations and linkages about the deletion
        await databaseService.DeleteSetupAsync(vm.Id);
        svm.SelectedFrontCalibration?.DeleteCommand.NotifyCanExecuteChanged();
        svm.SelectedRearCalibration?.DeleteCommand.NotifyCanExecuteChanged();
        svm.SelectedLinkage?.DeleteCommand.NotifyCanExecuteChanged();
        svm.SelectedFrontCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
        svm.SelectedRearCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
        svm.SelectedLinkage?.FakeDeleteCommand.NotifyCanExecuteChanged();
    }

    public override async Task LoadFromDatabase()
    {
        Source.Clear();
        Boards.Clear();
        await LoadBoardsAsync();
        await LoadSetupsAsync();
    }

    protected override void AddImplementation()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var setup = new Setup(
                Guid.NewGuid(),
                "new setup",
                linkagesPage.Items[0].Id,
                null,
                null);

            // Use the SST datastore's board ID only if it's not already associated to another setup;
            string? newSetupsBoardId = null;
            var datastoreBoardId = importSessionsPage.SelectedDataStore?.BoardId;
            var datastoreBoard = Boards.FirstOrDefault(b =>
                b?.Id.ToLower() == datastoreBoardId && b?.SetupId is not null, null);
            if (datastoreBoard is null || datastoreBoard.SetupId is null)
            {
                newSetupsBoardId = datastoreBoardId;
            }

            var svm = new SetupViewModel(setup, newSetupsBoardId, false, linkagesPage.Source, calibrationsPage.Source)
            {
                IsDirty = true
            };
            svm.PropertyChanged += OnSetupDirtinessChanged;

            OpenPage(svm);
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not add Setup: {e.Message}");
        }
    }

    private async Task LoadBoardsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var boards = await databaseService.GetBoardsAsync();

            foreach (var board in boards)
            {
                Boards.Add(board);
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Boards: {e.Message}");
        }
    }

    private void OnSetupDirtinessChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SetupViewModel.IsDirty) &&
            sender is SetupViewModel svm &&
            !svm.IsDirty)
        {
            svm.SelectedFrontCalibration?.DeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedRearCalibration?.DeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedLinkage?.DeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedFrontCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedRearCalibration?.FakeDeleteCommand.NotifyCanExecuteChanged();
            svm.SelectedLinkage?.FakeDeleteCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task LoadSetupsAsync()
    {
        Debug.Assert(databaseService != null, nameof(databaseService) + " != null");

        try
        {
            var setupList = await databaseService.GetSetupsAsync();
            foreach (var setup in setupList)
            {
                var board = Boards.FirstOrDefault(b => b?.SetupId == setup.Id, null);
                var svm = new SetupViewModel(
                    setup,
                    board?.Id,
                    true,
                    linkagesPage.Source,
                    calibrationsPage.Source);
                svm.PropertyChanged += OnSetupDirtinessChanged;
                Source.AddOrUpdate(svm);
            }
        }
        catch (Exception e)
        {
            ErrorMessages.Add($"Could not load Setups: {e.Message}");
        }
    }
}
