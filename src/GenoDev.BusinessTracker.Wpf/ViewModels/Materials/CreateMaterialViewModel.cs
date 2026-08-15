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
        Description = material.Description;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    public event Action<EditorCloseResult>? RequestClose;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        ClearValidationErrors();
        IsBusy = true;
        try
        {
            Guid? createdMaterialId = null;
            if (_editingMaterialId.HasValue)
            {
                var command = new UpdateMaterialCommand(_editingMaterialId.Value, Name, Description);
                await mediator.Send(command);
            }
            else
            {
                var command = new CreateMaterialCommand(Name, Description);
                createdMaterialId = await mediator.Send(command);
            }
            RequestClose?.Invoke(EditorCloseResult.Saved(createdMaterialId));
        }
        catch (ApplicationLogic.Exceptions.RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
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
