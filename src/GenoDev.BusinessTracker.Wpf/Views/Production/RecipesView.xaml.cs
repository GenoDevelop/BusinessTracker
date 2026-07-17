using System;
using System.ComponentModel;
using System.Windows.Controls;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Wpf.ViewModels.Production;

namespace GenoDev.BusinessTracker.Wpf.Views.Production;

public partial class RecipesView : UserControl
{
    public RecipesView()
    {
        InitializeComponent();
    }

    private void MaterialsDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is RecipesViewModel viewModel)
        {
            var propertyName = e.Column.SortMemberPath;
            if (Enum.TryParse<RecipeMaterialSortBy>(propertyName, out var sortBy))
            {
                e.Handled = true;

                var currentSortBy = viewModel.MaterialSortBy;
                var currentIsDescending = viewModel.IsMaterialDescending;

                if (currentSortBy == sortBy)
                {
                    viewModel.IsMaterialDescending = !currentIsDescending;
                }
                else
                {
                    viewModel.MaterialSortBy = sortBy;
                    viewModel.IsMaterialDescending = false;
                }

                e.Column.SortDirection = viewModel.IsMaterialDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }
        }
    }
}
