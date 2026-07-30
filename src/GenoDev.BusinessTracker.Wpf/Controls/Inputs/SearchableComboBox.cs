using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// ComboBox z polem wyszukiwania umieszczonym wyłącznie w otwartym popupie.
///
/// Wpisany tekst jest dzielony po białych znakach. Jeżeli cały tekst jest
/// ujęty w cudzysłowy, jest traktowany jako jedna fraza bez cudzysłowów.
/// Każda fraza musi wystąpić w co najmniej jednym z przeszukiwanych pól.
/// Porównanie nie rozróżnia wielkości liter.
///
/// Kontrolka nie używa IsEditable. Dzięki temu ComboBox nie synchronizuje
/// wpisywanej frazy z SelectedItem i nie nadpisuje tekstu wyszukiwania nazwą
/// aktualnie zaznaczonego elementu.
/// </summary>
[TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
public class SearchableComboBox : ComboBox
{
    private const string PopupPartName = "PART_Popup";

    static SearchableComboBox()
    {
        // IsEditable nie może zostać ponownie włączone przez lokalny styl lub XAML.
        // Pole wyszukiwania jest osobną kontrolką wewnątrz popupu.
        IsEditableProperty.OverrideMetadata(
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.None,
                null,
                CoerceBooleanFalse));

        IsTextSearchEnabledProperty.OverrideMetadata(
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(
                false,
                FrameworkPropertyMetadataOptions.None,
                null,
                CoerceBooleanFalse));
    }

    private static readonly ConcurrentDictionary<(Type Type, string Path), Func<object, object?>>
        PropertyAccessorCache = new();

    private readonly DispatcherTimer _debounceTimer;
    private readonly IValueConverter _generatedDisplayConverter;

    private Popup? _popup;
    private DockPanel? _popupWrapper;
    private UIElement? _originalPopupChild;
    private Border? _popupBorderHost;
    private UIElement? _originalBorderContent;
    private FrameworkElement? _popupWidthHost;
    private BindingBase? _originalPopupWidthBinding;
    private object _originalPopupWidthLocalValue = DependencyProperty.UnsetValue;
    private TextBox? _searchTextBox;
    private DataTemplate? _generatedItemTemplate;
    private string[] _searchTerms = Array.Empty<string>();
    private string[] _additionalSearchPaths = Array.Empty<string>();
    private bool _isResettingSearch;

    public SearchableComboBox()
    {
        _generatedDisplayConverter = new GeneratedDisplayConverter(this);

        _debounceTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(DebounceMilliseconds)
        };
        _debounceTimer.Tick += DebounceTimer_Tick;

        Unloaded += SearchableComboBox_Unloaded;
    }

    /// <summary>
    /// Opóźnienie filtrowania po wpisaniu tekstu. Domyślnie 500 ms.
    /// </summary>
    public int DebounceMilliseconds
    {
        get => (int)GetValue(DebounceMillisecondsProperty);
        set => SetValue(DebounceMillisecondsProperty, value);
    }

    public static readonly DependencyProperty DebounceMillisecondsProperty =
        DependencyProperty.Register(
            nameof(DebounceMilliseconds),
            typeof(int),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(
                500,
                OnDebounceMillisecondsChanged,
                CoerceDebounceMilliseconds));

    /// <summary>
    /// Ścieżka wartości będącej tekstem wyświetlanym i przeszukiwanym.
    ///
    /// Gdy właściwość jest pusta, kontrolka używa DisplayMemberPath, a następnie
    /// ToString() całego elementu.
    /// </summary>
    public string? DisplayTextPath
    {
        get => (string?)GetValue(DisplayTextPathProperty);
        set => SetValue(DisplayTextPathProperty, value);
    }

    public static readonly DependencyProperty DisplayTextPathProperty =
        DependencyProperty.Register(
            nameof(DisplayTextPath),
            typeof(string),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, OnDisplayConfigurationChanged));

    /// <summary>
    /// Dodatkowe ścieżki właściwości przeszukiwane oprócz tekstu wyświetlanego.
    /// Ścieżki rozdzielaj przecinkami lub średnikami, np. "Ean;ManufacturerCode".
    /// Obsługiwane są także ścieżki zagnieżdżone, np. "Supplier.Code".
    /// </summary>
    public string? SearchPropertyPaths
    {
        get => (string?)GetValue(SearchPropertyPathsProperty);
        set => SetValue(SearchPropertyPathsProperty, value);
    }

    public static readonly DependencyProperty SearchPropertyPathsProperty =
        DependencyProperty.Register(
            nameof(SearchPropertyPaths),
            typeof(string),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, OnSearchPropertyPathsChanged));

    /// <summary>
    /// Konwerter używany do ustalenia tekstu wyświetlanego i przeszukiwanego.
    /// Przy własnym ItemTemplate przekaż ten sam konwerter, którego szablon
    /// używa do prezentacji nazwy.
    /// </summary>
    public IValueConverter? DisplayTextConverter
    {
        get => (IValueConverter?)GetValue(DisplayTextConverterProperty);
        set => SetValue(DisplayTextConverterProperty, value);
    }

    public static readonly DependencyProperty DisplayTextConverterProperty =
        DependencyProperty.Register(
            nameof(DisplayTextConverter),
            typeof(IValueConverter),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, OnDisplayConfigurationChanged));

    public object? DisplayTextConverterParameter
    {
        get => GetValue(DisplayTextConverterParameterProperty);
        set => SetValue(DisplayTextConverterParameterProperty, value);
    }

    public static readonly DependencyProperty DisplayTextConverterParameterProperty =
        DependencyProperty.Register(
            nameof(DisplayTextConverterParameter),
            typeof(object),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, OnDisplayConfigurationChanged));

    /// <summary>
    /// Opcjonalny styl pola wyszukiwania znajdującego się w popupie.
    /// </summary>
    public Style? SearchTextBoxStyle
    {
        get => (Style?)GetValue(SearchTextBoxStyleProperty);
        set => SetValue(SearchTextBoxStyleProperty, value);
    }

    public static readonly DependencyProperty SearchTextBoxStyleProperty =
        DependencyProperty.Register(
            nameof(SearchTextBoxStyle),
            typeof(Style),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(null, OnSearchTextBoxStyleChanged));

    /// <summary>
    /// Wysokość pola wyszukiwania wewnątrz popupu. Domyślnie 24 px.
    /// </summary>
    public double SearchTextBoxHeight
    {
        get => (double)GetValue(SearchTextBoxHeightProperty);
        set => SetValue(SearchTextBoxHeightProperty, value);
    }

    public static readonly DependencyProperty SearchTextBoxHeightProperty =
        DependencyProperty.Register(
            nameof(SearchTextBoxHeight),
            typeof(double),
            typeof(SearchableComboBox),
            new FrameworkPropertyMetadata(
                24d,
                OnSearchTextBoxHeightChanged,
                CoerceSearchTextBoxHeight));

    public override void OnApplyTemplate()
    {
        DetachSearchTextBoxFromPopup();
        base.OnApplyTemplate();

        _popup = GetTemplateChild(PopupPartName) as Popup;
        AttachSearchTextBoxToPopup();
        UpdateGeneratedItemTemplate();
    }

    protected override void OnDropDownOpened(EventArgs e)
    {
        // Najpierw czyścimy poprzedni filtr, a dopiero potem oddajemy focus
        // niezależnemu polu tekstowemu.
        ResetSearch();
        base.OnDropDownOpened(e);

        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            new Action(() =>
            {
                if (!IsDropDownOpen || _searchTextBox is null)
                {
                    return;
                }

                _searchTextBox.Focus();
                Keyboard.Focus(_searchTextBox);
            }));
    }

    protected override void OnDropDownClosed(EventArgs e)
    {
        _debounceTimer.Stop();
        ResetSearch();
        base.OnDropDownClosed(e);
    }

    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        // Gwarantuje otwieranie po kliknięciu w dowolne miejsce zamkniętej
        // kontrolki, niezależnie od użytego globalnego template'u ComboBoxa.
        if (!IsDropDownOpen && IsEnabled && e.ChangedButton == MouseButton.Left)
        {
            Focus();
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
            return;
        }

        base.OnPreviewMouseLeftButtonDown(e);
    }

    protected override void PrepareContainerForItemOverride(
        DependencyObject element,
        object item)
    {
        base.PrepareContainerForItemOverride(element, item);
        UpdateContainerVisibility(element as ComboBoxItem, item);
    }

    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnItemsChanged(e);

        if (IsDropDownOpen)
        {
            Dispatcher.BeginInvoke(
                DispatcherPriority.Loaded,
                new Action(RefreshItemVisibility));
        }
    }

    private static object CoerceBooleanFalse(
        DependencyObject dependencyObject,
        object baseValue)
    {
        return false;
    }

    private static object CoerceDebounceMilliseconds(
        DependencyObject dependencyObject,
        object baseValue)
    {
        return Math.Max(0, (int)baseValue);
    }

    private static object CoerceSearchTextBoxHeight(
        DependencyObject dependencyObject,
        object baseValue)
    {
        var value = (double)baseValue;
        return double.IsNaN(value) || double.IsInfinity(value)
            ? 24d
            : Math.Max(18d, value);
    }

    private static void OnDebounceMillisecondsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableComboBox)dependencyObject;
        control._debounceTimer.Interval = TimeSpan.FromMilliseconds((int)e.NewValue);
    }

    private static void OnSearchPropertyPathsChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableComboBox)dependencyObject;
        control._additionalSearchPaths = ParsePropertyPaths((string?)e.NewValue);

        if (control.IsDropDownOpen)
        {
            control.RefreshItemVisibility();
        }
    }

    private static void OnDisplayConfigurationChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableComboBox)dependencyObject;
        control.UpdateGeneratedItemTemplate();

        if (control.IsDropDownOpen)
        {
            control.RefreshItemVisibility();
        }
    }

    private static void OnSearchTextBoxStyleChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableComboBox)dependencyObject;

        if (control._searchTextBox is not null)
        {
            control._searchTextBox.Style = (Style?)e.NewValue;
        }
    }

    private static void OnSearchTextBoxHeightChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var control = (SearchableComboBox)dependencyObject;

        if (control._searchTextBox is not null)
        {
            control._searchTextBox.Height = (double)e.NewValue;
        }
    }

    private static string[] ParsePropertyPaths(string? paths)
    {
        return string.IsNullOrWhiteSpace(paths)
            ? Array.Empty<string>()
            : paths.Split(
                new[] { ',', ';', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private void AttachSearchTextBoxToPopup()
    {
        if (_popup?.Child is not UIElement popupChild)
        {
            return;
        }

        _originalPopupChild = popupChild;

        _searchTextBox = new TextBox
        {
            Height = SearchTextBoxHeight,
            MinHeight = 0,
            Margin = new Thickness(0),
            Padding = new Thickness(6, 1, 6, 1),
            VerticalContentAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Style = SearchTextBoxStyle
        };
        _searchTextBox.TextChanged += SearchTextBox_TextChanged;
        _searchTextBox.PreviewKeyDown += SearchTextBox_PreviewKeyDown;
        DockPanel.SetDock(_searchTextBox, Dock.Top);

        _popupWrapper = new DockPanel
        {
            LastChildFill = true,
            Margin = new Thickness(0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        // W wielu motywach root popupu jest Gridem albo chrome'em z zapasem
        // na cień. Szerokość trzeba narzucić wewnętrznemu Borderowi listy,
        // a nie zewnętrznemu rootowi popupu — inaczej prawa część listy może
        // zostać pomniejszona o margines przeznaczony na cień.
        var popupContentBorder = FindPopupContentBorder(popupChild);
        if (popupContentBorder?.Child is UIElement borderContent)
        {
            _popupBorderHost = popupContentBorder;
            _originalBorderContent = borderContent;

            _searchTextBox.BorderThickness = new Thickness(0, 0, 0, 1);

            popupContentBorder.Child = null;
            _popupWrapper.Children.Add(_searchTextBox);
            _popupWrapper.Children.Add(borderContent);
            popupContentBorder.Child = _popupWrapper;

            SetPopupWidthHost(popupContentBorder);
            return;
        }

        // Fallback dla niestandardowych template'ów bez Border jako root popupu.
        _searchTextBox.BorderThickness = new Thickness(1, 1, 1, 0);

        _popup.Child = null;
        _popupWrapper.Children.Add(_searchTextBox);
        _popupWrapper.Children.Add(popupChild);
        _popup.Child = _popupWrapper;

        SetPopupWidthHost(_popupWrapper);
    }

    private Border? FindPopupContentBorder(UIElement popupChild)
    {
        // Najpierw próbujemy typowych nazw używanych przez template'y WPF
        // i biblioteki motywów. Sprawdzamy potomka popupu, aby nie trafić
        // na Border zamkniętej części ComboBoxa.
        foreach (var partName in new[] { "DropDownBorder", "PART_DropDownBorder" })
        {
            if (Template?.FindName(partName, this) is Border namedBorder &&
                namedBorder.Child is UIElement &&
                IsVisualDescendantOrSelf(popupChild, namedBorder))
            {
                return namedBorder;
            }
        }

        Border? firstBorderWithContent = null;
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(popupChild);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current is Border border && border.Child is UIElement)
            {
                firstBorderWithContent ??= border;

                if (border.Name.Contains("DropDown", StringComparison.OrdinalIgnoreCase) ||
                    ContainsItemsHost(border.Child))
                {
                    return border;
                }
            }

            foreach (var child in GetVisualChildren(current))
            {
                queue.Enqueue(child);
            }
        }

        return firstBorderWithContent;
    }

    private static bool ContainsItemsHost(DependencyObject root)
    {
        var queue = new Queue<DependencyObject>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (current is ItemsPresenter or ScrollViewer)
            {
                return true;
            }

            foreach (var child in GetVisualChildren(current))
            {
                queue.Enqueue(child);
            }
        }

        return false;
    }

    private static bool IsVisualDescendantOrSelf(
        DependencyObject root,
        DependencyObject candidate)
    {
        DependencyObject? current = candidate;

        while (current is not null)
        {
            if (ReferenceEquals(current, root))
            {
                return true;
            }

            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : null;
        }

        return false;
    }

    private static IEnumerable<DependencyObject> GetVisualChildren(
        DependencyObject parent)
    {
        if (parent is not Visual &&
            parent is not System.Windows.Media.Media3D.Visual3D)
        {
            yield break;
        }

        var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childrenCount; index++)
        {
            yield return VisualTreeHelper.GetChild(parent, index);
        }
    }

    private void SetPopupWidthHost(FrameworkElement widthHost)
    {
        _popupWidthHost = widthHost;
        _originalPopupWidthBinding = BindingOperations.GetBindingBase(
            widthHost,
            WidthProperty);
        _originalPopupWidthLocalValue = widthHost.ReadLocalValue(WidthProperty);

        widthHost.SetBinding(
            WidthProperty,
            new Binding(nameof(ActualWidth))
            {
                Source = this,
                Mode = BindingMode.OneWay
            });
    }

    private void RestorePopupWidthHost()
    {
        if (_popupWidthHost is null)
        {
            return;
        }

        BindingOperations.ClearBinding(_popupWidthHost, WidthProperty);

        if (_originalPopupWidthBinding is not null)
        {
            BindingOperations.SetBinding(
                _popupWidthHost,
                WidthProperty,
                _originalPopupWidthBinding);
        }
        else if (_originalPopupWidthLocalValue != DependencyProperty.UnsetValue)
        {
            _popupWidthHost.SetValue(
                WidthProperty,
                _originalPopupWidthLocalValue);
        }
        else
        {
            _popupWidthHost.ClearValue(WidthProperty);
        }

        _popupWidthHost = null;
        _originalPopupWidthBinding = null;
        _originalPopupWidthLocalValue = DependencyProperty.UnsetValue;
    }

    private void DetachSearchTextBoxFromPopup()
    {
        _debounceTimer.Stop();

        if (_searchTextBox is not null)
        {
            _searchTextBox.TextChanged -= SearchTextBox_TextChanged;
            _searchTextBox.PreviewKeyDown -= SearchTextBox_PreviewKeyDown;
        }

        RestorePopupWidthHost();

        if (_popupBorderHost is not null &&
            _popupWrapper is not null &&
            _originalBorderContent is not null &&
            ReferenceEquals(_popupBorderHost.Child, _popupWrapper))
        {
            _popupBorderHost.Child = null;
            _popupWrapper.Children.Remove(_originalBorderContent);
            _popupBorderHost.Child = _originalBorderContent;
        }
        else if (_popup is not null &&
                 _popupWrapper is not null &&
                 _originalPopupChild is not null &&
                 ReferenceEquals(_popup.Child, _popupWrapper))
        {
            _popup.Child = null;
            _popupWrapper.Children.Remove(_originalPopupChild);
            _popup.Child = _originalPopupChild;
        }

        _searchTextBox = null;
        _popupWrapper = null;
        _originalPopupChild = null;
        _popupBorderHost = null;
        _originalBorderContent = null;
        _popup = null;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsDropDownOpen || _isResettingSearch)
        {
            return;
        }

        _debounceTimer.Stop();

        if (DebounceMilliseconds == 0)
        {
            ApplySearchText(_searchTextBox?.Text);
            return;
        }

        _debounceTimer.Start();
    }

    private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
                break;

            case Key.Down:
                if (FocusFirstVisibleItem())
                {
                    e.Handled = true;
                }
                break;

            case Key.Enter:
                if (SelectFirstVisibleItem())
                {
                    e.Handled = true;
                }
                break;
        }
    }

    private void DebounceTimer_Tick(object? sender, EventArgs e)
    {
        _debounceTimer.Stop();
        ApplySearchText(_searchTextBox?.Text);
    }

    private void ApplySearchText(string? searchText)
    {
        _searchTerms = ParseSearchTerms(searchText);
        RefreshItemVisibility();
    }

    private static string[] ParseSearchTerms(string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return Array.Empty<string>();
        }

        var trimmedText = searchText.Trim();

        // Cudzysłowy mają specjalne znaczenie tylko wtedy, gdy obejmują cały
        // wpisany tekst. Przykład: "Zielona ka" => jeden token: Zielona ka.
        if (trimmedText.Length >= 2 &&
            trimmedText[0] == '"' &&
            trimmedText[^1] == '"')
        {
            var quotedPhrase = trimmedText[1..^1].Trim();
            return quotedPhrase.Length == 0
                ? Array.Empty<string>()
                : new[] { quotedPhrase };
        }

        return trimmedText.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private void ResetSearch()
    {
        _debounceTimer.Stop();
        _searchTerms = Array.Empty<string>();

        _isResettingSearch = true;
        try
        {
            if (_searchTextBox is not null && _searchTextBox.Text.Length > 0)
            {
                _searchTextBox.Clear();
            }
        }
        finally
        {
            _isResettingSearch = false;
        }

        RefreshItemVisibility();
    }

    private void RefreshItemVisibility()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            var item = Items[index];
            var container = ItemContainerGenerator.ContainerFromIndex(index) as ComboBoxItem;
            UpdateContainerVisibility(container, item);
        }
    }

    private void UpdateContainerVisibility(ComboBoxItem? container, object item)
    {
        if (container is null)
        {
            return;
        }

        var searchableItem = item is ComboBoxItem comboBoxItem
            ? comboBoxItem.Content ?? comboBoxItem.DataContext ?? comboBoxItem
            : item;

        container.SetCurrentValue(
            VisibilityProperty,
            MatchesCurrentFilter(searchableItem)
                ? Visibility.Visible
                : Visibility.Collapsed);
    }

    private bool MatchesCurrentFilter(object? item)
    {
        if (_searchTerms.Length == 0)
        {
            return true;
        }

        if (item is null)
        {
            return false;
        }

        var searchableValues = GetSearchableValues(item);

        // AND między frazami. Poszczególne frazy mogą znajdować się
        // w różnych polach tego samego obiektu.
        return _searchTerms.All(term =>
            searchableValues.Any(value =>
                value.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private IReadOnlyList<string> GetSearchableValues(object item)
    {
        var values = new List<string>(_additionalSearchPaths.Length + 1);

        AddNonEmpty(values, GetDisplayText(item));

        foreach (var propertyPath in _additionalSearchPaths)
        {
            var value = GetPropertyValue(item, propertyPath);
            AddNonEmpty(values, ConvertValueToString(value));
        }

        return values;
    }

    private string GetDisplayText(object? item)
    {
        if (item is null)
        {
            return string.Empty;
        }

        var displayPath = !string.IsNullOrWhiteSpace(DisplayTextPath)
            ? DisplayTextPath
            : DisplayMemberPath;

        var rawValue = string.IsNullOrWhiteSpace(displayPath)
            ? item
            : GetPropertyValue(item, displayPath);

        if (DisplayTextConverter is null)
        {
            return ConvertValueToString(rawValue);
        }

        var convertedValue = DisplayTextConverter.Convert(
            rawValue,
            typeof(string),
            DisplayTextConverterParameter,
            CultureInfo.CurrentCulture);

        return convertedValue == DependencyProperty.UnsetValue ||
               convertedValue == Binding.DoNothing
            ? ConvertValueToString(rawValue)
            : ConvertValueToString(convertedValue);
    }

    private static object? GetPropertyValue(object item, string propertyPath)
    {
        if (string.IsNullOrWhiteSpace(propertyPath))
        {
            return item;
        }

        var accessor = PropertyAccessorCache.GetOrAdd(
            (item.GetType(), propertyPath),
            key => CreatePropertyAccessor(key.Type, key.Path));

        return accessor(item);
    }

    private static Func<object, object?> CreatePropertyAccessor(
        Type sourceType,
        string propertyPath)
    {
        var segments = propertyPath.Split(
            '.',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var properties = new List<PropertyInfo>(segments.Length);
        var currentType = sourceType;

        foreach (var segment in segments)
        {
            var property = currentType.GetProperty(
                segment,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (property is null || property.GetIndexParameters().Length != 0)
            {
                return _ => null;
            }

            properties.Add(property);
            currentType = Nullable.GetUnderlyingType(property.PropertyType)
                          ?? property.PropertyType;
        }

        return source =>
        {
            object? current = source;

            foreach (var property in properties)
            {
                if (current is null)
                {
                    return null;
                }

                current = property.GetValue(current);
            }

            return current;
        };
    }

    private static string ConvertValueToString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            _ => Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty
        };
    }

    private static void AddNonEmpty(ICollection<string> target, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target.Add(value);
        }
    }

    private bool FocusFirstVisibleItem()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(index) is not ComboBoxItem container ||
                container.Visibility != Visibility.Visible)
            {
                continue;
            }

            container.Focus();
            Keyboard.Focus(container);
            return true;
        }

        return false;
    }

    private bool SelectFirstVisibleItem()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            if (ItemContainerGenerator.ContainerFromIndex(index) is not ComboBoxItem container ||
                container.Visibility != Visibility.Visible)
            {
                continue;
            }

            SetCurrentValue(SelectedItemProperty, Items[index]);
            SetCurrentValue(IsDropDownOpenProperty, false);
            return true;
        }

        return false;
    }

    private void UpdateGeneratedItemTemplate()
    {
        var hasUserItemTemplate = ItemTemplate is not null &&
                                  !ReferenceEquals(ItemTemplate, _generatedItemTemplate);

        var shouldGenerateTemplate =
            !hasUserItemTemplate &&
            string.IsNullOrWhiteSpace(DisplayMemberPath) &&
            (!string.IsNullOrWhiteSpace(DisplayTextPath) ||
             DisplayTextConverter is not null);

        if (!shouldGenerateTemplate)
        {
            if (_generatedItemTemplate is not null &&
                ReferenceEquals(ItemTemplate, _generatedItemTemplate))
            {
                SetCurrentValue(ItemTemplateProperty, null);
            }

            _generatedItemTemplate = null;
            return;
        }

#pragma warning disable CS0618 // FrameworkElementFactory jest nadal obsługiwany przez WPF.
        var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));
#pragma warning restore CS0618

        textBlockFactory.SetBinding(
            TextBlock.TextProperty,
            new Binding
            {
                Converter = _generatedDisplayConverter,
                Mode = BindingMode.OneWay
            });

        _generatedItemTemplate = new DataTemplate
        {
            VisualTree = textBlockFactory
        };

        SetCurrentValue(ItemTemplateProperty, _generatedItemTemplate);
    }

    private void SearchableComboBox_Unloaded(object sender, RoutedEventArgs e)
    {
        _debounceTimer.Stop();
    }

    private sealed class GeneratedDisplayConverter : IValueConverter
    {
        private readonly SearchableComboBox _owner;

        public GeneratedDisplayConverter(SearchableComboBox owner)
        {
            _owner = owner;
        }

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return _owner.GetDisplayText(value);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}