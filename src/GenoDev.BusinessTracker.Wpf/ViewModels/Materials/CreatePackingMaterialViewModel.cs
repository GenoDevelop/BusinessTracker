using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Update;
using MediatR;
using System;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreatePackingMaterialViewModel(IMediator mediator) : ViewModelBase
{
    private Guid? _id;

    [ObservableProperty]
    private string _title = "Dodaj pakunek";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _ean;

    [ObservableProperty]
    private string? _manufacturerCode;

    [ObservableProperty]
    private string? _unit;

    [ObservableProperty]
    private string? _description;

    public event Action? RequestClose;

    public void InitializeForEdit(PackingMaterialDto dto)
    {
        _id = dto.Id;
        Title = "Edytuj pakunek";
        Name = dto.Name;
        Ean = dto.Ean;
        ManufacturerCode = dto.ManufacturerCode;
        Unit = dto.Unit;
        Description = dto.Description;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        IsBusy = true;
        try
        {
            if (_id.HasValue)
            {
                var command = new UpdatePackingMaterialCommand(
                    _id.Value,
                    Name,
                    Ean,
                    ManufacturerCode,
                    Unit,
                    Description);

                await mediator.Send(command);
            }
            else
            {
                var command = new CreatePackingMaterialCommand(
                    Name,
                    Ean,
                    ManufacturerCode,
                    Unit,
                    Description);

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
