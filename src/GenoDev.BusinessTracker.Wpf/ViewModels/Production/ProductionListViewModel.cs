using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionSummary;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionHistory;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;
using MediatR;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class ProductionListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _filterCancellationTokenSource;
    private CancellationTokenSource? _recipeFilterCancellationTokenSource;
    private CancellationTokenSource? _historyFilterCancellationTokenSource;
    private bool _isRefreshing;

    [ObservableProperty]
    private string? _recipeSearchTerm;

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayPage))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 20;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _totalCount;

    [ObservableProperty]
    private bool _hasNextPage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HistoryDisplayPage))]
    [NotifyPropertyChangedFor(nameof(HistoryItemsRange))]
    private int _historyPageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HistoryItemsRange))]
    [NotifyPropertyChangedFor(nameof(HistoryTotalPages))]
    private int _historyPageSize = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HistoryTotalPages))]
    [NotifyPropertyChangedFor(nameof(HistoryItemsRange))]
    private int _historyTotalCount;

    [ObservableProperty]
    private bool _isHistoryFilterVisible;

    partial void OnIsHistoryFilterVisibleChanged(bool value)
    {
        _ = LoadProductionHistoryAsync();
    }

    [ObservableProperty]
    private string? _historyDescriptionFilter;

    [ObservableProperty]
    private OperatorWrapper? _historyAmountOperatorFilter;

    [ObservableProperty]
    private string? _historyAmountFilterText;

    private int? HistoryAmountFilter => int.TryParse(HistoryAmountFilterText, out var val) ? val : null;

    [ObservableProperty]
    private DateTime? _historyFromDateFilter;

    [ObservableProperty]
    private DateTime? _historyToDateFilter;

    public List<OperatorWrapper> AvailableHistoryOperators { get; } = new()
    {
        new OperatorWrapper(null, ""),
        new OperatorWrapper(NumericOperator.Equal, "="),
        new OperatorWrapper(NumericOperator.NotEqual, "≠"),
        new OperatorWrapper(NumericOperator.LessThan, "<"),
        new OperatorWrapper(NumericOperator.LessThanOrEqual, "≤"),
        new OperatorWrapper(NumericOperator.GreaterThan, ">"),
        new OperatorWrapper(NumericOperator.GreaterThanOrEqual, "≥")
    };

    [ObservableProperty]
    private bool _historyHasNextPage;

    public int HistoryTotalPages => (int)Math.Ceiling((double)HistoryTotalCount / HistoryPageSize);
    public int HistoryDisplayPage => HistoryPageIndex + 1;
    public string HistoryItemsRange
    {
        get
        {
            if (HistoryTotalCount == 0) return "0-0 / 0";
            var start = HistoryPageIndex * HistoryPageSize + 1;
            var end = Math.Min((HistoryPageIndex + 1) * HistoryPageSize, HistoryTotalCount);
            return $"{start}-{end} / {HistoryTotalCount}";
        }
    }

    [ObservableProperty]
    private ProductionSummaryDto? _selectedProduct;

    [ObservableProperty]
    private RecipeDto? _selectedRecipe;

    [ObservableProperty]
    private ProductionHistoryDto? _selectedProduction;

    [ObservableProperty]
    private bool _showRecipeDetails;

    public ObservableCollection<RecipeDto> ProductRecipes { get; } = new();
    public ObservableCollection<ProductionHistoryDto> ProductionHistory { get; } = new();
    public ObservableCollection<object> SelectedMaterials { get; } = new();

    public ObservableCollection<ProductionSummaryDto> Products { get; } = new();
    public ObservableCollection<int> AvailablePageSizes { get; } = new() { 5, 10, 20, 50 };

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public int DisplayPage => PageIndex + 1;
    public string ItemsRange
    {
        get
        {
            if (TotalCount == 0) return "0-0 / 0";
            var start = PageIndex * PageSize + 1;
            var end = Math.Min((PageIndex + 1) * PageSize, TotalCount);
            return $"{start}-{end} / {TotalCount}";
        }
    }

    public IAsyncRelayCommand LoadProductsCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IAsyncRelayCommand NextHistoryPageCommand { get; }
    public IAsyncRelayCommand PreviousHistoryPageCommand { get; }
    public IAsyncRelayCommand ApplyHistoryFilterCommand { get; }
    public IRelayCommand ClearHistoryFilterCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand RefreshHistoryCommand { get; }
    public IAsyncRelayCommand AddProductionCommand { get; }
    public IRelayCommand EditProductionCommand { get; }

    [ObservableProperty]
    private bool _isAddingProduction;

    [ObservableProperty]
    private bool _isEditingProduction;

    private Guid? _editingProductionId;

    [ObservableProperty]
    private bool _isProductionSaved;

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

    public ObservableCollection<DynamicMaterialInput> MaterialInputs { get; } = new();

    public IRelayCommand SaveProductionCommand { get; }
    public IRelayCommand CancelAddProductionCommand { get; }
    public IRelayCommand ConfirmDeleteProductionCommand { get; }
    public IRelayCommand CancelDeleteProductionCommand { get; }

    public ProductionListViewModel(IMediator mediator)
    {
        _mediator = mediator;
        LoadProductsCommand = new AsyncRelayCommand(LoadProductsAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        NextHistoryPageCommand = new AsyncRelayCommand(NextHistoryPageAsync, () => HistoryHasNextPage);
        PreviousHistoryPageCommand = new AsyncRelayCommand(PreviousHistoryPageAsync, () => HistoryPageIndex > 0);
        ApplyHistoryFilterCommand = new AsyncRelayCommand(LoadProductionHistoryAsync);
        ClearHistoryFilterCommand = new RelayCommand(ClearHistoryFilter);
        RefreshCommand = new AsyncRelayCommand(ManualRefreshAsync);
        RefreshHistoryCommand = new AsyncRelayCommand(RefreshHistoryAndRecipesAsync);
        AddProductionCommand = new AsyncRelayCommand(AddProductionAsync, () => SelectedRecipe != null && !IsAddingProduction && !IsEditingProduction);
        EditProductionCommand = new AsyncRelayCommand<ProductionHistoryDto>(EditProductionAsync, (p) => p != null && !IsAddingProduction && !IsEditingProduction);
        SaveProductionCommand = new AsyncRelayCommand(SaveProductionAsync, () => !IsProductionSaved);
        CancelAddProductionCommand = new RelayCommand(CancelAddProduction);
        ConfirmDeleteProductionCommand = new AsyncRelayCommand(ConfirmDeleteProductionAsync);
        CancelDeleteProductionCommand = new RelayCommand(CancelDeleteProduction);

        _ = LoadProductsAsync();
    }

    private async Task LoadProductsAsync()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            await App.Current.Dispatcher.InvokeAsync(LoadProductsAsync);
            return;
        }

        IsBusy = true;
        try
        {
            var currentSelectedId = SelectedProduct?.Id;
            var query = new GetProductionSummaryQuery(PageIndex, PageSize, SearchTerm);
            var result = await _mediator.Send(query);

            Products.Clear();
            foreach (var item in result.Items)
            {
                Products.Add(item);
            }

            TotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;
            
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(DisplayPage));
            OnPropertyChanged(nameof(ItemsRange));
            NextPageCommand.NotifyCanExecuteChanged();
            PreviousPageCommand.NotifyCanExecuteChanged();

            if (currentSelectedId.HasValue)
            {
                var productToSelect = Products.FirstOrDefault(p => p.Id == currentSelectedId.Value);
                if (productToSelect != null)
                {
                    SelectedProduct = productToSelect;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextPageAsync()
    {
        PageIndex++;
        await LoadProductsAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            await LoadProductsAsync();
        }
    }

    private async Task NextHistoryPageAsync()
    {
        HistoryPageIndex++;
        await LoadProductionHistoryAsync();
    }

    private async Task PreviousHistoryPageAsync()
    {
        if (HistoryPageIndex > 0)
        {
            HistoryPageIndex--;
            await LoadProductionHistoryAsync();
        }
    }

    private void ClearHistoryFilter()
    {
        HistoryDescriptionFilter = null;
        HistoryAmountOperatorFilter = AvailableHistoryOperators.FirstOrDefault();
        HistoryAmountFilterText = null;
        HistoryFromDateFilter = null;
        HistoryToDateFilter = null;
        HistoryPageIndex = 0;
        _ = LoadProductionHistoryAsync();
    }

    private async Task ManualRefreshAsync()
    {
        PageIndex = 0;
        await LoadProductsAsync();
    }

    partial void OnSearchTermChanged(string? value)
    {
        PageIndex = 0;
        DebounceLoad();
    }

    partial void OnPageSizeChanged(int value)
    {
        PageIndex = 0;
        _ = LoadProductsAsync();
    }

    partial void OnHistoryPageSizeChanged(int value)
    {
        HistoryPageIndex = 0;
        _ = LoadProductionHistoryAsync();
    }

    partial void OnSelectedProductChanged(ProductionSummaryDto? value)
    {
        HistoryPageIndex = 0;
        _ = LoadProductRecipesAsync(value);
        _ = LoadProductionHistoryAsync();
    }

    partial void OnSelectedRecipeChanged(RecipeDto? value)
    {
        AddProductionCommand.NotifyCanExecuteChanged();
        if (IsAddingProduction && value != null)
        {
            _ = InitializeMaterialInputsAsync(value);
        }

        if (ShowRecipeDetails && !_isRefreshing)
        {
            _ = LoadMaterialsAsync();
        }
    }

    partial void OnSelectedProductionChanged(ProductionHistoryDto? value)
    {
        if (!ShowRecipeDetails && !_isRefreshing)
        {
            _ = LoadMaterialsAsync();
        }
    }

    partial void OnShowRecipeDetailsChanged(bool value)
    {
        _ = LoadMaterialsAsync();
    }

    partial void OnRecipeSearchTermChanged(string? value)
    {
        DebounceLoadRecipes();
    }

    partial void OnHistoryDescriptionFilterChanged(string? value)
    {
        HistoryPageIndex = 0;
        DebounceLoadHistory();
    }

    partial void OnHistoryAmountOperatorFilterChanged(OperatorWrapper? value)
    {
        HistoryPageIndex = 0;
        DebounceLoadHistory();
    }

    partial void OnHistoryAmountFilterTextChanged(string? value)
    {
        HistoryPageIndex = 0;
        DebounceLoadHistory();
    }

    partial void OnHistoryFromDateFilterChanged(DateTime? value)
    {
        HistoryPageIndex = 0;
        DebounceLoadHistory();
    }

    partial void OnHistoryToDateFilterChanged(DateTime? value)
    {
        HistoryPageIndex = 0;
        DebounceLoadHistory();
    }

    private void DebounceLoadHistory()
    {
        _historyFilterCancellationTokenSource?.Cancel();
        _historyFilterCancellationTokenSource = new CancellationTokenSource();
        var token = _historyFilterCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await App.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadProductionHistoryAsync();
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    private void DebounceLoadRecipes()
    {
        _recipeFilterCancellationTokenSource?.Cancel();
        _recipeFilterCancellationTokenSource = new CancellationTokenSource();
        var token = _recipeFilterCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await App.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadProductRecipesAsync(SelectedProduct);
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    private async Task RefreshHistoryAndRecipesAsync()
    {
        HistoryPageIndex = 0;
        var currentRecipeId = SelectedRecipe?.Id;
        
        // Prevent multiple calls to LoadMaterialsAsync during refresh
        _isRefreshing = true;
        try
        {
            await LoadProductionHistoryAsync();
            await LoadProductRecipesAsync(SelectedProduct);
            
            if (currentRecipeId.HasValue)
            {
                var recipeToSelect = ProductRecipes.FirstOrDefault(r => r.Id == currentRecipeId.Value);
                if (recipeToSelect != null)
                {
                    SelectedRecipe = recipeToSelect;
                }
            }
        }
        finally
        {
            _isRefreshing = false;
        }
        
        if (!IsAddingProduction)
        {
            await LoadMaterialsAsync();
        }
    }

    private async Task LoadProductionHistoryAsync()
    {
        ProductionHistory.Clear();
        SelectedProduction = null;
        if (SelectedProduct == null) return;

        var result = await _mediator.Send(new GetProductionHistoryQuery(
            SelectedProduct.Id, 
            HistoryPageIndex, 
            HistoryPageSize,
            IsHistoryFilterVisible ? HistoryDescriptionFilter : null,
            IsHistoryFilterVisible ? HistoryAmountOperatorFilter?.Operator : null,
            IsHistoryFilterVisible ? HistoryAmountFilter : null,
            IsHistoryFilterVisible ? HistoryFromDateFilter : null,
            IsHistoryFilterVisible ? HistoryToDateFilter : null));
        foreach (var item in result.Items)
        {
            ProductionHistory.Add(item);
        }

        HistoryTotalCount = result.TotalCount;
        HistoryHasNextPage = result.HasNextPage;
        
        OnPropertyChanged(nameof(HistoryDisplayPage));
        OnPropertyChanged(nameof(HistoryItemsRange));
        NextHistoryPageCommand.NotifyCanExecuteChanged();
        PreviousHistoryPageCommand.NotifyCanExecuteChanged();
    }

    private async Task LoadMaterialsAsync()
    {
        SelectedMaterials.Clear();
        if (ShowRecipeDetails)
        {
            if (SelectedRecipe == null) return;
            var result = await _mediator.Send(new GetRecipeMaterialsQuery(SelectedRecipe.Id));
            foreach (var item in result.Items)
            {
                SelectedMaterials.Add(item);
            }
        }
        else
        {
            if (SelectedProduction == null) return;
            var result = await _mediator.Send(new GetProductionMaterialsQuery(SelectedProduction.Id));
            foreach (var item in result)
            {
                SelectedMaterials.Add(item);
            }
        }
    }

    private async Task SaveProductionAsync()
    {
        if (SelectedProduct == null) return;

        if (IsEditingProduction)
        {
            if (!_editingProductionId.HasValue) return;

            var command = new UpdateProductionCommand(
                _editingProductionId.Value,
                ProductionAmount,
                ProductionDescription,
                ProductionDate,
                MaterialInputs.Select(x => new MaterialUsageDto(x.ProductionMaterialId, x.MaterialId, x.TotalRequiredAmount)));

            await _mediator.Send(command);
        }
        else
        {
            var command = new AddProductionCommand(
                SelectedProduct.Id,
                ProductionAmount,
                ProductionDescription,
                ProductionDate,
                MaterialInputs.Select(x => new MaterialUsageDto(null, x.MaterialId, x.TotalRequiredAmount)));

            await _mediator.Send(command);
        }

        IsProductionSaved = true;
        SaveProductionCommand.NotifyCanExecuteChanged();
        
        // Refresh underlying data but keep form state
        await RefreshHistoryAndRecipesAsync();
        
        // Refresh product list to show new stock amount and total production
        await LoadProductsAsync(); 

        // Auto-close the form after saving
        CancelAddProduction();
    }

    [RelayCommand]
    private void DeleteProduction(ProductionHistoryDto production)
    {
        if (production == null) return;
        ProductionToDelete = production;
        IsDeletePopupOpen = true;
    }

    private async Task ConfirmDeleteProductionAsync()
    {
        if (ProductionToDelete == null) return;

        try
        {
            await _mediator.Send(new DeleteProductionCommand(ProductionToDelete.Id));
            await RefreshHistoryAndRecipesAsync();
            await LoadProductsAsync(); // Update product stock in the main list
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas usuwania produkcji: {ex.Message}", "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            CancelDeleteProduction();
        }
    }

    private void CancelDeleteProduction()
    {
        IsDeletePopupOpen = false;
        ProductionToDelete = null;
    }

    private void CancelAddProduction()
    {
        IsAddingProduction = false;
        IsEditingProduction = false;
        _editingProductionId = null;
        IsProductionSaved = false;
        AddProductionCommand.NotifyCanExecuteChanged();
        EditProductionCommand.NotifyCanExecuteChanged();
        SaveProductionCommand.NotifyCanExecuteChanged();
    }

    private async Task EditProductionAsync(ProductionHistoryDto? production)
    {
        if (production == null) return;

        _editingProductionId = production.Id;
        ProductionAmount = production.ProductionAmount;
        ProductionDescription = production.Description;
        ProductionDate = production.ProductionDate;
        MaterialInputs.Clear();

        var productionMaterials = await _mediator.Send(new GetProductionMaterialsQuery(production.Id));
        var materials = await _mediator.Send(new GetMaterialsQuery(0, 1000));

        foreach (var pm in productionMaterials)
        {
            var currentStock = materials.Items.FirstOrDefault(m => m.Id == pm.MaterialId)?.Amount ?? 0;
            var input = new DynamicMaterialInput(this)
            {
                ProductionMaterialId = pm.Id,
                RecipeMaterialId = Guid.Empty, // Not tied to a recipe material in edit mode unless we search for it
                MaterialId = pm.MaterialId,
                MaterialName = pm.MaterialName,
                RecipeAmount = 0, // Not applicable in edit mode from production history
                UsedAmount = pm.UsedAmount / production.ProductionAmount, // We want the per-unit amount for the form
                DefaultUsedAmount = pm.UsedAmount / production.ProductionAmount,
                CurrentStock = currentStock + pm.UsedAmount, // Stock if this production never happened
                Unit = pm.Unit
            };
            MaterialInputs.Add(input);
            input.UpdateRequiredAmount();
        }

        IsProductionSaved = false;
        IsEditingProduction = true;
        AddProductionCommand.NotifyCanExecuteChanged();
        EditProductionCommand.NotifyCanExecuteChanged();
        SaveProductionCommand.NotifyCanExecuteChanged();
    }

    private async Task AddProductionAsync()
    {
        if (SelectedRecipe == null) return;

        ProductionAmount = 1;
        ProductionDescription = string.Empty;
        ProductionDate = DateTime.Now;
        MaterialInputs.Clear();

        await InitializeMaterialInputsAsync(SelectedRecipe);

        IsProductionSaved = false;
        IsAddingProduction = true;
        AddProductionCommand.NotifyCanExecuteChanged();
        SaveProductionCommand.NotifyCanExecuteChanged();
    }

    private async Task InitializeMaterialInputsAsync(RecipeDto recipe)
    {
        var recipeMaterials = await _mediator.Send(new GetRecipeMaterialsQuery(recipe.Id, 0, 100));
        var materials = await _mediator.Send(new GetMaterialsQuery(0, 1000));

        var existingInputs = MaterialInputs.ToList();
        var newRecipeMaterialIds = recipeMaterials.Items.Select(rm => rm.Id).ToHashSet();

        // Remove materials no longer in the recipe
        var inputsToRemove = existingInputs.Where(i => !newRecipeMaterialIds.Contains(i.RecipeMaterialId)).ToList();
        foreach (var input in inputsToRemove)
        {
            MaterialInputs.Remove(input);
        }

        foreach (var rm in recipeMaterials.Items)
        {
            var currentStock = materials.Items.FirstOrDefault(m => m.Id == rm.MaterialId)?.Amount ?? 0;
            var existingInput = MaterialInputs.FirstOrDefault(i => i.RecipeMaterialId == rm.Id);

            if (existingInput != null)
            {
                // If it was default, update to new default. If modified, keep custom value.
                if (!existingInput.IsModified)
                {
                    existingInput.UsedAmount = rm.RequiredAmount;
                }
                
                existingInput.DefaultUsedAmount = rm.RequiredAmount;
                existingInput.CurrentStock = currentStock;
                existingInput.UpdateRequiredAmount();
            }
            else
            {
                var input = new DynamicMaterialInput(this)
                {
                    RecipeMaterialId = rm.Id,
                    MaterialId = rm.MaterialId,
                    MaterialName = rm.MaterialName,
                    RecipeAmount = rm.RequiredAmount,
                    UsedAmount = rm.RequiredAmount,
                    DefaultUsedAmount = rm.RequiredAmount,
                    CurrentStock = currentStock,
                    Unit = rm.Unit
                };
                MaterialInputs.Add(input);
                input.UpdateRequiredAmount();
            }
        }
    }

    partial void OnProductionAmountChanged(int value)
    {
        foreach (var input in MaterialInputs)
        {
            input.UpdateRequiredAmount();
        }
    }
}

public partial class DynamicMaterialInput : ObservableObject
{
    private readonly ProductionListViewModel _parent;

    public DynamicMaterialInput(ProductionListViewModel parent)
    {
        _parent = parent;
    }

    public Guid? ProductionMaterialId { get; init; }
    public Guid RecipeMaterialId { get; init; }
    public Guid MaterialId { get; init; }
    public string MaterialName { get; init; } = null!;
    public double RecipeAmount { get; init; }
    public string? Unit { get; init; }
    public double DefaultUsedAmount { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsModified))]
    private double _usedAmount;

    [ObservableProperty]
    private double _currentStock;

    [ObservableProperty]
    private double _totalRequiredAmount;

    [ObservableProperty]
    private bool _hasEnough;

    public bool IsModified => Math.Abs(UsedAmount - DefaultUsedAmount) > 0.0001;

    [RelayCommand]
    private void ReloadDefault()
    {
        UsedAmount = DefaultUsedAmount;
    }

    public void UpdateRequiredAmount()
    {
        TotalRequiredAmount = UsedAmount * _parent.ProductionAmount;
        HasEnough = CurrentStock >= TotalRequiredAmount;
    }

    partial void OnUsedAmountChanged(double value) => UpdateRequiredAmount();
    partial void OnCurrentStockChanged(double value) => UpdateRequiredAmount();
}

public partial class ProductionListViewModel
{
    private async Task LoadProductRecipesAsync(ProductionSummaryDto? product)
    {
        var currentSelectedId = SelectedRecipe?.Id;
        ProductRecipes.Clear();
        SelectedRecipe = null;

        if (product == null) return;

        var result = await _mediator.Send(new GetRecipesQuery(0, 100, RecipeSearchTerm, product.Id));
        
        foreach (var recipe in result.Items)
        {
            ProductRecipes.Add(recipe);
        }

        if (currentSelectedId.HasValue)
        {
            SelectedRecipe = ProductRecipes.FirstOrDefault(r => r.Id == currentSelectedId.Value);
        }

        if (SelectedRecipe == null && ProductRecipes.Any())
        {
            SelectedRecipe = ProductRecipes.First();
        }
    }

    private void DebounceLoad()
    {
        _filterCancellationTokenSource?.Cancel();
        _filterCancellationTokenSource = new CancellationTokenSource();
        var token = _filterCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await App.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        await LoadProductsAsync();
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    public record OperatorWrapper(NumericOperator? Operator, string Display);
}
