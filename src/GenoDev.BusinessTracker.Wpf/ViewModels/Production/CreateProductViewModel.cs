using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProducts;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Update;
using MediatR;
using System;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class CreateProductViewModel(IMediator mediator) : ViewModelBase
{
    private Guid? _editingProductId;

    [ObservableProperty]
    private string _title = "Dodaj produkt";

    public void InitializeForEdit(ProductDto product)
    {
        _editingProductId = product.Id;
        Title = "Edytuj produkt";
        Name = product.Name;
        Identifier = product.Identifier;
        Description = product.Description;
    }

    public void Clear()
    {
        _editingProductId = null;
        Title = "Dodaj produkt";
        Name = string.Empty;
        Identifier = string.Empty;
        Description = null;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _identifier = string.Empty;

    [ObservableProperty]
    private string? _description;

    public event Action<EditorCloseResult>? RequestClose;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        IsBusy = true;
        try
        {
            Guid? createdProductId = null;
            if (_editingProductId.HasValue)
            {
                var command = new UpdateProductCommand(_editingProductId.Value, Name, Identifier, Description);
                await mediator.Send(command);
            }
            else
            {
                var command = new CreateProductCommand(Name, Identifier, Description);
                createdProductId = await mediator.Send(command);
            }
            
            Clear();
            
            RequestClose?.Invoke(EditorCloseResult.Saved(createdProductId));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Identifier);

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(EditorCloseResult.Cancelled);
    }
}
