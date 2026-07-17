using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.CreateRecipe;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipe;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;
using MediatR;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class CreateRecipeViewModel(IMediator mediator) : ViewModelBase
{
    private Guid? _editingRecipeId;

    [ObservableProperty]
    private string _title = "Dodaj przepis";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private ProductDto? _selectedProduct;

    public ObservableCollection<ProductDto> Products { get; } = new();

    public event Action? RequestClose;

    public async Task LoadProductsAsync()
    {
        IsBusy = true;
        try
        {
            var query = new GetProductsQuery(0, 1000); // Load all products for selection
            var result = await mediator.Send(query);
            
            Products.Clear();
            foreach (var product in result.Items)
            {
                Products.Add(product);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Clear()
    {
        _editingRecipeId = null;
        Title = "Dodaj przepis";
        Name = string.Empty;
        Description = null;
        SelectedProduct = null;
    }

    public void LoadRecipe(RecipeDto recipe)
    {
        _editingRecipeId = recipe.Id;
        Title = "Edytuj przepis";
        Name = recipe.Name;
        Description = recipe.Description;
        SelectedProduct = Products.FirstOrDefault(p => p.Id == recipe.ProductId);
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (SelectedProduct == null) return;

        IsBusy = true;
        try
        {
            if (_editingRecipeId.HasValue)
            {
                var command = new UpdateRecipeCommand(_editingRecipeId.Value, SelectedProduct.Id, Name, Description);
                await mediator.Send(command);
            }
            else
            {
                var command = new CreateRecipeCommand(SelectedProduct.Id, Name, Description);
                await mediator.Send(command);
            }
            
            Clear();
            RequestClose?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && SelectedProduct != null;

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
