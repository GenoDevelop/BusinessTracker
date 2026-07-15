using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Update;
using MediatR;
using System;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreateMaterialViewModel(IMediator mediator) : ViewModelBase
{
    private Guid? _editingMaterialId;

    public void InitializeForEdit(MaterialDto material)
    {
        _editingMaterialId = material.Id;
        Name = material.Name;
        Ean = material.Ean;
        Description = material.Description;
        Unit = material.Unit;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _ean;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string? _unit;

    public event Action? RequestClose;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        IsBusy = true;
        try
        {
            if (_editingMaterialId.HasValue)
            {
                var command = new UpdateMaterialCommand(_editingMaterialId.Value, Name, Ean, Description, Unit);
                await mediator.Send(command);
            }
            else
            {
                var command = new CreateMaterialCommand(Name, Ean, Description, Unit);
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
