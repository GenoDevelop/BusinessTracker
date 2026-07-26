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
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    public event Action? RequestClose;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        IsBusy = true;
        try
        {
            if (_editingMaterialId.HasValue)
            {
                var command = new UpdateMaterialCommand(_editingMaterialId.Value, Name);
                await mediator.Send(command);
            }
            else
            {
                var command = new CreateMaterialCommand(Name);
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
