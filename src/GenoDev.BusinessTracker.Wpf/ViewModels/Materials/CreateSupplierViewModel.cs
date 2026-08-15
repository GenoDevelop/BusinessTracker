using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using MediatR;
using System;
using System.Threading.Tasks;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreateSupplierViewModel : ViewModelBase
{
    private Guid? _editingSupplierId;
    
    public void InitializeForEdit(SupplierDto supplier)
    {
        _editingSupplierId = supplier.Id;
        Name = supplier.Name;
        Nip = supplier.Nip;
        Description = supplier.Description;
        WebsiteUrl = supplier.WebsiteUrl;
    }
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private string? _nip;
    
    [ObservableProperty]
    private string? _description;
    
    [ObservableProperty]
    private string? _websiteUrl;

    private readonly IMediator _mediator;

    /// <inheritdoc/>
    public CreateSupplierViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public event Action<EditorCloseResult>? RequestClose;
    
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        IsBusy = true;
        try
        {
            Guid? createdSupplierId = null;
            if (_editingSupplierId.HasValue)
            {
                var command = new UpdateSupplierCommand(_editingSupplierId.Value, Name, Nip, Description, WebsiteUrl);
                await _mediator.Send(command);
            }
            else
            {
                var command = new CreateSupplierCommand(Name, Nip, Description, WebsiteUrl);
                createdSupplierId = await _mediator.Send(command);
            }
            RequestClose?.Invoke(EditorCloseResult.Saved(createdSupplierId));
        }
        finally
        {
            IsBusy = false;
        }
    }
    
    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);
    
    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke(EditorCloseResult.Cancelled);
    }
}
