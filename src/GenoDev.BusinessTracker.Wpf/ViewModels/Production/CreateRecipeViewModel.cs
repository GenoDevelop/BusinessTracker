using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.CreateRecipe;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using MediatR;
using System.Collections.ObjectModel;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class CreateRecipeViewModel(IMediator mediator) : ViewModelBase
{
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
        Name = string.Empty;
        Description = null;
        SelectedProduct = null;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (SelectedProduct == null) return;

        IsBusy = true;
        try
        {
            var command = new CreateRecipeCommand(SelectedProduct.Id, Name, Description);
            await mediator.Send(command);
            
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
