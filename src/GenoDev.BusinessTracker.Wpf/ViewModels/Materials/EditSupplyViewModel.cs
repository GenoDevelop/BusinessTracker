using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class EditSupplyViewModel(IMediator mediator, SupplyDetailsDto details) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private SupplierDto? _selectedSupplier;

    [ObservableProperty]
    private DateTime _orderDate = details.OrderDate;

    [ObservableProperty]
    private MaterialSupplyStatus _status = details.Status;

    [ObservableProperty]
    private string? _invoiceNo = details.InvoiceNo;

    [ObservableProperty]
    private string? _description = details.Description;

    [ObservableProperty]
    private decimal? _shippingNetPrice = details.ShippingNetPrice;

    [ObservableProperty]
    private decimal? _shippingGrossPrice = details.ShippingGrossPrice;

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();
    public ObservableCollection<MaterialSupplyStatus> AvailableStatuses { get; } = new(Enum.GetValues<MaterialSupplyStatus>());

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var result = await mediator.Send(new GetSuppliersQuery(0, 1000, SortBy: SupplierSortBy.Name));
            Suppliers.Clear();
            foreach (var supplier in result.Items)
            {
                Suppliers.Add(supplier);
            }
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == details.SupplierId);
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
            await mediator.Send(new UpdateSupplyCommand(
                details.Id,
                SelectedSupplier.Id,
                OrderDate,
                Status,
                Description,
                InvoiceNo,
                ShippingNetPrice ?? 0,
                ShippingGrossPrice ?? 0));
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