using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Wpf.Filtering;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class SuppliersView : UserControl
{
    // private SuppliersViewModel? _attachedViewModel;
    //
    // public SuppliersView()
    // {
    //     InitializeComponent();
    //
    //     Loaded += SuppliersView_Loaded;
    //     Unloaded += SuppliersView_Unloaded;
    //     DataContextChanged += SuppliersView_DataContextChanged;
    // }
    //
    // private void SuppliersView_Loaded(object sender, RoutedEventArgs e)
    // {
    //     AttachViewModel(DataContext as SuppliersViewModel);
    // }
    //
    // private void SuppliersView_Unloaded(object sender, RoutedEventArgs e)
    // {
    //     AttachViewModel(null);
    // }
    //
    // private void SuppliersView_DataContextChanged(
    //     object sender,
    //     DependencyPropertyChangedEventArgs e)
    // {
    //     if (IsLoaded)
    //     {
    //         AttachViewModel(e.NewValue as SuppliersViewModel);
    //     }
    // }
    //
    // private void AttachViewModel(SuppliersViewModel? viewModel)
    // {
    //     if (ReferenceEquals(_attachedViewModel, viewModel))
    //     {
    //         return;
    //     }
    //
    //     if (_attachedViewModel is not null)
    //     {
    //         _attachedViewModel.PaginationRefreshRequested -=
    //             ViewModel_PaginationRefreshRequested;
    //     }
    //
    //     _attachedViewModel = viewModel;
    //
    //     if (_attachedViewModel is not null)
    //     {
    //         _attachedViewModel.PaginationRefreshRequested +=
    //             ViewModel_PaginationRefreshRequested;
    //     }
    // }
    //
    // private async void ViewModel_PaginationRefreshRequested()
    // {
    //     await SuppliersPagination.RefreshAsync();
    // }
    //
    // private async void RefreshButton_Click(
    //     object sender,
    //     RoutedEventArgs e)
    // {
    //     await SuppliersPagination.RefreshAsync();
    // }
    //
    // private async void FilterToggleButton_Click(
    //     object sender,
    //     RoutedEventArgs e)
    // {
    //     await SuppliersPagination.ResetAndRefreshAsync();
    // }
    //
    // private async void SupplierFilter_FilterChanged(
    //     object sender,
    //     RoutedEventArgs e)
    // {
    //     if (DataContext is not SuppliersViewModel viewModel)
    //     {
    //         return;
    //     }
    //
    //     viewModel.SetSuppliersFilter(
    //         new SuppliersFilterCriteria(
    //             NameFilterColumn.FilterText,
    //             NipFilterColumn.FilterText,
    //             DescriptionFilterColumn.FilterText));
    //
    //     await SuppliersPagination.ResetAndRefreshAsync();
    // }
    //
    // private async void SuppliersDataGrid_Sorting(
    //     object sender,
    //     DataGridSortingEventArgs e)
    // {
    //     if (DataContext is not SuppliersViewModel viewModel ||
    //         sender is not DataGrid dataGrid ||
    //         !Enum.TryParse(
    //             e.Column.SortMemberPath,
    //             ignoreCase: true,
    //             out SupplierSortBy sortBy))
    //     {
    //         return;
    //     }
    //
    //     e.Handled = true;
    //
    //     var isDescending = viewModel.SortBy == sortBy &&
    //                        !viewModel.IsDescending;
    //
    //     foreach (var column in dataGrid.Columns)
    //     {
    //         column.SortDirection = null;
    //     }
    //
    //     e.Column.SortDirection = isDescending
    //         ? ListSortDirection.Descending
    //         : ListSortDirection.Ascending;
    //
    //     viewModel.SetSorting(sortBy, isDescending);
    //     await SuppliersPagination.RefreshAsync();
    // }
}