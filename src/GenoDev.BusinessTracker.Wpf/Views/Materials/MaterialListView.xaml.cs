using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
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
            _attachedViewModel.VariantsPaginationRefreshRequested -=
                ViewModel_VariantsPaginationRefreshRequested;
        }

        _attachedViewModel = viewModel;

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested +=
                ViewModel_PaginationRefreshRequested;
            _attachedViewModel.VariantsPaginationRefreshRequested +=
                ViewModel_VariantsPaginationRefreshRequested;
        }
    }

    private async void ViewModel_PaginationRefreshRequested()
    {
        await MaterialsPagination.RefreshAsync();
    }

    private async void ViewModel_VariantsPaginationRefreshRequested(
        bool resetPageIndex)
    {
        if (resetPageIndex)
        {
            await VariantsPagination.ResetAndRefreshAsync();
            return;
        }

        await VariantsPagination.RefreshAsync();
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

    private async void VariantFilterToggleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VariantsPagination.RefreshAsync();
    }

    private async void MaterialsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (!UpdateMaterialsFilter())
        {
            return;
        }

        await MaterialsPagination.RefreshAsync();
    }

    private async void MaterialsDataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdateMaterialsFilter() || !e.AffectsActiveFilter ||
            DataContext is not MaterialListViewModel { IsFilterVisible: true })
        {
            return;
        }

        await MaterialsPagination.RefreshAsync();
    }

    private bool UpdateMaterialsFilter()
    {
        if (DataContext is not MaterialListViewModel viewModel)
        {
            return false;
        }

        var isVariantsCountFilterActive =
            IsMaterialsColumnVisible("VariantsCount") &&
            VariantsCountFilterColumn.SelectedOperator.HasValue &&
            VariantsCountFilterColumn.FilterValue.HasValue;

        return viewModel.SetFilter(
            new MaterialFilterCriteria(
                IsMaterialsColumnVisible("Name")
                    ? NullIfWhiteSpace(NameFilterColumn.FilterText)
                    : null,
                IsMaterialsColumnVisible("Description")
                    ? NullIfWhiteSpace(DescriptionFilterColumn.FilterText)
                    : null,
                isVariantsCountFilterActive
                    ? VariantsCountFilterColumn.SelectedOperator
                    : null,
                isVariantsCountFilterActive
                    ? VariantsCountFilterColumn.FilterValue
                    : null));
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private bool IsMaterialsColumnVisible(string columnKey) =>
        MaterialsDataGrid.IsColumnVisible(columnKey);

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

    private async void VariantsRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await VariantsPagination.RefreshAsync();
    }

    private async void VariantsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (!UpdateVariantsFilter())
        {
            return;
        }

        await VariantsPagination.RefreshAsync();
    }

    private async void VariantsDataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdateVariantsFilter() || !e.AffectsActiveFilter ||
            DataContext is not MaterialListViewModel { IsVariantFilterVisible: true })
        {
            return;
        }

        await VariantsPagination.RefreshAsync();
    }

    private bool UpdateVariantsFilter()
    {
        if (DataContext is not MaterialListViewModel viewModel)
        {
            return false;
        }

        viewModel.SetVariantFilter(
            new MaterialVariantFilterCriteria(
                VariantsDataGrid.IsColumnVisible("Name") ? VariantNameFilterColumn.FilterText : null,
                VariantsDataGrid.IsColumnVisible("Ean") ? VariantEanFilterColumn.FilterText : null,
                VariantsDataGrid.IsColumnVisible("ManufacturerCode") ? VariantManufacturerCodeFilterColumn.FilterText : null,
                VariantsDataGrid.IsColumnVisible("Description") ? VariantDescriptionFilterColumn.FilterText : null,
                VariantsDataGrid.IsColumnVisible("Amount") ? VariantAmountFilterColumn.SelectedOperator : null,
                VariantsDataGrid.IsColumnVisible("Amount") ? VariantAmountFilterColumn.FilterValue : null,
                VariantsDataGrid.IsColumnVisible("TotalUsedAmount") ? VariantTotalUsedAmountFilterColumn.SelectedOperator : null,
                VariantsDataGrid.IsColumnVisible("TotalUsedAmount") ? VariantTotalUsedAmountFilterColumn.FilterValue : null));
        return true;
    }

    private async void VariantsDataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (DataContext is not MaterialListViewModel viewModel ||
            sender is not DataGrid dataGrid ||
            !Enum.TryParse(
                e.Column.SortMemberPath,
                ignoreCase: true,
                out MaterialVariantSortBy sortBy))
        {
            return;
        }

        e.Handled = true;

        var isDescending = viewModel.VariantSortBy == sortBy &&
                           !viewModel.IsVariantDescending;

        foreach (var column in dataGrid.Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = isDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        viewModel.SetVariantSorting(sortBy, isDescending);
        await VariantsPagination.RefreshAsync();
    }
}
