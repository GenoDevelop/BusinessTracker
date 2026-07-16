using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateSupply;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreateMaterialSupplyViewModel(IMediator mediator) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private SupplierDto? _selectedSupplier;

    [ObservableProperty]
    private DateTime _orderDate = DateTime.Now;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string? _invoiceNo;

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var result = await mediator.Send(new GetSuppliersQuery(0, 1000, SortBy: SupplierSortBy.Name, IsDescending: false));
            Suppliers.Clear();
            foreach (var supplier in result.Items)
            {
                Suppliers.Add(supplier);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (SelectedSupplier == null) return;

        IsBusy = true;
        try
        {
            var command = new CreateMaterialSupplyCommand(
                SelectedSupplier.Id,
                OrderDate,
                Description,
                InvoiceNo);

            await mediator.Send(command);
            RequestClose?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => SelectedSupplier != null;

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
