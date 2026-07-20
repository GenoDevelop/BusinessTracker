using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteRecipe;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipeMaterials;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class RecipesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _filterCancellationTokenSource;
    private CancellationTokenSource? _itemsFilterCancellationTokenSource;

    [ObservableProperty]
    private bool _isDeleteConfirmationOpen;

    [ObservableProperty]
    private bool _isItemsFilterVisible;

    [ObservableProperty]
    private string? _amountFilterValue;

    public List<OperatorWrapper> AvailableOperators { get; } = new()
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
    [NotifyPropertyChangedFor(nameof(IsAmountFilterEnabled))]
    private OperatorWrapper? _selectedAmountOperator;

    public bool IsAmountFilterEnabled => SelectedAmountOperator?.Operator != null;

    [ObservableProperty]
    private RecipeMaterialSortBy _materialSortBy = RecipeMaterialSortBy.MaterialName;

    [ObservableProperty]
    private bool _isMaterialDescending;

    public RecipesViewModel(IMediator mediator)
    {
        _mediator = mediator;
        _selectedAmountOperator = AvailableOperators[0];
        LoadRecipesCommand = new AsyncRelayCommand(LoadRecipesAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        
        CreateRecipeCommand = new RelayCommand(CreateRecipe);
        EditRecipeCommand = new AsyncRelayCommand(EditRecipeAsync);
        DeleteRecipeCommand = new RelayCommand(DeleteRecipe);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
        RefreshCommand = new AsyncRelayCommand(ManualRefreshAsync);

        RefreshRecipeMaterialsCommand = new AsyncRelayCommand(LoadRecipeMaterialsAsync);
        NextRecipeMaterialsPageCommand = new AsyncRelayCommand(NextRecipeMaterialsPageAsync, () => HasNextRecipeMaterialsPage);
        PreviousRecipeMaterialsPageCommand = new AsyncRelayCommand(PreviousRecipeMaterialsPageAsync, () => RecipeMaterialsPageIndex > 0);

        AddRecipeMaterialCommand = new RelayCommand(AddRecipeMaterial);
        EditRecipeMaterialCommand = new RelayCommand<RecipeMaterialDto>(EditRecipeMaterial);
        DeleteRecipeMaterialCommand = new RelayCommand<RecipeMaterialDto>(DeleteRecipeMaterial);
        ConfirmDeleteMaterialCommand = new AsyncRelayCommand(ConfirmDeleteMaterialAsync);
        CancelDeleteMaterialCommand = new RelayCommand(CancelDeleteMaterial);

        AvailablePageSizes = new ObservableCollection<int> { 5, 10, 20, 50 };
        _ = LoadRecipesAsync();
    }

    public ObservableCollection<RecipeDto> Recipes { get; } = new();
    public ObservableCollection<RecipeMaterialDto> RecipeMaterials { get; } = new();

    [ObservableProperty]
    private bool _isFilterVisible;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
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
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private bool _hasNextPage;

    // Recipe Materials Pagination
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousRecipeMaterialsPageCommand))]
    [NotifyPropertyChangedFor(nameof(DisplayRecipeMaterialsPage))]
    [NotifyPropertyChangedFor(nameof(RecipeMaterialsRange))]
    private int _recipeMaterialsPageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RecipeMaterialsRange))]
    [NotifyPropertyChangedFor(nameof(TotalRecipeMaterialsPages))]
    private int _recipeMaterialsPageSize = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalRecipeMaterialsPages))]
    [NotifyPropertyChangedFor(nameof(RecipeMaterialsRange))]
    private int _totalRecipeMaterialsCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextRecipeMaterialsPageCommand))]
    private bool _hasNextRecipeMaterialsPage;

    public ObservableCollection<int> AvailablePageSizes { get; }

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

    public int TotalRecipeMaterialsPages => (int)Math.Ceiling((double)TotalRecipeMaterialsCount / RecipeMaterialsPageSize);
    public int DisplayRecipeMaterialsPage => RecipeMaterialsPageIndex + 1;
    public string RecipeMaterialsRange
    {
        get
        {
            if (TotalRecipeMaterialsCount == 0) return "0-0 / 0";
            var start = RecipeMaterialsPageIndex * RecipeMaterialsPageSize + 1;
            var end = Math.Min((RecipeMaterialsPageIndex + 1) * RecipeMaterialsPageSize, TotalRecipeMaterialsCount);
            return $"{start}-{end} / {TotalRecipeMaterialsCount}";
        }
    }

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private string? _materialNameFilter;

    [ObservableProperty]
    private string? _eanFilter;

    [ObservableProperty]
    private RecipeDto? _selectedRecipe;

    partial void OnSelectedRecipeChanged(RecipeDto? value)
    {
        RecipeMaterialsPageIndex = 0;
        _ = LoadRecipeMaterialsAsync();
    }

    [ObservableProperty]
    private AddRecipeMaterialViewModel? _addRecipeMaterialViewModel;

    [ObservableProperty]
    private bool _isAddMaterialPopupOpen;

    [ObservableProperty]
    private bool _isDeleteMaterialConfirmationOpen;

    private RecipeMaterialDto? _materialToDelete;

    [ObservableProperty]
    private CreateRecipeViewModel? _createRecipeViewModel;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    public IAsyncRelayCommand LoadRecipesCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IRelayCommand CreateRecipeCommand { get; }
    public IAsyncRelayCommand EditRecipeCommand { get; }
    public IRelayCommand DeleteRecipeCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteCommand { get; }
    public IRelayCommand CancelDeleteCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand RefreshRecipeMaterialsCommand { get; }
    public IAsyncRelayCommand NextRecipeMaterialsPageCommand { get; }
    public IAsyncRelayCommand PreviousRecipeMaterialsPageCommand { get; }

    public IRelayCommand AddRecipeMaterialCommand { get; }
    public IRelayCommand<RecipeMaterialDto> EditRecipeMaterialCommand { get; }
    public IRelayCommand<RecipeMaterialDto> DeleteRecipeMaterialCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteMaterialCommand { get; }
    public IRelayCommand CancelDeleteMaterialCommand { get; }

    partial void OnSearchTermChanged(string? value)
    {
        PageIndex = 0;
        DebounceLoad();
    }

    partial void OnPageSizeChanged(int value)
    {
        PageIndex = 0;
        _ = LoadRecipesAsync();
    }

    partial void OnRecipeMaterialsPageSizeChanged(int value)
    {
        RecipeMaterialsPageIndex = 0;
        _ = LoadRecipeMaterialsAsync();
    }

    partial void OnMaterialNameFilterChanged(string? value)
    {
        RecipeMaterialsPageIndex = 0;
        DebounceLoadItems();
    }

    partial void OnEanFilterChanged(string? value)
    {
        RecipeMaterialsPageIndex = 0;
        DebounceLoadItems();
    }

    partial void OnAmountFilterValueChanged(string? value)
    {
        RecipeMaterialsPageIndex = 0;
        DebounceLoadItems();
    }

    partial void OnSelectedAmountOperatorChanged(OperatorWrapper? value)
    {
        RecipeMaterialsPageIndex = 0;
        DebounceLoadItems();
    }

    partial void OnMaterialSortByChanged(RecipeMaterialSortBy value)
    {
        _ = LoadRecipeMaterialsAsync();
    }

    partial void OnIsMaterialDescendingChanged(bool value)
    {
        _ = LoadRecipeMaterialsAsync();
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
                        PageIndex = 0;
                        await LoadRecipesAsync();
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    private void DebounceLoadItems()
    {
        _itemsFilterCancellationTokenSource?.Cancel();
        _itemsFilterCancellationTokenSource = new CancellationTokenSource();
        var token = _itemsFilterCancellationTokenSource.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                if (!token.IsCancellationRequested)
                {
                    await App.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        RecipeMaterialsPageIndex = 0;
                        await LoadRecipeMaterialsAsync();
                    });
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
        }, token);
    }

    private async Task LoadRecipeMaterialsAsync()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            await App.Current.Dispatcher.InvokeAsync(LoadRecipeMaterialsAsync);
            return;
        }

        if (SelectedRecipe == null)
        {
            RecipeMaterials.Clear();
            TotalRecipeMaterialsCount = 0;
            HasNextRecipeMaterialsPage = false;
            OnPropertyChanged(nameof(TotalRecipeMaterialsPages));
            OnPropertyChanged(nameof(RecipeMaterialsRange));
            return;
        }

        try
        {
            var query = new GetRecipeMaterialsQuery(
                SelectedRecipe.Id,
                RecipeMaterialsPageIndex,
                RecipeMaterialsPageSize,
                MaterialNameFilter,
                EanFilter,
                IsItemsFilterVisible ? ParseDouble(AmountFilterValue) : null,
                IsItemsFilterVisible ? SelectedAmountOperator?.Operator : null,
                MaterialSortBy,
                IsMaterialDescending);

            var result = await _mediator.Send(query);

            RecipeMaterials.Clear();
            foreach (var item in result.Items)
            {
                RecipeMaterials.Add(item);
            }

            TotalRecipeMaterialsCount = result.TotalCount;
            HasNextRecipeMaterialsPage = result.HasNextPage;
            OnPropertyChanged(nameof(TotalRecipeMaterialsPages));
            OnPropertyChanged(nameof(DisplayRecipeMaterialsPage));
            OnPropertyChanged(nameof(RecipeMaterialsRange));
        }
        catch
        {
            // Ignore
        }
    }

    private async Task NextRecipeMaterialsPageAsync()
    {
        RecipeMaterialsPageIndex++;
        await LoadRecipeMaterialsAsync();
    }

    private async Task PreviousRecipeMaterialsPageAsync()
    {
        if (RecipeMaterialsPageIndex > 0)
        {
            RecipeMaterialsPageIndex--;
            await LoadRecipeMaterialsAsync();
        }
    }

    private async Task LoadRecipesAsync()
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            await App.Current.Dispatcher.InvokeAsync(LoadRecipesAsync);
            return;
        }

        IsBusy = true;
        try
        {
            var selectedId = SelectedRecipe?.Id;
            var query = new GetRecipesQuery(PageIndex, PageSize, SearchTerm);
            var result = await _mediator.Send(query);

            Recipes.Clear();
            foreach (var item in result.Items)
            {
                Recipes.Add(item);
            }

            if (selectedId.HasValue)
            {
                SelectedRecipe = Recipes.FirstOrDefault(r => r.Id == selectedId.Value);
            }

            TotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(DisplayPage));
            OnPropertyChanged(nameof(ItemsRange));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task NextPageAsync()
    {
        PageIndex++;
        await LoadRecipesAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            await LoadRecipesAsync();
        }
    }

    private async void CreateRecipe()
    {
        EnsureCreateViewModelInitialized();

        CreateRecipeViewModel!.Clear();
        await CreateRecipeViewModel.LoadProductsAsync();
        IsCreatePopupOpen = true;
    }

    private async Task EditRecipeAsync()
    {
        if (SelectedRecipe == null) return;

        EnsureCreateViewModelInitialized();

        await CreateRecipeViewModel!.LoadProductsAsync();
        CreateRecipeViewModel.LoadRecipe(SelectedRecipe);
        IsCreatePopupOpen = true;
    }

    private void EnsureCreateViewModelInitialized()
    {
        if (CreateRecipeViewModel == null)
        {
            CreateRecipeViewModel = new CreateRecipeViewModel(_mediator);
            CreateRecipeViewModel.RequestClose += async () =>
            {
                IsCreatePopupOpen = false;
                await LoadRecipesAsync();
            };
        }
    }

    private void AddRecipeMaterial()
    {
        if (SelectedRecipe == null) return;
        EnsureAddMaterialViewModelInitialized();
        AddRecipeMaterialViewModel!.InitializeForAdd(SelectedRecipe.Id);
        IsAddMaterialPopupOpen = true;
    }

    private void EditRecipeMaterial(RecipeMaterialDto? material)
    {
        if (SelectedRecipe == null || material == null) return;
        EnsureAddMaterialViewModelInitialized();
        AddRecipeMaterialViewModel!.InitializeForEdit(SelectedRecipe.Id, material);
        IsAddMaterialPopupOpen = true;
    }

    private void EnsureAddMaterialViewModelInitialized()
    {
        if (AddRecipeMaterialViewModel == null)
        {
            AddRecipeMaterialViewModel = new AddRecipeMaterialViewModel(_mediator);
            AddRecipeMaterialViewModel.RequestClose += async () =>
            {
                IsAddMaterialPopupOpen = false;
                await LoadRecipeMaterialsAsync();
            };
        }
    }

    private void DeleteRecipeMaterial(RecipeMaterialDto? material)
    {
        if (material == null) return;
        _materialToDelete = material;
        IsDeleteMaterialConfirmationOpen = true;
    }

    private async Task ConfirmDeleteMaterialAsync()
    {
        if (_materialToDelete == null) return;

        IsBusy = true;
        try
        {
            var command = new GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.RemoveRecipeMaterial.RemoveRecipeMaterialCommand(_materialToDelete.Id);
            await _mediator.Send(command);
            IsDeleteMaterialConfirmationOpen = false;
            _materialToDelete = null;
            await LoadRecipeMaterialsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelDeleteMaterial()
    {
        IsDeleteMaterialConfirmationOpen = false;
        _materialToDelete = null;
    }

    private void DeleteRecipe()
    {
        if (SelectedRecipe == null) return;
        IsDeleteConfirmationOpen = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (SelectedRecipe == null) return;

        IsBusy = true;
        try
        {
            var command = new DeleteRecipeCommand(SelectedRecipe.Id);
            await _mediator.Send(command);
            IsDeleteConfirmationOpen = false;
            SelectedRecipe = null;
            await LoadRecipesAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelDelete()
    {
        IsDeleteConfirmationOpen = false;
    }

    private async Task ManualRefreshAsync()
    {
        PageIndex = 0;
        await LoadRecipesAsync();
    }

    private double? ParseDouble(string? value)
    {
        if (double.TryParse(value, out var result)) return result;
        return null;
    }

    public record OperatorWrapper(NumericOperator? Operator, string Display);
}
