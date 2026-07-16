using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateSupply;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class EditMaterialSupplyViewModel(IMediator mediator, MaterialSupplyDetailsDto details) : ViewModelBase
{
    private readonly Guid _supplyId = details.Id;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private SupplierDto? _selectedSupplier;

    [ObservableProperty]
    private DateTime _orderDate = details.OrderDate;

    [ObservableProperty]
    private MaterialSupplyStatus _status = details.Status;

    [ObservableProperty]
    private string? _description = details.Description;

    [ObservableProperty]
    private string? _invoiceNo = details.InvoiceNo;

    public ObservableCollection<SupplierDto> Suppliers { get; } = new();

    public List<MaterialSupplyStatus> AvailableStatuses { get; } = Enum.GetValues<MaterialSupplyStatus>().ToList();

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

            SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == details.SupplierId);
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
            var command = new UpdateMaterialSupplyCommand(
                _supplyId,
                SelectedSupplier.Id,
                OrderDate,
                Status,
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
