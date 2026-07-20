using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddRecipeMaterial;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipeMaterial;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class AddRecipeMaterialViewModel(IMediator mediator) : ViewModelBase
{
    private Guid? _recipeId;
    private Guid? _recipeMaterialId;

    [ObservableProperty]
    private string _title = "Dodaj materiał do przepisu";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private MaterialDto? _selectedMaterial;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private double _amount;

    [ObservableProperty]
    private string? _materialSearchTerm;

    public ObservableCollection<MaterialDto> Materials { get; } = new();

    public event Action? RequestClose;

    public async Task LoadMaterialsAsync()
    {
        IsBusy = true;
        try
        {
            var query = new GetMaterialsQuery(0, 100, NameFilter: MaterialSearchTerm);
            var result = await mediator.Send(query);
            
            Materials.Clear();
            foreach (var material in result.Items)
            {
                Materials.Add(material);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnMaterialSearchTermChanged(string? value)
    {
        _ = LoadMaterialsAsync();
    }

    public void InitializeForAdd(Guid recipeId)
    {
        _recipeId = recipeId;
        _recipeMaterialId = null;
        Title = "Dodaj materiał do przepisu";
        SelectedMaterial = null;
        Amount = 0;
        _ = LoadMaterialsAsync();
    }

    public void InitializeForEdit(Guid recipeId, RecipeMaterialDto material)
    {
        _recipeId = recipeId;
        _recipeMaterialId = material.Id;
        Title = "Edytuj materiał w przepisie";
        Amount = material.RequiredAmount;
        
        // We need to find the material in the list or load it
        _ = LoadMaterialsAndSelectAsync(material.MaterialId);
    }

    private async Task LoadMaterialsAndSelectAsync(Guid materialId)
    {
        await LoadMaterialsAsync();
        SelectedMaterial = Materials.FirstOrDefault(m => m.Id == materialId);
        
        // If not found (e.g. not in first 100), we might need to fetch it specifically, 
        // but for now let's assume it's there or user can search.
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task Save()
    {
        if (SelectedMaterial == null || !_recipeId.HasValue) return;

        IsBusy = true;
        try
        {
            if (_recipeMaterialId.HasValue)
            {
                var command = new UpdateRecipeMaterialCommand(_recipeMaterialId.Value, Amount);
                await mediator.Send(command);
            }
            else
            {
                var command = new AddRecipeMaterialCommand(_recipeId.Value, SelectedMaterial.Id, Amount);
                await mediator.Send(command);
            }
            
            RequestClose?.Invoke();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSave() => SelectedMaterial != null && Amount > 0;

    [RelayCommand]
    private void Cancel()
    {
        RequestClose?.Invoke();
    }
}
