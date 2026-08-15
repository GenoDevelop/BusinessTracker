using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Views.Production;

public partial class ProductsView : UserControl
{
    private ProductsViewModel? _attachedViewModel;
    
    public ProductsView()
    {
        InitializeComponent();
    
        Loaded += ProductsView_Loaded;
        Unloaded += ProductsView_Unloaded;
        DataContextChanged += ProductsView_DataContextChanged;
    }
    
    private void ProductsView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as ProductsViewModel);
    }
    
    private void ProductsView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }
    
    private void ProductsView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as ProductsViewModel);
        }
    }
    
    private void AttachViewModel(ProductsViewModel? viewModel)
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
    
    private async void ViewModel_PaginationRefreshRequested(
        ProductsPaginationTarget target)
    {
        await ProductsPagination.RefreshAsync();
    }
    
    private async void ProductsRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await ProductsPagination.RefreshAsync();
    }
    
    private async void SearchTerm_SourceUpdated(
        object sender,
        DataTransferEventArgs e)
    {
        await ProductsPagination.RefreshAsync();
    }
    
    private async void ProductsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (!UpdateProductsFilter())
        {
            return;
        }
    
        await ProductsPagination.RefreshAsync();
    }

    private async void ProductsDataGrid_ColumnVisibilityChanged(
        object? sender,
        ConfigurableDataGridColumnVisibilityChangedEventArgs e)
    {
        if (!UpdateProductsFilter() || !e.AffectsActiveFilter ||
            DataContext is not ProductsViewModel { IsFilterVisible: true })
        {
            return;
        }

        await ProductsPagination.RefreshAsync();
    }

    private bool UpdateProductsFilter()
    {
        if (DataContext is not ProductsViewModel viewModel)
        {
            return false;
        }

        viewModel.SetProductsFilter(
            new ProductsFilterCriteria(
                ProductsDataGrid.IsColumnVisible("Name") ? NameFilterColumn.FilterText : null,
                ProductsDataGrid.IsColumnVisible("Identifier") ? IdentifierFilterColumn.FilterText : null,
                ProductsDataGrid.IsColumnVisible("Amount") ? AmountFilterColumn.FilterValue : null,
                ProductsDataGrid.IsColumnVisible("Amount") ? AmountFilterColumn.SelectedOperator : null,
                ProductsDataGrid.IsColumnVisible("TotalSoldAmount") ? TotalSoldAmountFilterColumn.FilterValue : null,
                ProductsDataGrid.IsColumnVisible("TotalSoldAmount") ? TotalSoldAmountFilterColumn.SelectedOperator : null,
                ProductsDataGrid.IsColumnVisible("Description") ? DescriptionFilterColumn.FilterText : null));
        return true;
    }
    
    private async void DataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (DataContext is not ProductsViewModel viewModel ||
            sender is not DataGrid dataGrid ||
            !Enum.TryParse(
                e.Column.SortMemberPath,
                ignoreCase: true,
                out ProductSortBy sortBy))
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
    
        viewModel.SetProductsSorting(sortBy, isDescending);
        await ProductsPagination.RefreshAsync();
    }
}
