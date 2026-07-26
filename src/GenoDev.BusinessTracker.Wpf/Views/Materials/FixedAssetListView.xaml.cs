using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class FixedAssetListView : UserControl
{
    private FixedAssetListViewModel? _attachedViewModel;

    public FixedAssetListView()
    {
        InitializeComponent();

        Loaded += FixedAssetListView_Loaded;
        Unloaded += FixedAssetListView_Unloaded;
        DataContextChanged += FixedAssetListView_DataContextChanged;
    }

    private void FixedAssetListView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as FixedAssetListViewModel);
    }

    private void FixedAssetListView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void FixedAssetListView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as FixedAssetListViewModel);
        }
    }

    private void AttachViewModel(FixedAssetListViewModel? viewModel)
    {
        if (ReferenceEquals(_attachedViewModel, viewModel))
        {
            return;
        }

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested -= ViewModel_PaginationRefreshRequested;
        }

        _attachedViewModel = viewModel;

        if (_attachedViewModel is not null)
        {
            _attachedViewModel.PaginationRefreshRequested += ViewModel_PaginationRefreshRequested;
        }
    }

    private async void ViewModel_PaginationRefreshRequested()
    {
        await Pagination.RefreshAsync();
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await Pagination.RefreshAsync();
    }

    private async void FilterToggleButton_Click(object sender, RoutedEventArgs e)
    {
        await Pagination.RefreshAsync();
    }

    private async void Filter_FilterChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FixedAssetListViewModel viewModel)
        {
            return;
        }

        viewModel.SetFilter(new FixedAssetFilterCriteria(
            NameFilterColumn.FilterText,
            EanFilterColumn.FilterText,
            ManufacturerCodeFilterColumn.FilterText,
            DescriptionFilterColumn.FilterText,
            AmountFilterColumn.SelectedOperator,
            AmountFilterColumn.FilterValue));

        await Pagination.RefreshAsync();
    }

    private async void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not FixedAssetListViewModel viewModel)
        {
            return;
        }

        e.Handled = true;

        var sortBy = e.Column.SortMemberPath switch
        {
            "Name" => FixedAssetSortBy.Name,
            "Ean" => FixedAssetSortBy.Ean,
            "ManufacturerCode" => FixedAssetSortBy.ManufacturerCode,
            "Amount" => FixedAssetSortBy.Amount,
            "Description" => FixedAssetSortBy.Description,
            _ => FixedAssetSortBy.Name
        };

        var direction = e.Column.SortDirection == ListSortDirection.Ascending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        viewModel.SetSorting(sortBy, direction == ListSortDirection.Descending);
        e.Column.SortDirection = direction;

        await Pagination.RefreshAsync();
    }
}
