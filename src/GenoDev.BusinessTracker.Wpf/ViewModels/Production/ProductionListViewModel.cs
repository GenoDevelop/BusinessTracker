using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.Wpf.Filtering;
using GenoDev.BusinessTracker.ApplicationLogic;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetVariants;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionHistory;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionSummary;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialsForProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetMaterialVariantsForProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;
using MediatR;
using System.Collections.ObjectModel;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Wpf.Controls;
using GenoDev.BusinessTracker.Wpf.ViewModels.Products;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public enum ProductionPaginationTarget
{
    Productions,
    History
}

public partial class ProductionListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private Guid? _editingProductionId;
    private CancellationTokenSource? _productRecipesLoadCancellation;
    private CancellationTokenSource? _materialsLoadCancellation;
    private bool _isRestoringProductsSelection;
    private bool _isRestoringHistorySelection;
    private bool _isRestoringProductRecipesSelection;
    private Guid? _pendingCreatedProductionId;

    private ProductionHistoryFilterCriteria _historyFilter =
        ProductionHistoryFilterCriteria.Empty;

    public ProductionListViewModel(
        IMediator mediator,
        ProductImagesViewModel productImagesViewModel)
    {
        _mediator = mediator;
        ProductImages = productImagesViewModel;

        AddProductionCommand = new AsyncRelayCommand(AddProductionAsync, CanAddProduction);
        EditProductionCommand = new AsyncRelayCommand<ProductionHistoryDto>(
            EditProductionAsync,
            CanEditProduction);
        DeleteProductionCommand = new RelayCommand<ProductionHistoryDto>(
            DeleteProduction,
            CanDeleteProduction);
        SaveProductionCommand = new AsyncRelayCommand(SaveProductionAsync, CanSaveProduction);
        CancelAddProductionCommand = new RelayCommand(CancelAddProduction);
        ConfirmDeleteProductionCommand = new AsyncRelayCommand(ConfirmDeleteProductionAsync);
        CancelDeleteProductionCommand = new RelayCommand(CancelDeleteProduction);

        MaterialInputs.CollectionChanged += (s, e) => RefreshMaterialInputsWithAdd();
        RefreshMaterialInputsWithAdd();
    }

    private void RefreshMaterialInputsWithAdd()
    {
        MaterialInputsWithAdd.Clear();
        foreach (var input in MaterialInputs)
        {
            MaterialInputsWithAdd.Add(input);
        }
        MaterialInputsWithAdd.Add(new AddMaterialButtonViewModel(this));
    }

    public ObservableCollection<ProductionSummaryDto> Products { get; } = new();
    public ObservableCollection<RecipeDto> ProductRecipes { get; } = new();
    public ObservableCollection<ProductionHistoryDto> ProductionHistory { get; } = new();
    public ObservableCollection<object> SelectedMaterials { get; } = new();
    public ObservableCollection<DynamicMaterialInput> MaterialInputs { get; } = new();
    public ObservableCollection<object> MaterialInputsWithAdd { get; } = new();

    /// <summary>
    /// Loadery przekazywane bezpośrednio do kontrolek paginacji.
    /// Kontrolka dostarcza stan strony i sama przejmuje zwrócony TotalCount.
    /// </summary>
    public PaginationPageLoader ProductionsPageLoader => LoadProductsPageAsync;
    public PaginationPageLoader HistoryPageLoader => LoadHistoryPageAsync;

    /// <summary>
    /// Lekki, niezależny od WPF sygnał używany po operacjach CRUD,
    /// kiedy kontrolka paginacji powinna ponownie pobrać aktualną stronę.
    /// </summary>
    public event Action<ProductionPaginationTarget>? PaginationRefreshRequested;

    public bool IsRestoringProductsSelection => _isRestoringProductsSelection;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private bool _isHistoryFilterVisible;

    [ObservableProperty]
    private ProductionSummaryDto? _selectedProduct;

    [ObservableProperty]
    private RecipeDto? _selectedRecipe;

    [ObservableProperty]
    private ProductionHistoryDto? _selectedProduction;

    [ObservableProperty]
    private bool _showRecipeDetails;

    [ObservableProperty]
    private bool _isAddingProduction;

    [ObservableProperty]
    private bool _isEditingProduction;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private ProductionHistoryDto? _productionToDelete;

    [ObservableProperty]
    private int _productionAmount = 1;

    [ObservableProperty]
    private string? _productionDescription;

    [ObservableProperty]
    private DateTime _productionDate = DateTime.Now;

    public IAsyncRelayCommand AddProductionCommand { get; }
    public IAsyncRelayCommand<ProductionHistoryDto> EditProductionCommand { get; }
    public IRelayCommand<ProductionHistoryDto> DeleteProductionCommand { get; }
    public IAsyncRelayCommand SaveProductionCommand { get; }
    public IRelayCommand CancelAddProductionCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteProductionCommand { get; }
    public IRelayCommand CancelDeleteProductionCommand { get; }

    public ObservableCollection<MaterialDto> AllMaterials { get; } = new();
    public ObservableCollection<MaterialVariantDto> NewMaterialVariants { get; } = new();

    [ObservableProperty]
    private bool _isAddingNewMaterial;

    [ObservableProperty]
    private MaterialDto? _selectedMaterialToAdd;

    [ObservableProperty]
    private MaterialVariantDto? _selectedVariantToAdd;

    [ObservableProperty]
    private double _newMaterialAmount = 1.0;

    [RelayCommand]
    private async Task OpenAddMaterialPopupAsync()
    {
        IsAddingNewMaterial = true;
        SelectedMaterialToAdd = null;
        SelectedVariantToAdd = null;
        NewMaterialAmount = 1.0;

        var usedVariantIds = MaterialInputs
            .Select(m => m.SelectedVariant?.Id)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var materials = await _mediator.Send(new GetMaterialsForProductionQuery(usedVariantIds));

        ReplaceItems(AllMaterials, materials);
    }

    [RelayCommand]
    private void CancelAddNewMaterial()
    {
        IsAddingNewMaterial = false;
    }

    [RelayCommand]
    private async Task ConfirmAddMaterialAsync()
    {
        if (SelectedMaterialToAdd == null || SelectedVariantToAdd == null) return;

        // Check if this variant is already used in ANY input
        if (MaterialInputs.Any(m => m.SelectedVariant?.Id == SelectedVariantToAdd.Id))
        {
            IsAddingNewMaterial = false;
            return;
        }

        var input = new DynamicMaterialInput(this, _mediator)
        {
            MaterialId = SelectedMaterialToAdd.Id,
            MaterialName = SelectedMaterialToAdd.Name,
            UsedAmount = NewMaterialAmount,
            DefaultUsedAmount = 0,
            Unit = SelectedVariantToAdd.Unit
        };

        // Fetch all variants for this material so the user can change it later if they want
        var variantsResult = await _mediator.Send(new GetMaterialVariantsQuery(SelectedMaterialToAdd.Id, 0, 1000));
        input.Variants.Clear();
        foreach (var v in variantsResult.Items) input.Variants.Add(v);
        input.SelectedVariant = input.Variants.FirstOrDefault(v => v.Id == SelectedVariantToAdd.Id);

        MaterialInputs.Add(input);
        input.UpdateRequiredAmount();
        IsAddingNewMaterial = false;
    }

    async partial void OnSelectedMaterialToAddChanged(MaterialDto? value)
    {
        NewMaterialVariants.Clear();
        SelectedVariantToAdd = null;
        if (value == null) return;

        var usedVariantIds = MaterialInputs
            .Select(m => m.SelectedVariant?.Id)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        var variants = await _mediator.Send(new GetMaterialVariantsForProductionQuery(value.Id, usedVariantIds));
        
        foreach (var v in variants)
        {
            NewMaterialVariants.Add(v);
        }
        
        SelectedVariantToAdd = NewMaterialVariants.FirstOrDefault();
    }

    public void SetHistoryFilter(ProductionHistoryFilterCriteria filter)
    {
        _historyFilter = filter;
    }

    public Task RefreshProductRecipesAsync()
    {
        return LoadProductRecipesAsync(SelectedProduct);
    }

    private async Task<int> LoadProductsPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        var selectedProduct = SelectedProduct;
        var result = await _mediator.Send(
            new GetProductionSummaryQuery(state.PageIndex, state.PageSize, SearchTerm),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        _isRestoringProductsSelection = true;
        try
        {
            SelectedProduct = ReplaceItemsPreservingSelection(
                Products,
                result.Items,
                selectedProduct,
                product => product.Id);
        }
        finally
        {
            _isRestoringProductsSelection = false;
        }

        if (selectedProduct is not null && SelectedProduct is null)
        {
            HandleSelectedProductChanged(null);
        }

        return result.TotalCount;
    }

    private async Task<int> LoadHistoryPageAsync(
        PaginationState state,
        CancellationToken cancellationToken)
    {
        if (SelectedProduct is null)
        {
            ProductionHistory.Clear();
            SelectedProduction = null;
            return 0;
        }

        var selectedProduction = SelectedProduction;
        var filter = _historyFilter;

        var result = await _mediator.Send(
            new GetProductionHistoryQuery(
                SelectedProduct.Id,
                state.PageIndex,
                state.PageSize,
                IsHistoryFilterVisible ? filter.Description : null,
                IsHistoryFilterVisible ? filter.AmountOperator : null,
                IsHistoryFilterVisible ? (int?)filter.Amount : null,
                IsHistoryFilterVisible ? filter.FromDate : null,
                IsHistoryFilterVisible ? filter.ToDate : null),
            cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        _isRestoringHistorySelection = true;
        try
        {
            SelectedProduction = ReplaceItemsPreservingSelection(
                ProductionHistory,
                result.Items,
                selectedProduction,
                production => production.Id,
                _pendingCreatedProductionId);
            _pendingCreatedProductionId = null;
        }
        finally
        {
            _isRestoringHistorySelection = false;
        }

        if (selectedProduction is not null && SelectedProduction is null)
        {
            _ = LoadMaterialsAsync();
        }

        return result.TotalCount;
    }

    partial void OnSelectedProductChanged(ProductionSummaryDto? value)
    {
        if (_isRestoringProductsSelection)
        {
            return;
        }

        HandleSelectedProductChanged(value);
    }

    public ProductImagesViewModel ProductImages { get; }

    private void HandleSelectedProductChanged(ProductionSummaryDto? value)
    {
        CancelAddProduction();
        ProductionHistory.Clear();
        SelectedProduction = null;
        SelectedMaterials.Clear();
        _ = ProductImages.SetProductAsync(value?.Id);
        _ = LoadProductRecipesAsync(value);
    }

    partial void OnSelectedRecipeChanged(RecipeDto? value)
    {
        if (_isRestoringProductRecipesSelection)
        {
            return;
        }

        HandleSelectedRecipeChanged(value);
    }

    private void HandleSelectedRecipeChanged(RecipeDto? value)
    {
        NotifyCommandStatesChanged();

        if (IsAddingProduction && value is not null)
        {
            _ = InitializeMaterialInputsAsync(value);
        }

        if (ShowRecipeDetails)
        {
            _ = LoadMaterialsAsync();
        }
    }

    partial void OnSelectedProductionChanged(ProductionHistoryDto? value)
    {
        if (_isRestoringHistorySelection)
        {
            return;
        }

        if (!ShowRecipeDetails)
        {
            _ = LoadMaterialsAsync();
        }
    }

    partial void OnShowRecipeDetailsChanged(bool value)
    {
        _ = LoadMaterialsAsync();
    }

    partial void OnIsAddingProductionChanged(bool value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnIsEditingProductionChanged(bool value)
    {
        NotifyCommandStatesChanged();
    }

    partial void OnProductionAmountChanged(int value)
    {
        foreach (var input in MaterialInputs)
        {
            input.UpdateRequiredAmount();
        }

        SaveProductionCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadProductRecipesAsync(ProductionSummaryDto? product)
    {
        _productRecipesLoadCancellation?.Cancel();

        var selectedRecipe = SelectedRecipe;

        if (product is null)
        {
            ProductRecipes.Clear();
            SelectedRecipe = null;
            return;
        }

        var cancellation = new CancellationTokenSource();
        _productRecipesLoadCancellation = cancellation;

        try
        {
            await YieldToUiAsync();
            cancellation.Token.ThrowIfCancellationRequested();

            if (SelectedProduct?.Id != product.Id)
            {
                return;
            }

            var result = await _mediator.Send(
                new GetRecipesQuery(0, 1000, null, product.Id),
                cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            if (SelectedProduct?.Id != product.Id)
            {
                return;
            }

            _isRestoringProductRecipesSelection = true;
            try
            {
                SelectedRecipe = ReplaceItemsPreservingSelection(
                    ProductRecipes,
                    result.Items,
                    selectedRecipe,
                    recipe => recipe.Id);

                SelectedRecipe ??= ProductRecipes.FirstOrDefault();
            }
            finally
            {
                _isRestoringProductRecipesSelection = false;
            }

            HandleSelectedRecipeChanged(SelectedRecipe);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A newer product selection superseded this request.
        }
        finally
        {
            if (ReferenceEquals(_productRecipesLoadCancellation, cancellation))
            {
                _productRecipesLoadCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private async Task LoadMaterialsAsync()
    {
        _materialsLoadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _materialsLoadCancellation = cancellation;

        try
        {
            await YieldToUiAsync();
            cancellation.Token.ThrowIfCancellationRequested();

            if (ShowRecipeDetails)
            {
                if (SelectedRecipe is null)
                {
                    SelectedMaterials.Clear();
                    return;
                }

                var result = await _mediator.Send(
                    new GetRecipeMaterialsQuery(SelectedRecipe.Id),
                    cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                ReplaceItems(SelectedMaterials, result.Items.Cast<object>());
                return;
            }

            if (SelectedProduction is null)
            {
                SelectedMaterials.Clear();
                return;
            }

            var productionMaterials = await _mediator.Send(
                new GetProductionMaterialsQuery(SelectedProduction.Id),
                cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();
            ReplaceItems(SelectedMaterials, productionMaterials.Cast<object>());
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // A newer selection or display mode superseded this request.
        }
        finally
        {
            if (ReferenceEquals(_materialsLoadCancellation, cancellation))
            {
                _materialsLoadCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private bool CanAddProduction()
    {
        return !IsBusy &&
               !IsAddingProduction &&
               !IsEditingProduction &&
               SelectedRecipe is not null;
    }

    private async Task AddProductionAsync()
    {
        if (SelectedRecipe is null)
        {
            return;
        }

        IsBusy = true;
        NotifyCommandStatesChanged();
        try
        {
            ProductionAmount = 1;
            ProductionDescription = string.Empty;
            ProductionDate = DateTime.Now;
            MaterialInputs.Clear();

            await InitializeMaterialInputsAsync(SelectedRecipe);

            IsAddingProduction = true;
            IsEditingProduction = false;
            _editingProductionId = null;
        }
        finally
        {
            IsBusy = false;
            NotifyCommandStatesChanged();
        }
    }

    private bool CanEditProduction(ProductionHistoryDto? production)
    {
        return !IsBusy &&
               !IsAddingProduction &&
               !IsEditingProduction &&
               production is not null;
    }

    private bool CanDeleteProduction(ProductionHistoryDto? production)
    {
        return CanEditProduction(production);
    }

    private async Task EditProductionAsync(ProductionHistoryDto? production)
    {
        if (production is null)
        {
            return;
        }

        IsBusy = true;
        NotifyCommandStatesChanged();
        try
        {
            var productionMaterials = await _mediator.Send(
                new GetProductionMaterialsQuery(production.Id));

            MaterialInputs.Clear();
            foreach (var item in productionMaterials)
            {
                // Fetch variants for this material
                var variantsResult = await _mediator.Send(new GetMaterialVariantsQuery(item.MaterialId, 0, 1000));
                
                var input = new DynamicMaterialInput(this, _mediator)
                {
                    ProductionMaterialId = item.Id,
                    RecipeMaterialId = Guid.Empty,
                    MaterialId = item.MaterialId,
                    MaterialName = item.MaterialName,
                    RecipeAmount = 0,
                    UsedAmount = item.UsedAmount,
                    DefaultUsedAmount = item.UsedAmount,
                    Unit = item.Unit
                };
                
                foreach (var v in variantsResult.Items) input.Variants.Add(v);
                input.SelectedVariant = input.Variants.FirstOrDefault(v => v.Id == item.MaterialVariantId);
                
                MaterialInputs.Add(input);
            }

            ProductionAmount = production.ProductionAmount;
            ProductionDescription = production.Description;
            ProductionDate = production.ProductionDate;
            _editingProductionId = production.Id;
            IsAddingProduction = false;
            IsEditingProduction = true;

            foreach (var input in MaterialInputs)
            {
                input.UpdateRequiredAmount();
            }
        }
        finally
        {
            IsBusy = false;
            NotifyCommandStatesChanged();
        }
    }

    private void DeleteProduction(ProductionHistoryDto? production)
    {
        if (production is null)
        {
            return;
        }

        ProductionToDelete = production;
        IsDeletePopupOpen = true;
    }

    private async Task ConfirmDeleteProductionAsync()
    {
        if (ProductionToDelete is null)
        {
            return;
        }

        IsBusy = true;
        NotifyCommandStatesChanged();
        try
        {
            var deletedProductionId = ProductionToDelete.Id;
            await _mediator.Send(new DeleteProductionCommand(deletedProductionId));

            IsDeletePopupOpen = false;
            ProductionToDelete = null;
            if (SelectedProduction?.Id == deletedProductionId)
            {
                SelectedProduction = null;
            }

            await LoadProductRecipesAsync(SelectedProduct);
            RequestPaginationRefresh(ProductionPaginationTarget.History);
            RequestPaginationRefresh(ProductionPaginationTarget.Productions);
        }
        finally
        {
            IsBusy = false;
            NotifyCommandStatesChanged();
        }
    }

    private void CancelDeleteProduction()
    {
        IsDeletePopupOpen = false;
        ProductionToDelete = null;
    }

    private bool CanSaveProduction()
    {
        if (IsBusy) return false;
        if (!IsAddingProduction && !IsEditingProduction) return false;
        if (SelectedProduct is null) return false;
        if (ProductionAmount <= 0) return false;

        var selectedVariantIds = MaterialInputs
            .Select(m => m.SelectedVariant?.Id)
            .Where(id => id.HasValue)
            .ToList();

        // Must have at least one variant and no duplicates
        if (selectedVariantIds.Count == 0) return false;
        if (selectedVariantIds.Distinct().Count() != selectedVariantIds.Count) return false;

        return true;
    }

    private async Task SaveProductionAsync()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        ClearValidationErrors();
        IsBusy = true;
        NotifyCommandStatesChanged();
        try
        {
            Guid? createdProductionId = null;
            if (IsEditingProduction)
            {
                if (!_editingProductionId.HasValue)
                {
                    return;
                }

                await _mediator.Send(new UpdateProductionCommand(
                    _editingProductionId.Value,
                    ProductionAmount,
                    ProductionDescription,
                    ProductionDate,
                    MaterialInputs
                        .Where(input => input.SelectedVariant != null)
                        .Select(input => new MaterialVariantUsageDto(
                            input.ProductionMaterialId,
                            input.SelectedVariant!.Id,
                            input.UsedAmount))));
            }
            else
            {
                createdProductionId = await _mediator.Send(new AddProductionCommand(
                    SelectedProduct.Id,
                    ProductionAmount,
                    ProductionDescription,
                    ProductionDate,
                    MaterialInputs
                        .Where(input => input.SelectedVariant != null)
                        .Select(input => new MaterialVariantUsageDto(
                            null,
                            input.SelectedVariant!.Id,
                            input.UsedAmount))));
            }

            _pendingCreatedProductionId = createdProductionId;
            CancelAddProduction();
            await LoadProductRecipesAsync(SelectedProduct);
            RequestPaginationRefresh(ProductionPaginationTarget.History);
            RequestPaginationRefresh(ProductionPaginationTarget.Productions);
        }
        catch (ApplicationLogic.Exceptions.RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
        }
        finally
        {
            IsBusy = false;
            NotifyCommandStatesChanged();
        }
    }

    private void CancelAddProduction()
    {
        IsAddingProduction = false;
        IsEditingProduction = false;
        _editingProductionId = null;
        MaterialInputs.Clear();
        NotifyCommandStatesChanged();
    }

    private async Task InitializeMaterialInputsAsync(RecipeDto recipe)
    {
        var recipeMaterials = await _mediator.Send(
            new GetRecipeMaterialsQuery(recipe.Id, 0, 100));
        
        var recipeMaterialIds = recipeMaterials.Items
            .Select(item => item.Id)
            .ToHashSet();

        foreach (var obsoleteInput in MaterialInputs
                     .Where(input => !recipeMaterialIds.Contains(input.RecipeMaterialId))
                     .ToList())
        {
            MaterialInputs.Remove(obsoleteInput);
        }

        foreach (var item in recipeMaterials.Items)
        {
            var existingInput = MaterialInputs.FirstOrDefault(
                input => input.RecipeMaterialId == item.Id);

            if (existingInput is not null)
            {
                if (!existingInput.IsModified)
                {
                    existingInput.UsedAmount = 0;
                }

                existingInput.DefaultUsedAmount = 0;
                existingInput.UpdateRequiredAmount();
                continue;
            }

            // Fetch variants for this material
            var variantsResult = await _mediator.Send(new GetMaterialVariantsQuery(item.MaterialId, 0, 1000));

            var input = new DynamicMaterialInput(this, _mediator)
            {
                RecipeMaterialId = item.Id,
                MaterialId = item.MaterialId,
                MaterialName = item.MaterialName,
                RecipeAmount = 0,
                UsedAmount = 0,
                DefaultUsedAmount = 0,
                Unit = null
            };
            
            foreach (var v in variantsResult.Items) input.Variants.Add(v);
            input.SelectedVariant = input.Variants.FirstOrDefault();

            MaterialInputs.Add(input);
            input.UpdateRequiredAmount();
        }
    }

    private void RequestPaginationRefresh(ProductionPaginationTarget target)
    {
        PaginationRefreshRequested?.Invoke(target);
    }

    private void NotifyCommandStatesChanged()
    {
        AddProductionCommand.NotifyCanExecuteChanged();
        EditProductionCommand.NotifyCanExecuteChanged();
        DeleteProductionCommand.NotifyCanExecuteChanged();
        SaveProductionCommand.NotifyCanExecuteChanged();
    }

    private static void ReplaceItems<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();

        foreach (var item in source)
        {
            target.Add(item);
        }
    }
}

public partial class DynamicMaterialInput : ObservableObject
{
    private readonly ProductionListViewModel _parent;
    private readonly IMediator _mediator;

    public DynamicMaterialInput(ProductionListViewModel parent, IMediator mediator)
    {
        _parent = parent;
        _mediator = mediator;
    }

    public Guid? ProductionMaterialId { get; init; }
    public Guid RecipeMaterialId { get; init; }
    public Guid MaterialId { get; init; }
    public string MaterialName { get; init; } = null!;
    public double RecipeAmount { get; init; }

    [ObservableProperty]
    private string? _unit;

    public ObservableCollection<MaterialVariantDto> Variants { get; } = new();

    [ObservableProperty]
    private MaterialVariantDto? _selectedVariant;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    private double _defaultUsedAmount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    private double _usedAmount;

    [ObservableProperty]
    private double _totalRequiredAmount;

    [ObservableProperty]
    private double _availableAmount;

    [ObservableProperty]
    private bool _hasEnough;

    public bool IsModified => Math.Abs(UsedAmount - DefaultUsedAmount) > 0.0001;

    [RelayCommand]
    private void ReloadDefault()
    {
        UsedAmount = DefaultUsedAmount;
    }

    [RelayCommand]
    private void Remove()
    {
        _parent.MaterialInputs.Remove(this);
    }

    public void UpdateRequiredAmount()
    {
        TotalRequiredAmount = ProductionMaterial.CalculateTotalUsedAmount(UsedAmount, _parent.ProductionAmount);
        AvailableAmount = SelectedVariant != null 
            ? MaterialVariant.CalculateTotalAvailableAmount(SelectedVariant.TotalCompanyAmount, SelectedVariant.TotalPrivateAmount, SelectedVariant.TotalUsedAmount)
            : 0;
        HasEnough = AvailableAmount >= TotalRequiredAmount;
        Unit = SelectedVariant?.Unit;
        _parent.SaveProductionCommand.NotifyCanExecuteChanged();
    }

    partial void OnDefaultUsedAmountChanged(double value) => UpdateRequiredAmount();
    partial void OnUsedAmountChanged(double value) => UpdateRequiredAmount();
    partial void OnSelectedVariantChanged(MaterialVariantDto? value) => UpdateRequiredAmount();
}

public class AddMaterialButtonViewModel
{
    public ProductionListViewModel Parent { get; }
    public AddMaterialButtonViewModel(ProductionListViewModel parent) => Parent = parent;
}
