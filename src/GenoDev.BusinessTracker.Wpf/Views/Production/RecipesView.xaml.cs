using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GenoDev.BusinessTracker.Wpf.Views.Production;

public partial class RecipesView : UserControl
{
    private RecipesViewModel? _attachedViewModel;

    public RecipesView()
    {
        InitializeComponent();

        Loaded += RecipesView_Loaded;
        Unloaded += RecipesView_Unloaded;
        DataContextChanged += RecipesView_DataContextChanged;
    }

    private void RecipesView_Loaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(DataContext as RecipesViewModel);
    }

    private void RecipesView_Unloaded(object sender, RoutedEventArgs e)
    {
        AttachViewModel(null);
    }

    private void RecipesView_DataContextChanged(
        object sender,
        DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            AttachViewModel(e.NewValue as RecipesViewModel);
        }
    }

    private void AttachViewModel(RecipesViewModel? viewModel)
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
        RecipesPaginationTarget target)
    {
        var pagination = target == RecipesPaginationTarget.Recipes
            ? RecipesPagination
            : RecipeMaterialsPagination;

        await pagination.RefreshAsync();
    }

    private async void RecipesRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RecipesPagination.RefreshAsync();
    }

    private async void RecipeMaterialsRefreshButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RecipeMaterialsPagination.RefreshAsync();
    }

    private async void SearchTerm_SourceUpdated(
        object sender,
        DataTransferEventArgs e)
    {
        await RecipesPagination.RefreshAsync();
    }

    private async void RecipesList_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (DataContext is RecipesViewModel { IsRestoringRecipesSelection: true })
        {
            return;
        }

        // Materials belong to a different recipe context, so the previous page is invalid.
        await RecipeMaterialsPagination.ResetAndRefreshAsync();
    }

    private async void RecipeMaterialsFilterToggleButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        await RecipeMaterialsPagination.RefreshAsync();
    }

    private async void RecipeMaterialsFilter_FilterChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is not RecipesViewModel viewModel)
        {
            return;
        }

        viewModel.SetRecipeMaterialsFilter(
            new RecipeMaterialsFilterCriteria(
                MaterialNameFilterColumn.FilterText,
                DescriptionFilterColumn.FilterText));

        await RecipeMaterialsPagination.RefreshAsync();
    }

    private async void MaterialsDataGrid_Sorting(
        object sender,
        DataGridSortingEventArgs e)
    {
        if (DataContext is not RecipesViewModel viewModel ||
            sender is not DataGrid dataGrid ||
            !Enum.TryParse(
                e.Column.SortMemberPath,
                ignoreCase: true,
                out RecipeMaterialSortBy sortBy))
        {
            return;
        }

        e.Handled = true;

        var isDescending = viewModel.MaterialSortBy == sortBy &&
                           !viewModel.IsMaterialDescending;

        foreach (var column in dataGrid.Columns)
        {
            column.SortDirection = null;
        }

        e.Column.SortDirection = isDescending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        viewModel.SetRecipeMaterialsSorting(sortBy, isDescending);
        await RecipeMaterialsPagination.RefreshAsync();
    }
}
