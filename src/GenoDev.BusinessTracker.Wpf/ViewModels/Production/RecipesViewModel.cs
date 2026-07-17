using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetRecipes;
using MediatR;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Production;

public partial class RecipesViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private CancellationTokenSource? _filterCancellationTokenSource;

    public RecipesViewModel(IMediator mediator)
    {
        _mediator = mediator;
        LoadRecipesCommand = new AsyncRelayCommand(LoadRecipesAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        
        CreateRecipeCommand = new RelayCommand(CreateRecipe);
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);

        AvailablePageSizes = new ObservableCollection<int> { 5, 10, 20, 50 };
        _ = LoadRecipesAsync();
    }

    public ObservableCollection<RecipeDto> Recipes { get; } = new();

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

    [ObservableProperty]
    private string? _searchTerm;

    [ObservableProperty]
    private RecipeDto? _selectedRecipe;

    [ObservableProperty]
    private CreateRecipeViewModel? _createRecipeViewModel;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    public IAsyncRelayCommand LoadRecipesCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IRelayCommand CreateRecipeCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    partial void OnSearchTermChanged(string? value)
    {
        DebounceLoad();
    }

    partial void OnPageSizeChanged(int value)
    {
        PageIndex = 0;
        _ = LoadRecipesAsync();
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
            var query = new GetRecipesQuery(PageIndex, PageSize, SearchTerm);
            var result = await _mediator.Send(query);

            Recipes.Clear();
            foreach (var item in result.Items)
            {
                Recipes.Add(item);
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
        if (CreateRecipeViewModel == null)
        {
            CreateRecipeViewModel = new CreateRecipeViewModel(_mediator);
            CreateRecipeViewModel.RequestClose += async () =>
            {
                IsCreatePopupOpen = false;
                await RefreshAsync();
            };
        }

        CreateRecipeViewModel.Clear();
        await CreateRecipeViewModel.LoadProductsAsync();
        IsCreatePopupOpen = true;
    }

    private async Task RefreshAsync()
    {
        PageIndex = 0;
        await LoadRecipesAsync();
    }
}
