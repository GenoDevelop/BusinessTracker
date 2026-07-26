using GenoDev.BusinessTracker.Wpf.ViewModels.Production;
using GenoDev.BusinessTracker.Wpf.Filtering;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Views.Production;

public partial class ProductionListView : UserControl
{
    // private ProductionListViewModel? _attachedViewModel;
    //
    // public ProductionListView()
    // {
    //     InitializeComponent();
    //
    //     Loaded += ProductionListView_Loaded;
    //     Unloaded += ProductionListView_Unloaded;
    //     DataContextChanged += ProductionListView_DataContextChanged;
    // }
    //
    // private void ProductionListView_Loaded(object sender, RoutedEventArgs e)
    // {
    //     AttachViewModel(DataContext as ProductionListViewModel);
    // }
    //
    // private void ProductionListView_Unloaded(object sender, RoutedEventArgs e)
    // {
    //     AttachViewModel(null);
    // }
    //
    // private void ProductionListView_DataContextChanged(
    //     object sender,
    //     DependencyPropertyChangedEventArgs e)
    // {
    //     if (IsLoaded)
    //     {
    //         AttachViewModel(e.NewValue as ProductionListViewModel);
    //     }
    // }
    //
    // private void AttachViewModel(ProductionListViewModel? viewModel)
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
    // private async void ViewModel_PaginationRefreshRequested(
    //     ProductionPaginationTarget target)
    // {
    //     var pagination = target == ProductionPaginationTarget.Productions
    //         ? ProductionsPagination
    //         : HistoryPagination;
    //
    //     await pagination.RefreshAsync();
    // }
    //
    // private async void ProductionsRefreshButton_Click(
    //     object sender,
    //     RoutedEventArgs e)
    // {
    //     await ProductionsPagination.RefreshAsync();
    // }
    //
    // private async void HistoryRefreshButton_Click(
    //     object sender,
    //     RoutedEventArgs e)
    // {
    //     await HistoryPagination.RefreshAsync();
    // }
    //
    // private async void SearchTerm_SourceUpdated(
    //     object sender,
    //     DataTransferEventArgs e)
    // {
    //     await ProductionsPagination.RefreshAsync();
    // }
    //
    // private async void RecipeSearchTerm_SourceUpdated(
    //     object sender,
    //     DataTransferEventArgs e)
    // {
    //     if (DataContext is ProductionListViewModel viewModel)
    //     {
    //         await viewModel.RefreshProductRecipesAsync();
    //     }
    // }
    //
    // private async void ProductsList_SelectionChanged(
    //     object sender,
    //     SelectionChangedEventArgs e)
    // {
    //     await HistoryPagination.ResetAndRefreshAsync();
    // }
    //
    // private async void HistoryFilterToggleButton_Click(
    //     object sender,
    //     RoutedEventArgs e)
    // {
    //     await HistoryPagination.RefreshAsync();
    // }
    //
    // private async void HistoryFilter_FilterChanged(
    //     object sender,
    //     RoutedEventArgs e)
    // {
    //     if (DataContext is not ProductionListViewModel viewModel)
    //     {
    //         return;
    //     }
    //
    //     viewModel.SetHistoryFilter(
    //         new ProductionHistoryFilterCriteria(
    //             Description: HistoryDescriptionFilterColumn.FilterText,
    //             Amount: HistoryAmountFilterColumn.FilterValue,
    //             AmountOperator: HistoryAmountFilterColumn.SelectedOperator,
    //             FromDate: HistoryDateFilterColumn.StartDate,
    //             ToDate: HistoryDateFilterColumn.EndDate));
    //
    //     await HistoryPagination.RefreshAsync();
    // }
}