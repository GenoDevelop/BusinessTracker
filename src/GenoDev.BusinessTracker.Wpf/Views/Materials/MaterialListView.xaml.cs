using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class MaterialListView : UserControl
{
    private MaterialListViewModel? _attachedViewModel;

    public MaterialListView()
    {
        InitializeComponent();

        Loaded += MaterialListView_Loaded;
        Unloaded += MaterialListView_Unloaded;
        DataContextChanged += MaterialListView_DataContextChanged;
    }

    private void MaterialListView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as MaterialListViewModel);
    }

    private void MaterialListView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void MaterialListView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as MaterialListViewModel);
        }
    }

    private void AttachViewModel(MaterialListViewModel? viewModel)
    {
        if (ReferenceEquals(_attachedViewModel, viewModel))
        {
            return;
        }

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested -=
                ViewModel_PaginationRefreshRequested;
        }

        _attachedViewModel = viewModel;

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested +=
                ViewModel_PaginationRefreshRequested;
        }
    }

    private async void ViewModel_PaginationRefreshRequested()
    {
        await MaterialsPagination.RefreshAsync();
    }

    private async void RefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await MaterialsPagination.RefreshAsync();
    }

    private async void FilterToggleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await MaterialsPagination.RefreshAsync();
    }

    private async void MaterialsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MaterialListViewModel viewModel)
        {
            return;
        }

        viewModel.SetFilter(
            new MaterialFilterCriteria(
                NameFilterColumn.FilterText,
                EanFilterColumn.FilterText,
                UnitFilterColumn.FilterText,
                AmountFilterColumn.FilterValue,
                AmountFilterColumn.SelectedOperator,
                DescriptionFilterColumn.FilterText));

        await MaterialsPagination.RefreshAsync();
    }

    private async void MaterialsDataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (DataContext is not MaterialListViewModel viewModel ||
            sender is not DataGrid dataGrid ||
            !Enum.TryParse(
                e.Column.SortMemberPath,
                ignoreCase: true,
                out MaterialSortBy sortBy))
        {
            return;
        }

        e.Handled = true;

        var isDescending = viewModel.SortBy == sortBy &&
                           !viewModel.IsDescending;

        foreach (var column in dataGrid.Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = isDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        viewModel.SetSorting(sortBy, isDescending);
        await MaterialsPagination.RefreshAsync();
    }
}