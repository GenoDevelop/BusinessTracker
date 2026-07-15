using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Materials;

public partial class MaterialListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;

    public MaterialListViewModel(IMediator mediator, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
        LoadMaterialsCommand = new AsyncRelayCommand(LoadMaterialsAsync);
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => HasNextPage);
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => PageIndex > 0);
        CreateMaterialCommand = new RelayCommand(OpenCreatePopup);
        EditMaterialCommand = new RelayCommand<MaterialDto>(OpenEditPopup);
        DeleteMaterialCommand = new RelayCommand<MaterialDto>(OpenDeletePopup);
        ConfirmDeleteCommand = new AsyncRelayCommand(ConfirmDeleteAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
        AvailablePageSizes = new ObservableCollection<int> { 5, 10, 20, 50 };
        _ = LoadMaterialsAsync();
    }

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private CreateMaterialViewModel? _createMaterialViewModel;

    [ObservableProperty]
    private bool _isDeletePopupOpen;

    [ObservableProperty]
    private MaterialDto? _materialToDelete;

    public ObservableCollection<MaterialDto> Materials { get; } = new();

    [ObservableProperty]
    private MaterialSortBy _sortBy = MaterialSortBy.Name;

    [ObservableProperty]
    private bool _isDescending;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PreviousPageCommand))]
    [NotifyPropertyChangedFor(nameof(DisplayPage))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _pageIndex;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    private int _pageSize = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPages))]
    [NotifyPropertyChangedFor(nameof(ItemsRange))]
    private int _totalCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextPageCommand))]
    private bool _hasNextPage;

    public ObservableCollection<int> AvailablePageSizes { get; }

    public int DisplayPage => PageIndex + 1;

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

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

    public IAsyncRelayCommand LoadMaterialsCommand { get; }
    public IAsyncRelayCommand NextPageCommand { get; }
    public IAsyncRelayCommand PreviousPageCommand { get; }
    public IRelayCommand CreateMaterialCommand { get; }
    public IRelayCommand<MaterialDto> EditMaterialCommand { get; }
    public IRelayCommand<MaterialDto> DeleteMaterialCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteCommand { get; }
    public IRelayCommand CancelDeleteCommand { get; }

    partial void OnPageSizeChanged(int value)
    {
        PageIndex = 0;
        _ = LoadMaterialsAsync();
    }

    partial void OnSortByChanged(MaterialSortBy value)
    {
        _ = LoadMaterialsAsync();
    }

    partial void OnIsDescendingChanged(bool value)
    {
        _ = LoadMaterialsAsync();
    }

    private async Task LoadMaterialsAsync()
    {
        IsBusy = true;
        try
        {
            var query = new GetMaterialsQuery(PageIndex, PageSize, SortBy, IsDescending);
            var result = await _mediator.Send(query);

            Materials.Clear();
            foreach (var item in result.Items)
            {
                Materials.Add(item);
            }

            TotalCount = result.TotalCount;
            HasNextPage = result.HasNextPage;

            if (PageIndex > 0 && Materials.Count == 0 && TotalCount > 0)
            {
                var maxPage = (int)Math.Ceiling((double)TotalCount / PageSize) - 1;
                if (PageIndex > maxPage)
                {
                    PageIndex = Math.Max(0, maxPage);
                    await LoadMaterialsAsync();
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
        await LoadMaterialsAsync();
    }

    private async Task PreviousPageAsync()
    {
        if (PageIndex > 0)
        {
            PageIndex--;
            await LoadMaterialsAsync();
        }
    }

    private void OpenCreatePopup()
    {
        CreateMaterialViewModel = _serviceProvider.GetRequiredService<CreateMaterialViewModel>();
        
        CreateMaterialViewModel.RequestClose += () =>
        {
            IsCreatePopupOpen = false;
            _ = LoadMaterialsAsync();
        };

        IsCreatePopupOpen = true;
    }

    private void OpenEditPopup(MaterialDto? material)
    {
        if (material == null) return;
        
        CreateMaterialViewModel = _serviceProvider.GetRequiredService<CreateMaterialViewModel>();
        CreateMaterialViewModel.InitializeForEdit(material);
        
        CreateMaterialViewModel.RequestClose += () =>
        {
            IsCreatePopupOpen = false;
            _ = LoadMaterialsAsync();
        };

        IsCreatePopupOpen = true;
    }

    private void OpenDeletePopup(MaterialDto? material)
    {
        if (material == null) return;
        MaterialToDelete = material;
        IsDeletePopupOpen = true;
    }

    private async Task ConfirmDeleteAsync()
    {
        if (MaterialToDelete == null) return;

        IsBusy = true;
        try
        {
            await _mediator.Send(new DeleteMaterialCommand(MaterialToDelete.Id));
            IsDeletePopupOpen = false;
            MaterialToDelete = null;
            await LoadMaterialsAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CancelDelete()
    {
        IsDeletePopupOpen = false;
        MaterialToDelete = null;
    }
}
