using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Update;
using MediatR;
using System;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class CreateFixedAssetViewModel(IMediator mediator) : ViewModelBase
{
    private Guid? _id;

    [ObservableProperty]
    private string _title = "Dodaj środek trwały";

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

    public void InitializeForEdit(FixedAssetDto dto)
    {
        _id = dto.Id;
        Title = "Edytuj środek trwały";
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
            Guid? createdAssetId = null;
            if (_id.HasValue)
            {
                var command = new UpdateFixedAssetCommand(
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
                var command = new CreateFixedAssetCommand(
                    Name,
                    Ean,
                    ManufacturerCode,
                    Unit,
                    Description);

                createdAssetId = await mediator.Send(command);
            }
            RequestClose?.Invoke(EditorCloseResult.Saved(createdAssetId));
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
