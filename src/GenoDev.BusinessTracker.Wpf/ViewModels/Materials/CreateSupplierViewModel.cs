using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using MediatR;
using System;
using System.Threading.Tasks;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Update;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreateSupplierViewModel(IMediator mediator) : ViewModelBase
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

    public event Action? RequestClose;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        IsBusy = true;
        try
        {
            if (_editingSupplierId.HasValue)
            {
                var command = new UpdateSupplierCommand(_editingSupplierId.Value, Name, Nip, Description, WebsiteUrl);
                await mediator.Send(command);
            }
            else
            {
                var command = new CreateSupplierCommand(Name, Nip, Description, WebsiteUrl);
                await mediator.Send(command);
            }
            RequestClose?.Invoke();
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
        RequestClose?.Invoke();
    }
}
