using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// DataGrid with a reusable column configuration menu. Users can reorder columns
/// by dragging their headers or through the context menu, hide/show columns and
/// restore the initial layout declared by the view.
/// </summary>
public class ConfigurableDataGrid : DataGrid
{
    private IReadOnlyList<InitialColumnLayout>? _initialLayout;
    private DataGridColumn? _contextColumn;
    private ColumnDragPayload? _dragCandidate;
    private MenuItem? _activeVisibilityMenu;
    private Point _dragStartPoint;
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

    private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _contextColumn = FindAncestor<DataGridColumnHeader>(e.OriginalSource as DependencyObject)?.Column;
        BuildContextMenu();
    }

    private void BuildContextMenu()
    {
        if (ContextMenu is null)
        {
            return;
        }

        ContextMenu.Items.Clear();

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
        var finalOrder = persistedKnownColumns
            .Concat(configurableColumns.Where(column => !persistedKnownSet.Contains(column)))
            .ToArray();
        var configurableSlots = configurableColumns
            .Select(GetInitialDisplayIndex)
            .OrderBy(index => index)
            .ToArray();

        _isApplyingLayout = true;
        try
        {
            for (var index = 0; index < finalOrder.Length; index++)
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
