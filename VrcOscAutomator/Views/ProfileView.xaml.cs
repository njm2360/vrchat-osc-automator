using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using VrcOscAutomator.ViewModels;

namespace VrcOscAutomator.Views;

public partial class ProfileView : UserControl
{
    public ProfileView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private ProfileViewModel? _vm;

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        _vm?.SelectionRequested -= OnSelectionRequested;
        _vm = e.NewValue as ProfileViewModel;
        _vm?.SelectionRequested += OnSelectionRequested;
    }

    private void SlotsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var grid = (DataGrid)sender;
        if (_vm is null) return;

        var selected = grid.SelectedItems.Cast<SequenceSlotViewModel>().ToList();
        _vm.SetSelectedSlots(selected);

        grid.RowDetailsVisibilityMode = selected.Count <= 1
            ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
            : DataGridRowDetailsVisibilityMode.Collapsed;
    }

    private void OnSelectionRequested(object? sender, IList<SequenceSlotViewModel> slots)
    {
        SlotsDataGrid.SelectionChanged -= SlotsDataGrid_SelectionChanged;
        SlotsDataGrid.SelectedItems.Clear();
        foreach (var slot in slots)
            SlotsDataGrid.SelectedItems.Add(slot);
        SlotsDataGrid.SelectionChanged += SlotsDataGrid_SelectionChanged;

        // 最終状態を手動でVMへ同期
        _vm!.SetSelectedSlots(slots.ToList());
        SlotsDataGrid.RowDetailsVisibilityMode = slots.Count <= 1
            ? DataGridRowDetailsVisibilityMode.VisibleWhenSelected
            : DataGridRowDetailsVisibilityMode.Collapsed;
    }
}
