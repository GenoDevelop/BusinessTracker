using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateVariant;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateVariant;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;
using MediatR;
using System;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreateMaterialVariantViewModel(IMediator mediator) : ViewModelBase
{
    private Guid _materialId;
    private Guid? _variantId;

    public void Initialize(Guid materialId)
    {
        _materialId = materialId;
        _variantId = null;
    }

    public void InitializeForEdit(MaterialVariantDto variant)
    {
        _materialId = variant.MaterialId;
        _variantId = variant.Id;
        Name = variant.Name;
        Ean = variant.Ean;
        ManufacturerCode = variant.ManufacturerCode;
        Unit = variant.Unit;
        Description = variant.Description;
    }

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

    public event Action<EditorCloseResult>? RequestClose;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        ClearValidationErrors();
        IsBusy = true;
        try
        {
            Guid? createdVariantId = null;
            if (_variantId.HasValue)
            {
                var command = new UpdateMaterialVariantCommand(
                    _variantId.Value,
                    Name,
                    Ean,
                    ManufacturerCode,
                    Unit,
                    Description);

                await mediator.Send(command);
            }
            else
            {
                var command = new CreateMaterialVariantCommand(
                    _materialId,
                    Name,
                    Ean,
                    ManufacturerCode,
                    Unit,
                    Description);

                createdVariantId = await mediator.Send(command);
            }
            RequestClose?.Invoke(EditorCloseResult.Saved(createdVariantId));
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
