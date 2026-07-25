using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplies;
using GenoDev.BusinessTracker.Wpf.ViewModels.Materials;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using GenoDev.BusinessTracker.Wpf.Filtering;

namespace GenoDev.BusinessTracker.Wpf.Views.Materials;

public partial class MaterialSuppliesView : UserControl
{
    private MaterialSuppliesViewModel? _attachedViewModel;

    public MaterialSuppliesView()
    {
        InitializeComponent();

        Loaded += MaterialSuppliesView_Loaded;
        Unloaded += MaterialSuppliesView_Unloaded;
        DataContextChanged += MaterialSuppliesView_DataContextChanged;
    }

    private void MaterialSuppliesView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as MaterialSuppliesViewModel);
    }

    private void MaterialSuppliesView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void MaterialSuppliesView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as MaterialSuppliesViewModel);
        }
    }

    private void AttachViewModel(MaterialSuppliesViewModel? viewModel)
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

        if (_attachedViewModel is null)
        {
            return;
        }

        _attachedViewModel.PaginationRefreshRequested +=
            ViewModel_PaginationRefreshRequested;

        ConfigureSuppliesView(_attachedViewModel);
    }

    private static void ConfigureSuppliesView(
        MaterialSuppliesViewModel viewModel)
    {
        var view = CollectionViewSource.GetDefaultView(viewModel.Supplies);

        using (view.DeferRefresh())
        {
            view.GroupDescriptions.Clear();
            view.GroupDescriptions.Add(
                new PropertyGroupDescription(
                    nameof(MaterialSupplyDto.OrderDate),
                    new DateToDateOnlyConverter()));

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(
                new SortDescription(
                    nameof(MaterialSupplyDto.OrderDate),
                    ListSortDirection.Descending));
        }
    }

    private async void ViewModel_PaginationRefreshRequested(
        MaterialSuppliesPaginationTarget target,
        bool resetPageIndex)
    {
        var pagination = target == MaterialSuppliesPaginationTarget.Supplies
            ? SuppliesPagination
            : SupplyItemsPagination;

        if (resetPageIndex)
        {
            await pagination.ResetAndRefreshAsync();
            return;
        }

        await pagination.RefreshAsync();
    }

    private async void SuppliesRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SuppliesPagination.RefreshAsync();
    }

    private async void SuppliesList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        await SupplyItemsPagination.ResetAndRefreshAsync();
    }

    private async void SupplyItemsRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SupplyItemsPagination.RefreshAsync();
    }

    private async void SupplyItemsFilterToggleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await SupplyItemsPagination.ResetAndRefreshAsync();
    }

    private async void SupplyItemsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MaterialSuppliesViewModel viewModel)
        {
            return;
        }

        viewModel.SetSupplyItemsFilter(
            new MaterialSupplyItemsFilterCriteria(
                MaterialNameFilterColumn.FilterText,
                EanFilterColumn.FilterText,
                SetsAmountFilterColumn.FilterValue,
                SetsAmountFilterColumn.SelectedOperator,
                UnitsInSetFilterColumn.FilterValue,
                UnitsInSetFilterColumn.SelectedOperator,
                TotalAmountFilterColumn.FilterValue,
                TotalAmountFilterColumn.SelectedOperator,
                SetNetPriceFilterColumn.FilterValue,
                SetNetPriceFilterColumn.SelectedOperator,
                TotalNetPriceFilterColumn.FilterValue,
                TotalNetPriceFilterColumn.SelectedOperator,
                SetGrossPriceFilterColumn.FilterValue,
                SetGrossPriceFilterColumn.SelectedOperator,
                TotalGrossPriceFilterColumn.FilterValue,
                TotalGrossPriceFilterColumn.SelectedOperator));

        await SupplyItemsPagination.ResetAndRefreshAsync();
    }

    private async void SupplyItemsDataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (DataContext is not MaterialSuppliesViewModel viewModel ||
            sender is not DataGrid dataGrid ||
            string.IsNullOrWhiteSpace(e.Column.SortMemberPath))
        {
            return;
        }

        e.Handled = true;

        var sortColumn = e.Column.SortMemberPath;
        var isDescending = viewModel.SupplyItemsSortColumn == sortColumn &&
                           !viewModel.IsSupplyItemsDescending;

        foreach (var column in dataGrid.Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = isDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        viewModel.SetSupplyItemsSorting(sortColumn, isDescending);
        await SupplyItemsPagination.ResetAndRefreshAsync();
    }
}

internal sealed class DateToDateOnlyConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is DateTime dateTime
            ? dateTime.Date
            : value;
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return Binding.DoNothing;
    }
}