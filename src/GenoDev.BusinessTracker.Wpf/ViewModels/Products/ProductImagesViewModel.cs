using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;
using MediatR;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Imaging;

namespace GenoDev.BusinessTracker.Wpf.ViewModels.Products;

public partial class ProductImagesViewModel : ViewModelBase
{
    private const int MinZoomPercent = 25;
    private const int MaxZoomPercent = 400;
    private const int ZoomStepPercent = 25;
    private static readonly IReadOnlyDictionary<string, string> ContentTypesByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".gif"] = "image/gif",
            [".bmp"] = "image/bmp",
            [".tif"] = "image/tiff",
            [".tiff"] = "image/tiff"
        };

    private readonly IMediator _mediator;
    private CancellationTokenSource? _imagesLoadCancellation;
    private CancellationTokenSource? _contentLoadCancellation;
    private ProductImageDto? _imageToDelete;

    public ProductImagesViewModel(IMediator mediator)
    {
        _mediator = mediator;
        PreviousImageCommand = new RelayCommand(ShowPreviousImage, CanMoveBetweenImages);
        NextImageCommand = new RelayCommand(ShowNextImage, CanMoveBetweenImages);
        OpenDeleteConfirmationCommand = new RelayCommand(
            OpenDeleteConfirmation,
            CanDeleteImage);
        ConfirmDeleteCommand = new AsyncRelayCommand(DeleteSelectedImageAsync);
        CancelDeleteCommand = new RelayCommand(CancelDelete);
        OpenPopupCommand = new AsyncRelayCommand(
            OpenCurrentProductPopupAsync,
            CanOpenPopup);
        ClosePopupCommand = new RelayCommand(ClosePopup);
        ZoomInCommand = new RelayCommand(ZoomIn, CanZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut, CanZoomOut);
    }

    public ObservableCollection<ProductImageDto> Images { get; } = new();

    [ObservableProperty]
    private Guid? _productId;

    [ObservableProperty]
    private bool _canManage;

    [ObservableProperty]
    private ProductImageDto? _selectedImage;

    [ObservableProperty]
    private BitmapImage? _imageSource;

    [ObservableProperty]
    private string _imagePositionText = string.Empty;

    [ObservableProperty]
    private bool _isDeleteConfirmationOpen;

    [ObservableProperty]
    private bool _isPopupOpen;

    [ObservableProperty]
    private int _zoomPercent = 100;

    public IRelayCommand PreviousImageCommand { get; }
    public IRelayCommand NextImageCommand { get; }
    public IRelayCommand OpenDeleteConfirmationCommand { get; }
    public IAsyncRelayCommand ConfirmDeleteCommand { get; }
    public IRelayCommand CancelDeleteCommand { get; }
    public IAsyncRelayCommand OpenPopupCommand { get; }
    public IRelayCommand ClosePopupCommand { get; }
    public IRelayCommand ZoomInCommand { get; }
    public IRelayCommand ZoomOutCommand { get; }

    public bool HasImages => Images.Count > 0;
    public double ZoomFactor => ZoomPercent / 100d;

    public async Task OpenPopupAsync(Guid productId, bool canManage)
    {
        CanManage = canManage;
        IsPopupOpen = true;
        RequestPopupOpen(nameof(IsPopupOpen));
        ZoomPercent = 100;

        if (ProductId == productId)
        {
            await RefreshAsync();
        }
        else
        {
            await SetProductAsync(productId);
        }
    }

    public async Task SetProductAsync(Guid? productId)
    {
        if (ProductId == productId && (productId is null || Images.Count > 0))
        {
            return;
        }

        _imagesLoadCancellation?.Cancel();
        _contentLoadCancellation?.Cancel();
        CancelDelete();
        ProductId = productId;

        if (productId is null)
        {
            ClearImages();
            return;
        }

        await RefreshAsync();
    }

    public async Task RefreshAsync(Guid? preferredImageId = null)
    {
        if (ProductId is not Guid productId)
        {
            ClearImages();
            return;
        }

        _imagesLoadCancellation?.Cancel();
        var cancellation = new CancellationTokenSource();
        _imagesLoadCancellation = cancellation;

        try
        {
            IsBusy = true;
            await YieldToUiAsync();
            var result = await _mediator.Send(
                new GetProductImagesQuery(productId),
                cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            if (ProductId != productId)
            {
                return;
            }

            var previousImageId = SelectedImage?.Id;
            Images.Clear();
            foreach (var image in result)
            {
                Images.Add(image);
            }

            SelectedImage = Images.FirstOrDefault(x => x.Id == preferredImageId) ??
                            Images.FirstOrDefault(x => x.Id == previousImageId) ??
                            Images.FirstOrDefault();
            NotifyImagesChanged();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (RequestValidationException exception)
        {
            if (ProductId == productId)
            {
                ClearImages();
                ApplyValidationErrors(exception);
            }
        }
        finally
        {
            if (ReferenceEquals(_imagesLoadCancellation, cancellation))
            {
                IsBusy = false;
            }
        }
    }

    public async Task UploadFilesAsync(IReadOnlyList<string> filePaths)
    {
        if (!CanManage || ProductId is not Guid productId || filePaths.Count == 0)
        {
            return;
        }

        ClearValidationErrors();
        IsBusy = true;
        try
        {
            var uploads = new List<ProductImageUpload>(filePaths.Count);
            foreach (var filePath in filePaths)
            {
                var extension = Path.GetExtension(filePath);
                if (!ContentTypesByExtension.TryGetValue(extension, out var contentType))
                {
                    throw new InvalidDataException("Obsługiwane formaty zdjęć to JPEG, PNG, GIF, BMP i TIFF.");
                }

                var content = await File.ReadAllBytesAsync(filePath);
                EnsureImageCanBeDecoded(content);
                uploads.Add(new ProductImageUpload(Path.GetFileName(filePath), contentType, content));
            }

            var createdIds = await _mediator.Send(new AddProductImagesCommand(productId, uploads));
            await RefreshAsync(createdIds.LastOrDefault());
        }
        catch (RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public Task<ProductImageContentDto> GetOriginalImageAsync(
        Guid imageId,
        CancellationToken cancellationToken = default) =>
        _mediator.Send(new GetProductImageContentQuery(imageId), cancellationToken);

    private void OpenDeleteConfirmation()
    {
        _imageToDelete = SelectedImage;
        IsDeleteConfirmationOpen = _imageToDelete is not null;
        if (IsDeleteConfirmationOpen)
        {
            RequestPopupOpen(nameof(IsDeleteConfirmationOpen));
        }
    }

    private void CancelDelete()
    {
        IsDeleteConfirmationOpen = false;
        _imageToDelete = null;
    }

    private async Task DeleteSelectedImageAsync()
    {
        if (!CanManage || _imageToDelete is not { } image)
        {
            return;
        }

        ClearValidationErrors();
        IsBusy = true;
        try
        {
            await _mediator.Send(new DeleteProductImageCommand(image.Id));
            IsDeleteConfirmationOpen = false;
            _imageToDelete = null;
            await RefreshAsync();
        }
        catch (RequestValidationException exception)
        {
            ApplyValidationErrors(exception);
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedImageChanged(ProductImageDto? value)
    {
        ZoomPercent = 100;
        UpdatePositionText();
        PreviousImageCommand.NotifyCanExecuteChanged();
        NextImageCommand.NotifyCanExecuteChanged();
        OpenDeleteConfirmationCommand.NotifyCanExecuteChanged();
        NotifyZoomCommandStates();
        _ = LoadSelectedImageContentAsync(value);
    }

    partial void OnCanManageChanged(bool value)
    {
        OpenDeleteConfirmationCommand.NotifyCanExecuteChanged();
        OpenPopupCommand.NotifyCanExecuteChanged();
    }

    partial void OnProductIdChanged(Guid? value) =>
        OpenPopupCommand.NotifyCanExecuteChanged();

    partial void OnZoomPercentChanged(int value)
    {
        OnPropertyChanged(nameof(ZoomFactor));
        NotifyZoomCommandStates();
    }

    private async Task LoadSelectedImageContentAsync(ProductImageDto? image)
    {
        _contentLoadCancellation?.Cancel();

        if (image is null)
        {
            ImageSource = null;
            return;
        }

        var cancellation = new CancellationTokenSource();
        _contentLoadCancellation = cancellation;

        try
        {
            var result = await GetOriginalImageAsync(image.Id, cancellation.Token);
            cancellation.Token.ThrowIfCancellationRequested();

            if (SelectedImage?.Id != image.Id)
            {
                return;
            }

            ImageSource = CreateBitmap(result.Content);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (RequestValidationException exception)
        {
            if (SelectedImage?.Id == image.Id)
            {
                ImageSource = null;
                ApplyValidationErrors(exception);
            }
        }
    }

    private void ShowPreviousImage()
    {
        var index = SelectedImage is null ? -1 : Images.IndexOf(SelectedImage);
        if (index >= 0)
        {
            SelectedImage = Images[(index - 1 + Images.Count) % Images.Count];
        }
    }

    private void ShowNextImage()
    {
        var index = SelectedImage is null ? -1 : Images.IndexOf(SelectedImage);
        if (index >= 0)
        {
            SelectedImage = Images[(index + 1) % Images.Count];
        }
    }

    private bool CanMoveBetweenImages() => Images.Count > 1 && SelectedImage is not null;

    private bool CanDeleteImage() => CanManage && SelectedImage is not null;

    private async Task OpenCurrentProductPopupAsync()
    {
        if (ProductId is null)
        {
            return;
        }

        IsPopupOpen = true;
        RequestPopupOpen(nameof(IsPopupOpen));
        ZoomPercent = 100;
        await RefreshAsync();
    }

    private bool CanOpenPopup() => ProductId is not null && (CanManage || HasImages);

    private void ClosePopup()
    {
        CancelDelete();
        IsPopupOpen = false;
        ZoomPercent = 100;
    }

    private void ZoomIn() => ZoomPercent = Math.Min(MaxZoomPercent, ZoomPercent + ZoomStepPercent);

    private void ZoomOut() => ZoomPercent = Math.Max(MinZoomPercent, ZoomPercent - ZoomStepPercent);

    private bool CanZoomIn() => HasImages && ZoomPercent < MaxZoomPercent;

    private bool CanZoomOut() => HasImages && ZoomPercent > MinZoomPercent;

    private void ClearImages()
    {
        Images.Clear();
        SelectedImage = null;
        ImageSource = null;
        NotifyImagesChanged();
    }

    private void NotifyImagesChanged()
    {
        OnPropertyChanged(nameof(HasImages));
        UpdatePositionText();
        PreviousImageCommand.NotifyCanExecuteChanged();
        NextImageCommand.NotifyCanExecuteChanged();
        OpenDeleteConfirmationCommand.NotifyCanExecuteChanged();
        OpenPopupCommand.NotifyCanExecuteChanged();
        NotifyZoomCommandStates();
    }

    private void NotifyZoomCommandStates()
    {
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
    }

    private void UpdatePositionText()
    {
        var index = SelectedImage is null ? -1 : Images.IndexOf(SelectedImage);
        ImagePositionText = index < 0 ? string.Empty : $"{index + 1} z {Images.Count}";
    }

    private static void EnsureImageCanBeDecoded(byte[] content) => _ = CreateBitmap(content);

    private static BitmapImage CreateBitmap(byte[] content)
    {
        using var stream = new MemoryStream(content, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
