using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GenoDev.BusinessTracker.Wpf.Infrastructure.Controls;

public partial class PaginationControl : UserControl
{
    private enum LoadOutcome
    {
        Success,
        Failed,
        Superseded,
        MissingLoader
    }

    private static readonly IReadOnlyList<int> DefaultAvailablePageSizes =
        Array.AsReadOnly(new[] { 5, 10, 20, 50 });

    private static readonly DependencyPropertyKey PageIndexPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(PageIndex),
            typeof(int),
            typeof(PaginationControl),
            new PropertyMetadata(0));

    private static readonly DependencyPropertyKey PageSizePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(PageSize),
            typeof(int),
            typeof(PaginationControl),
            new PropertyMetadata(20));

    private static readonly DependencyPropertyKey TotalCountPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(TotalCount),
            typeof(int),
            typeof(PaginationControl),
            new PropertyMetadata(0));

    private static readonly DependencyPropertyKey DisplayPagePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(DisplayPage),
            typeof(int),
            typeof(PaginationControl),
            new PropertyMetadata(1));

    private static readonly DependencyPropertyKey TotalPagesPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(TotalPages),
            typeof(int),
            typeof(PaginationControl),
            new PropertyMetadata(0));

    private static readonly DependencyPropertyKey ItemsRangePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(ItemsRange),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("0-0 / 0"));

    private static readonly DependencyPropertyKey CanGoPreviousPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanGoPrevious),
            typeof(bool),
            typeof(PaginationControl),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey CanGoNextPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanGoNext),
            typeof(bool),
            typeof(PaginationControl),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey CanChangePageSizePropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(CanChangePageSize),
            typeof(bool),
            typeof(PaginationControl),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey IsLoadingPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(IsLoading),
            typeof(bool),
            typeof(PaginationControl),
            new PropertyMetadata(false));

    private static readonly DependencyPropertyKey LastLoadExceptionPropertyKey =
        DependencyProperty.RegisterReadOnly(
            nameof(LastLoadException),
            typeof(Exception),
            typeof(PaginationControl),
            new PropertyMetadata(null));

    public static readonly DependencyProperty PageIndexProperty =
        PageIndexPropertyKey.DependencyProperty;

    public static readonly DependencyProperty PageSizeProperty =
        PageSizePropertyKey.DependencyProperty;

    public static readonly DependencyProperty TotalCountProperty =
        TotalCountPropertyKey.DependencyProperty;

    public static readonly DependencyProperty DisplayPageProperty =
        DisplayPagePropertyKey.DependencyProperty;

    public static readonly DependencyProperty TotalPagesProperty =
        TotalPagesPropertyKey.DependencyProperty;

    public static readonly DependencyProperty ItemsRangeProperty =
        ItemsRangePropertyKey.DependencyProperty;

    public static readonly DependencyProperty CanGoPreviousProperty =
        CanGoPreviousPropertyKey.DependencyProperty;

    public static readonly DependencyProperty CanGoNextProperty =
        CanGoNextPropertyKey.DependencyProperty;

    public static readonly DependencyProperty CanChangePageSizeProperty =
        CanChangePageSizePropertyKey.DependencyProperty;

    public static readonly DependencyProperty IsLoadingProperty =
        IsLoadingPropertyKey.DependencyProperty;

    public static readonly DependencyProperty LastLoadExceptionProperty =
        LastLoadExceptionPropertyKey.DependencyProperty;

    public static readonly DependencyProperty PageLoaderProperty =
        DependencyProperty.Register(
            nameof(PageLoader),
            typeof(PaginationPageLoader),
            typeof(PaginationControl),
            new PropertyMetadata(null, OnPageLoaderChanged));

    public static readonly DependencyProperty AutoLoadProperty =
        DependencyProperty.Register(
            nameof(AutoLoad),
            typeof(bool),
            typeof(PaginationControl),
            new PropertyMetadata(true, OnAutoLoadChanged));

    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(
            nameof(Layout),
            typeof(PaginationLayout),
            typeof(PaginationControl),
            new PropertyMetadata(PaginationLayout.SingleLine));

    public static readonly DependencyProperty InitialPageSizeProperty =
        DependencyProperty.Register(
            nameof(InitialPageSize),
            typeof(int),
            typeof(PaginationControl),
            new PropertyMetadata(20, OnInitialPageSizeChanged, CoerceInitialPageSize));

    public static readonly DependencyProperty NavigationIconBrushProperty =
        DependencyProperty.Register(
            nameof(NavigationIconBrush),
            typeof(Brush),
            typeof(PaginationControl),
            new PropertyMetadata(Brushes.DodgerBlue));

    public static readonly DependencyProperty PaginationHorizontalAlignmentProperty =
        DependencyProperty.Register(
            nameof(PaginationHorizontalAlignment),
            typeof(HorizontalAlignment),
            typeof(PaginationControl),
            new PropertyMetadata(HorizontalAlignment.Right));

    public static readonly DependencyProperty PageSizeLabelProperty =
        DependencyProperty.Register(
            nameof(PageSizeLabel),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("Na stronie:"));

    public static readonly DependencyProperty ItemsRangeLabelProperty =
        DependencyProperty.Register(
            nameof(ItemsRangeLabel),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("Pozycje:"));

    public static readonly DependencyProperty CompactItemsRangeLabelProperty =
        DependencyProperty.Register(
            nameof(CompactItemsRangeLabel),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("Poz.:"));

    public static readonly DependencyProperty PageLabelProperty =
        DependencyProperty.Register(
            nameof(PageLabel),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("Strona"));

    public static readonly DependencyProperty OfLabelProperty =
        DependencyProperty.Register(
            nameof(OfLabel),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("z"));

    public static readonly DependencyProperty PreviousToolTipProperty =
        DependencyProperty.Register(
            nameof(PreviousToolTip),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("Poprzednia"));

    public static readonly DependencyProperty NextToolTipProperty =
        DependencyProperty.Register(
            nameof(NextToolTip),
            typeof(string),
            typeof(PaginationControl),
            new PropertyMetadata("Następna"));

    private CancellationTokenSource? _activeLoadCancellation;
    private long _loadRequestVersion;
    private bool _autoLoadExecuted;

    public PaginationControl()
    {
        AvailablePageSizes = DefaultAvailablePageSizes;

        InitializeComponent();

        SetPageSize(InitialPageSize, resetPageIndex: false);
        UpdateCalculatedValues();

        Loaded += PaginationControl_Loaded;
        Unloaded += PaginationControl_Unloaded;
    }

    public IReadOnlyList<int> AvailablePageSizes { get; }

    /// <summary>
    /// Callback responsible for loading data for the supplied state and returning TotalCount.
    /// </summary>
    public PaginationPageLoader? PageLoader
    {
        get => (PaginationPageLoader?)GetValue(PageLoaderProperty);
        set => SetValue(PageLoaderProperty, value);
    }

    /// <summary>
    /// Automatically performs the first load when the control becomes loaded.
    /// </summary>
    public bool AutoLoad
    {
        get => (bool)GetValue(AutoLoadProperty);
        set => SetValue(AutoLoadProperty, value);
    }

    public PaginationLayout Layout
    {
        get => (PaginationLayout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public int InitialPageSize
    {
        get => (int)GetValue(InitialPageSizeProperty);
        set => SetValue(InitialPageSizeProperty, value);
    }

    public int PageIndex => (int)GetValue(PageIndexProperty);

    public int PageSize => (int)GetValue(PageSizeProperty);

    /// <summary>
    /// Read-only result of the most recent successful load.
    /// </summary>
    public int TotalCount => (int)GetValue(TotalCountProperty);

    public int DisplayPage => (int)GetValue(DisplayPageProperty);

    public int TotalPages => (int)GetValue(TotalPagesProperty);

    public string ItemsRange => (string)GetValue(ItemsRangeProperty);

    public bool CanGoPrevious => (bool)GetValue(CanGoPreviousProperty);

    public bool CanGoNext => (bool)GetValue(CanGoNextProperty);

    public bool CanChangePageSize => (bool)GetValue(CanChangePageSizeProperty);

    public bool IsLoading => (bool)GetValue(IsLoadingProperty);

    /// <summary>
    /// Last exception raised by PageLoader. Cleared before the next request.
    /// </summary>
    public Exception? LastLoadException =>
        (Exception?)GetValue(LastLoadExceptionProperty);

    public Brush NavigationIconBrush
    {
        get => (Brush)GetValue(NavigationIconBrushProperty);
        set => SetValue(NavigationIconBrushProperty, value);
    }

    public HorizontalAlignment PaginationHorizontalAlignment
    {
        get => (HorizontalAlignment)GetValue(PaginationHorizontalAlignmentProperty);
        set => SetValue(PaginationHorizontalAlignmentProperty, value);
    }

    public string PageSizeLabel
    {
        get => (string)GetValue(PageSizeLabelProperty);
        set => SetValue(PageSizeLabelProperty, value);
    }

    public string ItemsRangeLabel
    {
        get => (string)GetValue(ItemsRangeLabelProperty);
        set => SetValue(ItemsRangeLabelProperty, value);
    }

    public string CompactItemsRangeLabel
    {
        get => (string)GetValue(CompactItemsRangeLabelProperty);
        set => SetValue(CompactItemsRangeLabelProperty, value);
    }

    public string PageLabel
    {
        get => (string)GetValue(PageLabelProperty);
        set => SetValue(PageLabelProperty, value);
    }

    public string OfLabel
    {
        get => (string)GetValue(OfLabelProperty);
        set => SetValue(OfLabelProperty, value);
    }

    public string PreviousToolTip
    {
        get => (string)GetValue(PreviousToolTipProperty);
        set => SetValue(PreviousToolTipProperty, value);
    }

    public string NextToolTip
    {
        get => (string)GetValue(NextToolTipProperty);
        set => SetValue(NextToolTipProperty, value);
    }

    public PaginationState GetState() => new(PageIndex, PageSize);

    /// <summary>
    /// Reloads the current page. Returns false when loading failed or no loader is assigned.
    /// </summary>
    public async Task<bool> RefreshAsync()
    {
        var outcome = await LoadCurrentPageAsync(allowPageCorrection: true);
        return outcome == LoadOutcome.Success;
    }

    public void RequestRefresh()
    {
        _ = RefreshAsync();
    }

    /// <summary>
    /// Resets the page to zero and reloads data.
    /// Useful after changing filters or sorting.
    /// </summary>
    public async Task<bool> ResetAndRefreshAsync()
    {
        SetPageIndex(0);
        return await RefreshAsync();
    }

    /// <summary>
    /// Resets the page without loading data.
    /// </summary>
    public void ResetPageIndex()
    {
        SetPageIndex(0);
    }

    private static void OnPageLoaderChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (PaginationControl)dependencyObject;

        control.CancelActiveLoad();
        control._autoLoadExecuted = false;
        control.SetValue(LastLoadExceptionPropertyKey, null);
        control.UpdateCalculatedValues();
        control.TryAutoLoad();
    }

    private static void OnAutoLoadChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (PaginationControl)dependencyObject;

        if ((bool)e.NewValue)
        {
            control.TryAutoLoad();
        }
    }

    private static void OnInitialPageSizeChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (PaginationControl)dependencyObject;
        control.SetPageSize((int)e.NewValue, resetPageIndex: true);
    }

    private static object CoerceInitialPageSize(
        DependencyObject dependencyObject,
        object baseValue)
    {
        var requestedValue = Math.Max(1, (int)baseValue);

        return DefaultAvailablePageSizes.Contains(requestedValue)
            ? requestedValue
            : 20;
    }

    private async void PaginationControl_Loaded(object sender, RoutedEventArgs e)
    {
        await TryAutoLoadAsync();
    }

    private void PaginationControl_Unloaded(object sender, RoutedEventArgs e)
    {
        CancelActiveLoad();
    }

    private async void PreviousButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanGoPrevious)
        {
            return;
        }

        await ChangePageAndLoadAsync(PageIndex - 1);
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CanGoNext)
        {
            return;
        }

        await ChangePageAndLoadAsync(PageIndex + 1);
    }

    private async void PageSizeComboBox_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedItem: int selectedPageSize } ||
            selectedPageSize == PageSize ||
            !CanChangePageSize)
        {
            return;
        }

        var previousState = GetState();
        var previousTotalCount = TotalCount;

        SetPageSize(selectedPageSize, resetPageIndex: true);

        var outcome = await LoadCurrentPageAsync(allowPageCorrection: true);
        RollBackStateAfterFailedLoad(outcome, previousState, previousTotalCount);
    }

    private async Task ChangePageAndLoadAsync(int pageIndex)
    {
        var previousState = GetState();
        var previousTotalCount = TotalCount;

        SetPageIndex(pageIndex);

        var outcome = await LoadCurrentPageAsync(allowPageCorrection: true);
        RollBackStateAfterFailedLoad(outcome, previousState, previousTotalCount);
    }

    private void RollBackStateAfterFailedLoad(
        LoadOutcome outcome,
        PaginationState previousState,
        int previousTotalCount)
    {
        // A superseded request must not restore stale state over a newer request.
        // If TotalCount changed, a successful first request already established a newer range.
        if (outcome != LoadOutcome.Failed || TotalCount != previousTotalCount)
        {
            return;
        }

        SetPageSize(previousState.PageSize, resetPageIndex: false);
        SetPageIndex(previousState.PageIndex);
    }

    private void TryAutoLoad()
    {
        if (!IsLoaded)
        {
            return;
        }

        _ = TryAutoLoadAsync();
    }

    private async Task TryAutoLoadAsync()
    {
        if (!AutoLoad ||
            _autoLoadExecuted ||
            PageLoader is null)
        {
            return;
        }

        _autoLoadExecuted = true;
        await LoadCurrentPageAsync(allowPageCorrection: true);
    }

    private async Task<LoadOutcome> LoadCurrentPageAsync(bool allowPageCorrection)
    {
        var loader = PageLoader;
        if (loader is null)
        {
            UpdateCalculatedValues();
            return LoadOutcome.MissingLoader;
        }

        var requestVersion = ++_loadRequestVersion;
        var cancellation = new CancellationTokenSource();
        var previousCancellation = _activeLoadCancellation;

        _activeLoadCancellation = cancellation;
        previousCancellation?.Cancel();

        SetValue(LastLoadExceptionPropertyKey, null);
        SetValue(IsLoadingPropertyKey, true);
        UpdateCalculatedValues();

        try
        {
            var requestedState = GetState();
            var totalCount = await loader(requestedState, cancellation.Token);

            if (requestVersion != _loadRequestVersion)
            {
                return LoadOutcome.Superseded;
            }

            var pageIndexCorrected = ApplyTotalCount(totalCount);

            // Data may have been deleted between requests. If the requested page no longer
            // exists, move to the last valid page and load it once more.
            if (pageIndexCorrected && allowPageCorrection)
            {
                return await LoadCurrentPageAsync(allowPageCorrection: false);
            }

            return LoadOutcome.Success;
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return LoadOutcome.Superseded;
        }
        catch (Exception exception)
        {
            if (requestVersion == _loadRequestVersion)
            {
                SetValue(LastLoadExceptionPropertyKey, exception);
            }

            return LoadOutcome.Failed;
        }
        finally
        {
            if (requestVersion == _loadRequestVersion)
            {
                _activeLoadCancellation = null;
                SetValue(IsLoadingPropertyKey, false);
                UpdateCalculatedValues();
            }

            cancellation.Dispose();
        }
    }

    private bool ApplyTotalCount(int totalCount)
    {
        SetValue(TotalCountPropertyKey, Math.Max(0, totalCount));

        var previousPageIndex = PageIndex;
        CoercePageIndexToAvailableRange();
        UpdateCalculatedValues();

        return PageIndex != previousPageIndex;
    }

    private void CancelActiveLoad()
    {
        // Invalidate the request even if its loader ignores cancellation.
        ++_loadRequestVersion;

        var cancellation = _activeLoadCancellation;
        _activeLoadCancellation = null;
        cancellation?.Cancel();

        SetValue(IsLoadingPropertyKey, false);
        UpdateCalculatedValues();
    }

    private void SetPageIndex(int pageIndex)
    {
        var maximumPageIndex = Math.Max(0, CalculateTotalPages() - 1);
        var coercedPageIndex = Math.Min(Math.Max(pageIndex, 0), maximumPageIndex);

        SetValue(PageIndexPropertyKey, coercedPageIndex);
        UpdateCalculatedValues();
    }

    private void SetPageSize(int pageSize, bool resetPageIndex)
    {
        var normalizedPageSize = DefaultAvailablePageSizes.Contains(pageSize)
            ? pageSize
            : 20;

        SetValue(PageSizePropertyKey, normalizedPageSize);

        if (resetPageIndex)
        {
            SetValue(PageIndexPropertyKey, 0);
        }

        UpdateCalculatedValues();
    }

    private void CoercePageIndexToAvailableRange()
    {
        var maximumPageIndex = Math.Max(0, CalculateTotalPages() - 1);

        if (PageIndex > maximumPageIndex)
        {
            SetValue(PageIndexPropertyKey, maximumPageIndex);
        }
    }

    private int CalculateTotalPages()
    {
        return PageSize <= 0
            ? 0
            : (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    private void UpdateCalculatedValues()
    {
        var totalPages = CalculateTotalPages();
        var displayPage = totalPages == 0 ? 1 : PageIndex + 1;
        var hasLoader = PageLoader is not null;
        var interactionEnabled = hasLoader && !IsLoading;

        string itemsRange;
        if (TotalCount == 0)
        {
            itemsRange = "0-0 / 0";
        }
        else
        {
            var start = PageIndex * PageSize + 1;
            var end = Math.Min((PageIndex + 1) * PageSize, TotalCount);
            itemsRange = $"{start}-{end} / {TotalCount}";
        }

        SetValue(DisplayPagePropertyKey, displayPage);
        SetValue(TotalPagesPropertyKey, totalPages);
        SetValue(ItemsRangePropertyKey, itemsRange);
        SetValue(CanGoPreviousPropertyKey, interactionEnabled && PageIndex > 0);
        SetValue(CanGoNextPropertyKey, interactionEnabled && PageIndex + 1 < totalPages);
        SetValue(CanChangePageSizePropertyKey, interactionEnabled);
    }
}