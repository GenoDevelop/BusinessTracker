using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// DataGrid with reusable clipboard and column configuration menus. Users can copy
/// displayed cell values, selected rows and visible headers, reorder columns by
/// dragging their headers or through the context menu, hide/show columns and
/// restore the initial layout declared by the view.
/// </summary>
public class ConfigurableDataGrid : DataGrid
{
    private IReadOnlyList<InitialColumnLayout>? _initialLayout;
    private DataGridCell? _contextCell;
    private DataGridColumn? _contextColumn;
    private object? _contextItem;
    private ColumnDragPayload? _dragCandidate;
    private MenuItem? _activeVisibilityMenu;
    private Point _dragStartPoint;
    private bool _contextIsColumnHeader;
    private bool _isColumnMenuDragging;
    private bool _contextMenuStaysOpenBeforeDrag;
    private bool _hasLoadedPersistedLayout;
    private bool _isApplyingLayout;

    public ConfigurableDataGrid()
    {
        CanUserReorderColumns = true;
        ContextMenu = new ContextMenu();
        ContextMenu.AddHandler(
            Mouse.MouseUpEvent,
            new MouseButtonEventHandler(OnContextMenuMouseUp),
            handledEventsToo: true);
        ContextMenu.Closed += (_, _) => FinishColumnMenuDrag();
        ContextMenuOpening += OnContextMenuOpening;
        PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ColumnDisplayIndexChanged += OnColumnDisplayIndexChanged;
    }

    public static readonly DependencyProperty LayoutKeyProperty =
        DependencyProperty.Register(
            nameof(LayoutKey),
            typeof(string),
            typeof(ConfigurableDataGrid),
            new PropertyMetadata(null));

    public string? LayoutKey
    {
        get => (string?)GetValue(LayoutKeyProperty);
        set => SetValue(LayoutKeyProperty, value);
    }

    public event EventHandler<ConfigurableDataGridColumnVisibilityChangedEventArgs>?
        ColumnVisibilityChanged;

    public bool IsColumnVisible(string columnKey) =>
        Columns.Any(column =>
            string.Equals(GetPersistenceKey(column), columnKey, StringComparison.Ordinal) &&
            column.Visibility == Visibility.Visible);

    public static readonly DependencyProperty ColumnKeyProperty =
        DependencyProperty.RegisterAttached(
            "ColumnKey",
            typeof(string),
            typeof(ConfigurableDataGrid),
            new PropertyMetadata(null));

    public static void SetColumnKey(DependencyObject element, string? value) =>
        element.SetValue(ColumnKeyProperty, value);

    public static string? GetColumnKey(DependencyObject element) =>
        (string?)element.GetValue(ColumnKeyProperty);

    public static readonly DependencyProperty ColumnTitleProperty =
        DependencyProperty.RegisterAttached(
            "ColumnTitle",
            typeof(string),
            typeof(ConfigurableDataGrid),
            new PropertyMetadata(null));

    public static void SetColumnTitle(DependencyObject element, string? value) =>
        element.SetValue(ColumnTitleProperty, value);

    public static string? GetColumnTitle(DependencyObject element) =>
        (string?)element.GetValue(ColumnTitleProperty);

    public static readonly DependencyProperty IsColumnConfigurableProperty =
        DependencyProperty.RegisterAttached(
            "IsColumnConfigurable",
            typeof(bool),
            typeof(ConfigurableDataGrid),
            new PropertyMetadata(true));

    public static void SetIsColumnConfigurable(DependencyObject element, bool value) =>
        element.SetValue(IsColumnConfigurableProperty, value);

    public static bool GetIsColumnConfigurable(DependencyObject element) =>
        (bool)element.GetValue(IsColumnConfigurableProperty);

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_initialLayout is null)
        {
            _initialLayout = Columns
                .Select((column, index) => new InitialColumnLayout(
                    column,
                    column.DisplayIndex >= 0 ? column.DisplayIndex : index,
                    column.Visibility))
                .OrderBy(item => item.DisplayIndex)
                .ToArray();
        }

        if (!_hasLoadedPersistedLayout)
        {
            _hasLoadedPersistedLayout = true;
            ApplyPersistedLayout();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => SaveCurrentLayout();

    private void OnColumnDisplayIndexChanged(object? sender, DataGridColumnEventArgs e)
    {
        if (IsLoaded && !_isApplyingLayout)
        {
            SaveCurrentLayout();
        }
    }

    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        SetContextTarget(e.OriginalSource as DependencyObject);

        if (_contextCell is null || _contextItem is null)
        {
            return;
        }

        var row = FindAncestor<DataGridRow>(_contextCell);
        if (row is null)
        {
            return;
        }

        if (!row.IsSelected)
        {
            SelectedItems.Clear();
            row.IsSelected = true;
        }

        CurrentCell = new DataGridCellInfo(row.Item, _contextCell.Column);
    }

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        var sourceCell = FindAncestor<DataGridCell>(source);
        var sourceHeader = FindAncestor<DataGridColumnHeader>(source);
        if (sourceCell is not null || sourceHeader is not null)
        {
            SetContextTarget(source);
        }
        else if (e.CursorLeft < 0 && e.CursorTop < 0 && CurrentCell.IsValid)
        {
            _contextCell = null;
            _contextColumn = CurrentCell.Column;
            _contextItem = CurrentCell.Item;
            _contextIsColumnHeader = false;
        }

        BuildContextMenu();
    }

    private void SetContextTarget(DependencyObject? source)
    {
        _contextCell = FindAncestor<DataGridCell>(source);
        var columnHeader = FindAncestor<DataGridColumnHeader>(source);
        _contextColumn = _contextCell?.Column ?? columnHeader?.Column;
        _contextItem = _contextCell?.DataContext;
        _contextIsColumnHeader = columnHeader is not null;
    }

    private void BuildContextMenu()
    {
        if (ContextMenu is null)
        {
            return;
        }

        ContextMenu.Items.Clear();

        if (_contextItem is not null && _contextColumn is not null &&
            !_contextIsColumnHeader && IsCopyableColumn(_contextColumn))
        {
            AddCellCopyMenuItems();
            ContextMenu.Items.Add(new Separator());
        }
        else if (_contextIsColumnHeader)
        {
            if (_contextColumn is not null && IsCopyableColumn(_contextColumn))
            {
                var copyHeaderItem = new MenuItem { Header = "Kopiuj nagłówek" };
                copyHeaderItem.Click += (_, _) => CopyHeader();
                ContextMenu.Items.Add(copyHeaderItem);
            }

            var copyHeadersItem = new MenuItem
            {
                Header = "Kopiuj nagłówki (CSV)",
                IsEnabled = GetCopyableVisibleColumns().Count > 0
            };
            copyHeadersItem.Click += (_, _) => CopyHeadersAsCsv();
            ContextMenu.Items.Add(copyHeadersItem);
            ContextMenu.Items.Add(new Separator());
        }

        BuildColumnLayoutMenu();
    }

    private void AddCellCopyMenuItems()
    {
        var copyCellItem = new MenuItem { Header = "Kopiuj" };
        copyCellItem.Click += (_, _) => CopyCell();
        ContextMenu!.Items.Add(copyCellItem);

        var selectedRowsCount = GetRowsForCopy().Count;
        var copyRowsItem = new MenuItem
        {
            Header = selectedRowsCount > 1
                ? "Kopiuj wiersze jako CSV"
                : "Kopiuj wiersz jako CSV"
        };
        copyRowsItem.Click += (_, _) => CopyRowsAsCsv();
        ContextMenu.Items.Add(copyRowsItem);
    }

    private void BuildColumnLayoutMenu()
    {
        if (ContextMenu is null)
        {
            return;
        }

        ContextMenu.Items.Add(new MenuItem
        {
            Header = "Układ kolumn",
            IsEnabled = false,
            FontWeight = FontWeights.Bold
        });

        var configurableColumns = Columns
            .Where(GetIsColumnConfigurable)
            .OrderBy(column => column.DisplayIndex)
            .ToArray();
        var visibleCount = configurableColumns.Count(column => column.Visibility == Visibility.Visible);

        var visibilityMenu = new MenuItem
        {
            Header = "Widoczność i kolejność kolumn",
            StaysOpenOnClick = true
        };
        var visibilityItems = new List<(DataGridColumn Column, MenuItem Item)>();
        foreach (var column in configurableColumns)
        {
            var columnItem = new MenuItem
            {
                Header = GetDisplayTitle(column),
                IsCheckable = true,
                IsChecked = column.Visibility == Visibility.Visible,
                IsEnabled = column.Visibility != Visibility.Visible || visibleCount > 1,
                StaysOpenOnClick = true
            };
            visibilityItems.Add((column, columnItem));
            columnItem.Click += (_, _) =>
            {
                var newVisibility = columnItem.IsChecked
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                var visibilityChanged = column.Visibility != newVisibility;
                column.Visibility = newVisibility;
                UpdateVisibilityItems(visibilityItems);
                SaveCurrentLayout();
                if (visibilityChanged)
                {
                    OnColumnVisibilityChanged([column]);
                }
            };
            ConfigureColumnDrag(columnItem, column, visibilityMenu);
            visibilityMenu.Items.Add(columnItem);
        }
        ContextMenu.Items.Add(visibilityMenu);

        if (_contextColumn is not null && GetIsColumnConfigurable(_contextColumn))
        {
            ContextMenu.Items.Add(new Separator());

            var orderedColumns = configurableColumns.OrderBy(column => column.DisplayIndex).ToArray();
            var position = Array.IndexOf(orderedColumns, _contextColumn);

            var moveLeftItem = new MenuItem
            {
                Header = "Przesuń w lewo",
                IsEnabled = position > 0
            };
            moveLeftItem.Click += (_, _) => MoveColumn(_contextColumn, -1);
            ContextMenu.Items.Add(moveLeftItem);

            var moveRightItem = new MenuItem
            {
                Header = "Przesuń w prawo",
                IsEnabled = position >= 0 && position < orderedColumns.Length - 1
            };
            moveRightItem.Click += (_, _) => MoveColumn(_contextColumn, 1);
            ContextMenu.Items.Add(moveRightItem);

            var hideItem = new MenuItem
            {
                Header = "Ukryj tę kolumnę",
                IsEnabled = _contextColumn.Visibility == Visibility.Visible && visibleCount > 1
            };
            hideItem.Click += (_, _) =>
            {
                _contextColumn.Visibility = Visibility.Collapsed;
                SaveCurrentLayout();
                OnColumnVisibilityChanged([_contextColumn]);
            };
            ContextMenu.Items.Add(hideItem);
        }

        ContextMenu.Items.Add(new Separator());
        var resetItem = new MenuItem
        {
            Header = "Przywróć układ początkowy",
            IsEnabled = _initialLayout is not null
        };
        resetItem.Click += (_, _) => RestoreInitialLayout();
        ContextMenu.Items.Add(resetItem);
    }

    private void CopyCell()
    {
        if (_contextItem is null || _contextColumn is null ||
            !IsCopyableColumn(_contextColumn))
        {
            return;
        }

        Clipboard.SetDataObject(GetDisplayedCellText(_contextColumn, _contextItem), true);
    }

    private void CopyHeader()
    {
        if (_contextColumn is null || !IsCopyableColumn(_contextColumn))
        {
            return;
        }

        Clipboard.SetDataObject(GetDisplayTitle(_contextColumn), true);
    }

    private void CopyRowsAsCsv()
    {
        var rows = GetRowsForCopy();
        var columns = GetCopyableVisibleColumns();
        if (rows.Count == 0 || columns.Count == 0)
        {
            return;
        }

        var csv = string.Join(
            "\r\n",
            rows.Select(row => string.Join(",", columns.Select(column =>
                EscapeCsv(GetDisplayedCellText(column, row))))));
        SetCsvClipboard(csv);
    }

    private void CopyHeadersAsCsv()
    {
        var columns = GetCopyableVisibleColumns();
        if (columns.Count == 0)
        {
            return;
        }

        SetCsvClipboard(
            string.Join(",", columns.Select(column => EscapeCsv(GetDisplayTitle(column)))));
    }

    private IReadOnlyList<DataGridColumn> GetCopyableVisibleColumns() =>
        Columns
            .Where(column => column.Visibility == Visibility.Visible && IsCopyableColumn(column))
            .OrderBy(column => column.DisplayIndex)
            .ToArray();

    private static bool IsCopyableColumn(DataGridColumn column) =>
        !string.Equals(GetColumnKey(column), "Actions", StringComparison.Ordinal);

    private IReadOnlyList<object> GetRowsForCopy()
    {
        if (_contextItem is null)
        {
            return [];
        }

        var selectedRows = SelectedItems
            .Cast<object>()
            .ToHashSet(ReferenceEqualityComparer.Instance);
        if (!selectedRows.Contains(_contextItem))
        {
            return [_contextItem];
        }

        return Items
            .Cast<object>()
            .Where(selectedRows.Contains)
            .ToArray();
    }

    private string GetDisplayedCellText(DataGridColumn column, object item)
    {
        var realizedContent = column.GetCellContent(item);
        if (realizedContent is not null)
        {
            return ExtractVisibleText(realizedContent);
        }

        if (column is DataGridTemplateColumn templateColumn)
        {
            var presenter = new ContentPresenter
            {
                Content = item,
                ContentTemplate = templateColumn.CellTemplate,
                ContentTemplateSelector = templateColumn.CellTemplateSelector,
                Language = Language,
                FlowDirection = FlowDirection
            };
            presenter.ApplyTemplate();
            presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            presenter.Arrange(new Rect(presenter.DesiredSize));
            presenter.UpdateLayout();
            return ExtractVisibleText(presenter);
        }

        if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is not null)
        {
            var textBlock = new TextBlock { DataContext = item };
            BindingOperations.SetBinding(textBlock, TextBlock.TextProperty, boundColumn.Binding);
            return textBlock.Text;
        }

        var clipboardContent = column.OnCopyingCellClipboardContent(item);
        return clipboardContent?.ToString() ?? string.Empty;
    }

    private static string ExtractVisibleText(DependencyObject element)
    {
        if (element is UIElement { Visibility: not Visibility.Visible })
        {
            return string.Empty;
        }

        if (element is TextBlock textBlock)
        {
            var text = new StringBuilder();
            foreach (var inline in textBlock.Inlines)
            {
                AppendInlineText(text, inline);
            }

            return text.ToString();
        }

        if (element is TextBox textBox)
        {
            return textBox.Text;
        }

        var childrenText = new StringBuilder();
        var visualChildrenCount = element is Visual or Visual3D
            ? VisualTreeHelper.GetChildrenCount(element)
            : 0;
        for (var index = 0; index < visualChildrenCount; index++)
        {
            var child = VisualTreeHelper.GetChild(element, index);
            var text = ExtractVisibleText(child);
            if (text.Length == 0)
            {
                continue;
            }

            if (element is StackPanel { Orientation: Orientation.Horizontal } &&
                child is FrameworkElement { Margin.Left: > 0 } &&
                childrenText.Length > 0 &&
                !char.IsWhiteSpace(childrenText[^1]) &&
                !char.IsWhiteSpace(text[0]))
            {
                childrenText.Append(' ');
            }

            childrenText.Append(text);
        }

        if (visualChildrenCount == 0 && element is ContentControl { Content: string content })
        {
            childrenText.Append(content);
        }

        return childrenText.ToString();
    }

    private static void AppendInlineText(StringBuilder text, Inline inline)
    {
        switch (inline)
        {
            case Run run:
                text.Append(run.Text);
                break;
            case LineBreak:
                text.AppendLine();
                break;
            case Span span:
                foreach (var child in span.Inlines)
                {
                    AppendInlineText(text, child);
                }
                break;
            case InlineUIContainer { Child: DependencyObject child }:
                text.Append(ExtractVisibleText(child));
                break;
        }
    }

    private static string EscapeCsv(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') &&
            !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void SetCsvClipboard(string csv)
    {
        var data = new DataObject();
        data.SetData(DataFormats.UnicodeText, csv);
        data.SetData(DataFormats.CommaSeparatedValue, csv);
        Clipboard.SetDataObject(data, true);
    }

    private static void UpdateVisibilityItems(
        IReadOnlyCollection<(DataGridColumn Column, MenuItem Item)> visibilityItems)
    {
        var visibleCount = visibilityItems.Count(item => item.Column.Visibility == Visibility.Visible);
        foreach (var (column, item) in visibilityItems)
        {
            item.IsChecked = column.Visibility == Visibility.Visible;
            item.IsEnabled = column.Visibility != Visibility.Visible || visibleCount > 1;
        }
    }

    private void ConfigureColumnDrag(
        MenuItem columnItem,
        DataGridColumn column,
        MenuItem visibilityMenu)
    {
        columnItem.PreviewMouseLeftButtonDown += (_, e) =>
        {
            _dragStartPoint = e.GetPosition(this);
            _dragCandidate = new ColumnDragPayload(column, columnItem);
        };
        columnItem.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed ||
                _dragCandidate is not { })
            {
                return;
            }

            var currentPoint = e.GetPosition(this);
            if (!_isColumnMenuDragging &&
                Math.Abs(currentPoint.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPoint.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (!_isColumnMenuDragging)
            {
                BeginColumnMenuDrag(visibilityMenu);
            }

            if (!ReferenceEquals(_dragCandidate.Column, column))
            {
                var insertBefore = e.GetPosition(columnItem).Y < columnItem.ActualHeight / 2;
                ShowDropIndicator(visibilityMenu, columnItem, insertBefore);
            }
            else
            {
                ClearDropIndicators(visibilityMenu);
            }

            e.Handled = true;
        };
        columnItem.PreviewMouseLeftButtonUp += (_, e) =>
        {
            if (!_isColumnMenuDragging || _dragCandidate is not { } payload)
            {
                _dragCandidate = null;
                return;
            }

            if (!ReferenceEquals(payload.Column, column))
            {
                var insertBefore = e.GetPosition(columnItem).Y < columnItem.ActualHeight / 2;
                ReorderColumn(payload, column, columnItem, visibilityMenu, insertBefore);
            }

            e.Handled = true;
            FinishColumnMenuDrag();
        };
    }

    private void BeginColumnMenuDrag(MenuItem visibilityMenu)
    {
        _isColumnMenuDragging = true;
        _activeVisibilityMenu = visibilityMenu;
        if (ContextMenu is not null)
        {
            _contextMenuStaysOpenBeforeDrag = ContextMenu.StaysOpen;
            ContextMenu.StaysOpen = true;
        }
    }

    private void OnContextMenuMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isColumnMenuDragging)
        {
            FinishColumnMenuDrag();
            e.Handled = true;
        }
    }

    private void FinishColumnMenuDrag()
    {
        if (_activeVisibilityMenu is not null)
        {
            ClearDropIndicators(_activeVisibilityMenu);
        }

        _dragCandidate = null;
        _activeVisibilityMenu = null;
        _isColumnMenuDragging = false;
        if (ContextMenu is not null)
        {
            ContextMenu.StaysOpen = _contextMenuStaysOpenBeforeDrag;
        }
    }

    private void ReorderColumn(
        ColumnDragPayload payload,
        DataGridColumn targetColumn,
        MenuItem targetItem,
        MenuItem visibilityMenu,
        bool insertBefore)
    {
        var orderedColumns = Columns
            .Where(GetIsColumnConfigurable)
            .OrderBy(column => column.DisplayIndex)
            .ToList();
        if (!orderedColumns.Remove(payload.Column))
        {
            return;
        }

        var targetIndex = orderedColumns.IndexOf(targetColumn);
        if (targetIndex < 0)
        {
            return;
        }

        var insertionIndex = insertBefore ? targetIndex : targetIndex + 1;
        orderedColumns.Insert(insertionIndex, payload.Column);
        ApplyConfigurableColumnOrder(orderedColumns);

        visibilityMenu.Items.Remove(payload.MenuItem);
        var targetMenuIndex = visibilityMenu.Items.IndexOf(targetItem);
        var menuInsertionIndex = insertBefore ? targetMenuIndex : targetMenuIndex + 1;
        visibilityMenu.Items.Insert(menuInsertionIndex, payload.MenuItem);
        SaveCurrentLayout();
    }

    private void ApplyConfigurableColumnOrder(IReadOnlyList<DataGridColumn> orderedColumns)
    {
        var displayIndexes = Columns
            .Where(GetIsColumnConfigurable)
            .Select(column => column.DisplayIndex)
            .OrderBy(index => index)
            .ToArray();

        _isApplyingLayout = true;
        try
        {
            for (var index = 0; index < orderedColumns.Count; index++)
            {
                orderedColumns[index].DisplayIndex = displayIndexes[index];
            }
        }
        finally
        {
            _isApplyingLayout = false;
        }
    }

    private static void ShowDropIndicator(
        MenuItem visibilityMenu,
        MenuItem targetItem,
        bool insertBefore)
    {
        ClearDropIndicators(visibilityMenu);
        targetItem.BorderBrush = SystemColors.HighlightBrush;
        targetItem.BorderThickness = insertBefore
            ? new Thickness(0, 2, 0, 0)
            : new Thickness(0, 0, 0, 2);
    }

    private static void ClearDropIndicators(MenuItem visibilityMenu)
    {
        foreach (var item in visibilityMenu.Items.OfType<MenuItem>())
        {
            item.ClearValue(BorderBrushProperty);
            item.ClearValue(BorderThicknessProperty);
        }
    }

    private void MoveColumn(DataGridColumn column, int offset)
    {
        var orderedColumns = Columns
            .Where(GetIsColumnConfigurable)
            .OrderBy(item => item.DisplayIndex)
            .ToArray();
        var currentPosition = Array.IndexOf(orderedColumns, column);
        var targetPosition = currentPosition + offset;
        if (currentPosition < 0 || targetPosition < 0 || targetPosition >= orderedColumns.Length)
        {
            return;
        }

        column.DisplayIndex = orderedColumns[targetPosition].DisplayIndex;
        SaveCurrentLayout();
    }

    private void RestoreInitialLayout(
        bool persist = true,
        bool notifyVisibilityChange = true)
    {
        if (_initialLayout is null)
        {
            return;
        }

        var visibilityChangedColumns = new List<DataGridColumn>();
        _isApplyingLayout = true;
        try
        {
            foreach (var item in _initialLayout.OrderBy(item => item.DisplayIndex))
            {
                item.Column.DisplayIndex = item.DisplayIndex;
                if (item.Column.Visibility != item.Visibility)
                {
                    visibilityChangedColumns.Add(item.Column);
                }
                item.Column.Visibility = item.Visibility;
            }
        }
        finally
        {
            _isApplyingLayout = false;
        }

        if (persist)
        {
            SaveCurrentLayout();
        }

        if (notifyVisibilityChange && visibilityChangedColumns.Count > 0)
        {
            OnColumnVisibilityChanged(visibilityChangedColumns);
        }
    }

    private void ApplyPersistedLayout()
    {
        if (string.IsNullOrWhiteSpace(LayoutKey) || _initialLayout is null)
        {
            return;
        }

        var persistedLayout = GridLayoutStorage.Load(LayoutKey);
        if (persistedLayout?.Columns is not { Count: > 0 })
        {
            return;
        }

        var configurableColumns = Columns
            .Where(GetIsColumnConfigurable)
            .OrderBy(GetInitialDisplayIndex)
            .ToArray();
        var previousVisibility = configurableColumns.ToDictionary(
            column => column,
            column => column.Visibility);
        var columnsByKey = configurableColumns
            .Select(column => (Column: column, Key: GetPersistenceKey(column)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key!, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single().Column, StringComparer.Ordinal);
        var persistedByKey = persistedLayout.Columns
            .Where(item => !string.IsNullOrWhiteSpace(item.ColumnKey))
            .GroupBy(item => item.ColumnKey, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);

        var persistedKnownColumns = persistedByKey.Values
            .Where(item => columnsByKey.ContainsKey(item.ColumnKey))
            .OrderBy(item => item.DisplayIndex)
            .ThenBy(item => GetInitialDisplayIndex(columnsByKey[item.ColumnKey]))
            .Select(item => columnsByKey[item.ColumnKey])
            .ToArray();
        var persistedKnownSet = persistedKnownColumns.ToHashSet();
        var finalOrder = persistedKnownColumns.ToList();
        foreach (var newColumn in configurableColumns
                     .Where(column => !persistedKnownSet.Contains(column))
                     .OrderBy(GetInitialDisplayIndex))
        {
            var initialDisplayIndex = GetInitialDisplayIndex(newColumn);
            var insertionIndex = finalOrder.FindIndex(column =>
                GetInitialDisplayIndex(column) > initialDisplayIndex);
            if (insertionIndex < 0)
            {
                finalOrder.Add(newColumn);
            }
            else
            {
                finalOrder.Insert(insertionIndex, newColumn);
            }
        }
        var configurableSlots = configurableColumns
            .Select(GetInitialDisplayIndex)
            .OrderBy(index => index)
            .ToArray();

        _isApplyingLayout = true;
        try
        {
            for (var index = 0; index < finalOrder.Count; index++)
            {
                finalOrder[index].DisplayIndex = configurableSlots[index];
            }

            foreach (var (columnKey, column) in columnsByKey)
            {
                if (persistedByKey.TryGetValue(columnKey, out var persistedColumn))
                {
                    var persistedVisibility = persistedColumn.IsVisible
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                    column.Visibility = persistedVisibility;
                }
            }

            EnsureAtLeastOneConfigurableColumnIsVisible(configurableColumns);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            RestoreInitialLayout(persist: false, notifyVisibilityChange: false);
        }
        finally
        {
            _isApplyingLayout = false;
        }

        var visibilityChangedColumns = configurableColumns
            .Where(column => previousVisibility[column] != column.Visibility)
            .ToArray();
        if (visibilityChangedColumns.Length > 0)
        {
            OnColumnVisibilityChanged(visibilityChangedColumns);
        }
    }

    private void SaveCurrentLayout()
    {
        if (string.IsNullOrWhiteSpace(LayoutKey) || _isApplyingLayout)
        {
            return;
        }

        var persistableColumns = Columns
            .Where(GetIsColumnConfigurable)
            .Select(column => (Column: column, Key: GetPersistenceKey(column)))
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key!, StringComparer.Ordinal)
            .Where(group => group.Count() == 1)
            .Select(group => group.Single())
            .OrderBy(item => item.Column.DisplayIndex)
            .Select(item => new GridColumnLayoutState
            {
                ColumnKey = item.Key!,
                DisplayIndex = item.Column.DisplayIndex,
                IsVisible = item.Column.Visibility == Visibility.Visible
            })
            .ToList();

        if (persistableColumns.Count == 0)
        {
            return;
        }

        GridLayoutStorage.Save(LayoutKey, new GridLayoutState { Columns = persistableColumns });
    }

    private int GetInitialDisplayIndex(DataGridColumn column) =>
        _initialLayout?.FirstOrDefault(item => ReferenceEquals(item.Column, column))?.DisplayIndex
        ?? column.DisplayIndex;

    private bool EnsureAtLeastOneConfigurableColumnIsVisible(IReadOnlyList<DataGridColumn> columns)
    {
        if (columns.Any(column => column.Visibility == Visibility.Visible))
        {
            return false;
        }

        var visibilityChanged = false;
        foreach (var column in columns)
        {
            var initialVisibility = _initialLayout?
                .FirstOrDefault(item => ReferenceEquals(item.Column, column))?
                .Visibility;
            var restoredVisibility = initialVisibility ?? Visibility.Visible;
            visibilityChanged |= column.Visibility != restoredVisibility;
            column.Visibility = restoredVisibility;
        }

        if (columns.Count > 0 && columns.All(column => column.Visibility != Visibility.Visible))
        {
            visibilityChanged |= columns[0].Visibility != Visibility.Visible;
            columns[0].Visibility = Visibility.Visible;
        }

        return visibilityChanged;
    }

    private void OnColumnVisibilityChanged(IReadOnlyCollection<DataGridColumn> columns) =>
        ColumnVisibilityChanged?.Invoke(
            this,
            new ConfigurableDataGridColumnVisibilityChangedEventArgs(
                columns.Any(column => column.Header is IColumnFilterHeader { HasActiveFilter: true })));

    private static string? GetPersistenceKey(DataGridColumn column)
    {
        var configuredKey = GetColumnKey(column);
        if (!string.IsNullOrWhiteSpace(configuredKey))
        {
            return configuredKey;
        }

        if (!string.IsNullOrWhiteSpace(column.SortMemberPath))
        {
            return column.SortMemberPath;
        }

        var title = GetDisplayTitle(column);
        return string.IsNullOrWhiteSpace(title) ? null : $"Header:{title}";
    }

    private static string GetDisplayTitle(DataGridColumn column)
    {
        var configuredTitle = GetColumnTitle(column);
        if (!string.IsNullOrWhiteSpace(configuredTitle))
        {
            return configuredTitle;
        }

        if (column.Header is string text)
        {
            return text;
        }

        if (column.Header is TextBlock textBlock && !string.IsNullOrWhiteSpace(textBlock.Text))
        {
            return textBlock.Text;
        }

        var headerProperty = column.Header?.GetType().GetProperty(
            "Header",
            BindingFlags.Instance | BindingFlags.Public);
        if (headerProperty?.GetValue(column.Header) is string header && !string.IsNullOrWhiteSpace(header))
        {
            return header;
        }

        return string.IsNullOrWhiteSpace(column.SortMemberPath)
            ? "Kolumna"
            : column.SortMemberPath;
    }

    private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T result)
            {
                return result;
            }

            source = source is Visual or Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return null;
    }

    private sealed record InitialColumnLayout(
        DataGridColumn Column,
        int DisplayIndex,
        Visibility Visibility);

    private sealed record ColumnDragPayload(
        DataGridColumn Column,
        MenuItem MenuItem);
}

public sealed class ConfigurableDataGridColumnVisibilityChangedEventArgs(
    bool affectsActiveFilter) : EventArgs
{
    public bool AffectsActiveFilter { get; } = affectsActiveFilter;
}
