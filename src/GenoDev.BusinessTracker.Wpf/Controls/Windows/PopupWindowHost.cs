using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using GenoDev.BusinessTracker.Wpf.ViewModels;

namespace GenoDev.BusinessTracker.Wpf.Controls;

/// <summary>
/// Declarative bridge between an MVVM IsOpen property and a real PopupWindow.
/// Existing inline XAML content is temporarily moved into the native window.
/// </summary>
public sealed class PopupWindowHost : ContentControl
{
    private const double ShadowMargin = 22;
    private PopupWindow? _window;
    private object? _detachedContent;
    private PopupContentLayoutSnapshot? _contentLayoutSnapshot;
    private ViewModelBase? _observedViewModel;
    private Window? _logicalHostWindow;
    private bool _isClosingFromHost;
    private bool _isWindowHiddenInRegistry;
    private bool _hasHandledOpenRequestForCurrentWindow;

    static PopupWindowHost()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(PopupWindowHost),
            new FrameworkPropertyMetadata(typeof(PopupWindowHost)));
    }

    public PopupWindowHost()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
    }

    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(PopupWindowHost),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnWindowPropertyChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(PopupWindowHost),
        new PropertyMetadata(string.Empty, OnWindowPropertyChanged));

    public static readonly DependencyProperty IsResizableProperty = DependencyProperty.Register(
        nameof(IsResizable),
        typeof(bool),
        typeof(PopupWindowHost),
        new PropertyMetadata(false, OnWindowPropertyChanged));

    public static readonly DependencyProperty PopupWidthProperty = DependencyProperty.Register(
        nameof(PopupWidth),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty PopupHeightProperty = DependencyProperty.Register(
        nameof(PopupHeight),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(double.NaN));

    public static readonly DependencyProperty MinPopupWidthProperty = DependencyProperty.Register(
        nameof(MinPopupWidth),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty MinPopupHeightProperty = DependencyProperty.Register(
        nameof(MinPopupHeight),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(0d));

    public static readonly DependencyProperty MaxPopupWidthProperty = DependencyProperty.Register(
        nameof(MaxPopupWidth),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(double.PositiveInfinity));

    public static readonly DependencyProperty MaxPopupHeightProperty = DependencyProperty.Register(
        nameof(MaxPopupHeight),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(double.PositiveInfinity));

    public static readonly DependencyProperty OpenAtMouseProperty = DependencyProperty.Register(
        nameof(OpenAtMouse),
        typeof(bool),
        typeof(PopupWindowHost),
        new PropertyMetadata(true));

    public static readonly DependencyProperty CenterOnHostProperty = DependencyProperty.Register(
        nameof(CenterOnHost),
        typeof(bool),
        typeof(PopupWindowHost),
        new PropertyMetadata(false));

    public static readonly DependencyProperty MouseOffsetXProperty = DependencyProperty.Register(
        nameof(MouseOffsetX),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(12d));

    public static readonly DependencyProperty MouseOffsetYProperty = DependencyProperty.Register(
        nameof(MouseOffsetY),
        typeof(double),
        typeof(PopupWindowHost),
        new PropertyMetadata(12d));

    public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
        nameof(CloseCommand),
        typeof(ICommand),
        typeof(PopupWindowHost));

    // Kept as a declarative compatibility property. A full window always stays
    // open until its state or close button explicitly closes it.
    public static readonly DependencyProperty StaysOpenProperty = DependencyProperty.Register(
        nameof(StaysOpen),
        typeof(bool),
        typeof(PopupWindowHost),
        new PropertyMetadata(true));

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsResizable
    {
        get => (bool)GetValue(IsResizableProperty);
        set => SetValue(IsResizableProperty, value);
    }

    public double PopupWidth
    {
        get => (double)GetValue(PopupWidthProperty);
        set => SetValue(PopupWidthProperty, value);
    }

    public double PopupHeight
    {
        get => (double)GetValue(PopupHeightProperty);
        set => SetValue(PopupHeightProperty, value);
    }

    public double MinPopupWidth
    {
        get => (double)GetValue(MinPopupWidthProperty);
        set => SetValue(MinPopupWidthProperty, value);
    }

    public double MinPopupHeight
    {
        get => (double)GetValue(MinPopupHeightProperty);
        set => SetValue(MinPopupHeightProperty, value);
    }

    public double MaxPopupWidth
    {
        get => (double)GetValue(MaxPopupWidthProperty);
        set => SetValue(MaxPopupWidthProperty, value);
    }

    public double MaxPopupHeight
    {
        get => (double)GetValue(MaxPopupHeightProperty);
        set => SetValue(MaxPopupHeightProperty, value);
    }

    public bool OpenAtMouse
    {
        get => (bool)GetValue(OpenAtMouseProperty);
        set => SetValue(OpenAtMouseProperty, value);
    }

    public bool CenterOnHost
    {
        get => (bool)GetValue(CenterOnHostProperty);
        set => SetValue(CenterOnHostProperty, value);
    }

    public double MouseOffsetX
    {
        get => (double)GetValue(MouseOffsetXProperty);
        set => SetValue(MouseOffsetXProperty, value);
    }

    public double MouseOffsetY
    {
        get => (double)GetValue(MouseOffsetYProperty);
        set => SetValue(MouseOffsetYProperty, value);
    }

    public ICommand? CloseCommand
    {
        get => (ICommand?)GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    public bool StaysOpen
    {
        get => (bool)GetValue(StaysOpenProperty);
        set => SetValue(StaysOpenProperty, value);
    }

    private static void OnWindowPropertyChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        var host = (PopupWindowHost)dependencyObject;
        host.SynchronizeWindow();
        host.UpdateOpenWindowProperties();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _logicalHostWindow = Window.GetWindow(this) ?? _logicalHostWindow;
        AttachViewModel(DataContext as ViewModelBase);
        SynchronizeWindow();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        // Tab navigation unloads inactive views even though their hosted popup
        // is still a live, independent application window. Keep both the
        // window and its open-request subscription alive across that transient
        // visual-tree change. Once no window exists, the subscription is no
        // longer needed while the host remains unloaded.
        if (_window == null)
        {
            AttachViewModel(null);
        }
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded || _window != null)
        {
            AttachViewModel(e.NewValue as ViewModelBase);
        }

        if (_window != null)
        {
            _window.DataContext = e.NewValue;
        }
    }

    private void AttachViewModel(ViewModelBase? viewModel)
    {
        if (ReferenceEquals(_observedViewModel, viewModel))
        {
            return;
        }

        if (_observedViewModel != null)
        {
            _observedViewModel.PopupOpenRequested -= ViewModel_PopupOpenRequested;
        }

        _observedViewModel = viewModel;
        if (_observedViewModel != null)
        {
            _observedViewModel.PopupOpenRequested += ViewModel_PopupOpenRequested;
        }
    }

    private void ViewModel_PopupOpenRequested(object? sender, PopupOpenRequestedEventArgs e)
    {
        var boundPropertyName = BindingOperations
            .GetBindingExpression(this, IsOpenProperty)?
            .ParentBinding.Path?.Path;
        if (_window == null ||
            !IsOpen ||
            !string.Equals(boundPropertyName, e.PropertyName, StringComparison.Ordinal))
        {
            return;
        }

        if (!_hasHandledOpenRequestForCurrentWindow)
        {
            // Setting IsOpen=true creates the first window synchronously. The
            // explicit request immediately following it belongs to that same
            // opening and must not recreate the freshly initialized shell.
            _hasHandledOpenRequestForCurrentWindow = true;
            _window.BringToFront();
            return;
        }

        ReopenWindowAsNewSession();
    }

    private void ReopenWindowAsNewSession()
    {
        CloseWindow(suppressHostActivation: true);

        // An explicit ViewModel action may target a host in an inactive tab,
        // so recreate directly instead of relying on IsLoaded-based
        // synchronization. A new PopupWindow restores default size, cursor
        // placement, topmost state and every other per-window shell setting.
        if (IsOpen)
        {
            OpenWindow();
            AttachViewModel(DataContext as ViewModelBase);
            _hasHandledOpenRequestForCurrentWindow = true;
            _window?.BringToFront();
        }
    }

    private void SynchronizeWindow()
    {
        if (!IsLoaded)
        {
            return;
        }

        if (IsOpen)
        {
            OpenWindow();
        }
        else
        {
            if (!_isWindowHiddenInRegistry || _window is not { IsVisible: false })
            {
                CloseWindow();
            }
        }
    }

    private void OpenWindow()
    {
        if (_window != null)
        {
            if (!_window.IsVisible)
            {
                _isWindowHiddenInRegistry = false;
                _window.Show();
                _window.ConstrainToWorkArea(false);
                _window.BeginOpeningAnimation();
                _window.BringToFront();
            }

            return;
        }

        var hostWindow = Window.GetWindow(this) ?? _logicalHostWindow;
        _logicalHostWindow = hostWindow;
        _detachedContent = Content;
        _contentLayoutSnapshot = PopupContentLayoutSnapshot.Capture(_detachedContent as DependencyObject);
        SetCurrentValue(ContentProperty, null);

        _window = new PopupWindow
        {
            Title = Title,
            DataContext = DataContext,
            HostWindow = hostWindow,
            WindowContent = _detachedContent,
            IsResizable = IsResizable,
            Padding = Padding,
            HorizontalContentAlignment = HorizontalContentAlignment,
            VerticalContentAlignment = VerticalContentAlignment
        };
        _hasHandledOpenRequestForCurrentWindow = false;
        ApplySize(_window);
        SetInitialPosition(_window, hostWindow);
        _window.Closed += Window_Closed;
        _window.HiddenToRegistry += Window_HiddenToRegistry;
        _window.RestoreRequested += Window_RestoreRequested;
        _window.Show();

        if (CenterOnHost || !OpenAtMouse)
        {
            CenterWindowOnHost(_window, hostWindow);
        }

        _window.ConstrainToWorkArea(OpenAtMouse && !CenterOnHost);
        _window.BeginOpeningAnimation();
    }

    private void ApplySize(PopupWindow window)
    {
        window.MinWidth = Math.Max(window.MinWidth, MinPopupWidth + 2 * ShadowMargin);
        window.MinHeight = Math.Max(window.MinHeight, MinPopupHeight + 2 * ShadowMargin);
        window.MaxWidth = double.IsPositiveInfinity(MaxPopupWidth)
            ? double.PositiveInfinity
            : MaxPopupWidth + 2 * ShadowMargin;
        window.MaxHeight = double.IsPositiveInfinity(MaxPopupHeight)
            ? double.PositiveInfinity
            : MaxPopupHeight + 2 * ShadowMargin;

        var hasWidth = !double.IsNaN(PopupWidth);
        var hasHeight = !double.IsNaN(PopupHeight);
        if (hasWidth)
        {
            window.Width = PopupWidth + 2 * ShadowMargin;
        }

        if (hasHeight)
        {
            window.Height = PopupHeight + 2 * ShadowMargin;
        }

        window.SizeToContent = (hasWidth, hasHeight) switch
        {
            (true, true) => SizeToContent.Manual,
            (true, false) => SizeToContent.Height,
            (false, true) => SizeToContent.Width,
            _ => SizeToContent.WidthAndHeight
        };
    }

    private void SetInitialPosition(PopupWindow window, Window? hostWindow)
    {
        if (CenterOnHost)
        {
            CenterWindowOnHost(window, hostWindow);
            return;
        }

        if (!OpenAtMouse)
        {
            CenterWindowOnHost(window, hostWindow);
            return;
        }

        var cursor = GetCursorPositionInDips(this);
        // Left/Top describe the layered HWND including its invisible shadow
        // gutter. Keep the visible popup corner at the configured mouse offset.
        window.Left = cursor.X + MouseOffsetX - ShadowMargin;
        window.Top = cursor.Y + MouseOffsetY - ShadowMargin;
    }

    private static void CenterWindowOnHost(PopupWindow window, Window? hostWindow)
    {
        var width = double.IsNaN(window.Width) ? window.ActualWidth : window.Width;
        var height = double.IsNaN(window.Height) ? window.ActualHeight : window.Height;
        if (hostWindow is not { IsVisible: true })
        {
            var workArea = SystemParameters.WorkArea;
            window.Left = workArea.Left + (workArea.Width - width) / 2;
            window.Top = workArea.Top + (workArea.Height - height) / 2;
            return;
        }

        window.Left = hostWindow.Left + (hostWindow.ActualWidth - width) / 2;
        window.Top = hostWindow.Top + (hostWindow.ActualHeight - height) / 2;
    }

    private void UpdateOpenWindowProperties()
    {
        if (_window == null)
        {
            return;
        }

        _window.Title = Title;
        _window.IsResizable = IsResizable;
    }

    private void CloseWindow(bool suppressHostActivation = false)
    {
        if (_window == null)
        {
            return;
        }

        _isClosingFromHost = true;
        var window = _window;
        _isWindowHiddenInRegistry = false;
        _hasHandledOpenRequestForCurrentWindow = false;
        if (suppressHostActivation)
        {
            _window = null;
            window.Closed -= Window_Closed;
            window.HiddenToRegistry -= Window_HiddenToRegistry;
            window.RestoreRequested -= Window_RestoreRequested;
            RestoreContent(window);
            window.CloseImmediatelyWithoutHostActivation();
            _isClosingFromHost = false;

            if (!IsLoaded)
            {
                AttachViewModel(null);
            }

            return;
        }

        window.Close();
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        if (sender is not PopupWindow window)
        {
            return;
        }

        window.Closed -= Window_Closed;
        window.HiddenToRegistry -= Window_HiddenToRegistry;
        window.RestoreRequested -= Window_RestoreRequested;
        _window = null;
        _isWindowHiddenInRegistry = false;
        _hasHandledOpenRequestForCurrentWindow = false;
        var wasClosingFromHost = _isClosingFromHost;
        _isClosingFromHost = false;
        try
        {
            if (!wasClosingFromHost)
            {
                // Clear dynamic editor content while it still has a stable parent.
                // Reparenting its live DataTemplate first can recursively invalidate
                // ContentPresenter bindings during the native close callback.
                RequestClose();
            }
        }
        finally
        {
            // A close command can indirectly touch complex controls (for example
            // a WebBrowser-backed preview). Never let such an exception strand
            // the declarative content inside an HWND that has already closed.
            RestoreContent(window);

            if (!IsLoaded)
            {
                AttachViewModel(null);
            }
        }
    }

    private void Window_HiddenToRegistry(object? sender, EventArgs e)
    {
        _isWindowHiddenInRegistry = true;
        if (IsOpen)
        {
            SetCurrentValue(IsOpenProperty, false);
        }
    }

    private void Window_RestoreRequested(object? sender, EventArgs e)
    {
        if (!IsOpen)
        {
            SetCurrentValue(IsOpenProperty, true);
        }
    }

    private void RestoreContent(PopupWindow window)
    {
        var content = _detachedContent;
        var pinnedDataContextElement = PinInheritedDataContext(content, DataContext);
        try
        {
            window.WindowContent = null;
            _contentLayoutSnapshot?.Restore();
            _contentLayoutSnapshot = null;
            if (content == null) return;

            SetCurrentValue(ContentProperty, content);
            _detachedContent = null;

        }
        finally
        {
            pinnedDataContextElement?.ClearValue(DataContextProperty);
        }
    }

    private static FrameworkElement? PinInheritedDataContext(object? content, object? dataContext)
    {
        if (content is not FrameworkElement element ||
            element.ReadLocalValue(DataContextProperty) != DependencyProperty.UnsetValue)
        {
            return null;
        }

        element.SetCurrentValue(DataContextProperty, dataContext);
        return element;
    }

    private void RequestClose()
    {
        try
        {
            if (CloseCommand?.CanExecute(null) == true)
            {
                CloseCommand.Execute(null);
            }
        }
        finally
        {
            // Keep two-way popup state recoverable even when ViewModel cleanup
            // fails. A later explicit open request must always create a session.
            if (IsOpen)
            {
                SetCurrentValue(IsOpenProperty, false);
            }
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out NativePoint point);

    private static Point GetCursorPositionInDips(Visual referenceVisual)
    {
        if (!GetCursorPos(out var cursorPosition))
        {
            return default;
        }

        var positionInPixels = new Point(cursorPosition.X, cursorPosition.Y);
        var source = PresentationSource.FromVisual(referenceVisual);
        return source?.CompositionTarget is { } compositionTarget
            ? compositionTarget.TransformFromDevice.Transform(positionInPixels)
            : positionInPixels;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }

    private sealed class PopupContentLayoutSnapshot
    {
        private static readonly DependencyProperty[] SessionLayoutProperties =
        [
            FrameworkElement.WidthProperty,
            FrameworkElement.HeightProperty,
            FrameworkElement.HorizontalAlignmentProperty,
            FrameworkElement.VerticalAlignmentProperty
        ];

        private readonly List<ElementLayoutSnapshot> _elements;

        private PopupContentLayoutSnapshot(List<ElementLayoutSnapshot> elements)
        {
            _elements = elements;
        }

        public static PopupContentLayoutSnapshot? Capture(DependencyObject? root)
        {
            if (root == null)
            {
                return null;
            }

            var elements = EnumerateDescendants(root)
                .OfType<FrameworkElement>()
                .Select(ElementLayoutSnapshot.Capture)
                .ToList();
            return new PopupContentLayoutSnapshot(elements);
        }

        public void Restore()
        {
            foreach (var element in _elements)
            {
                element.Restore();
            }
        }

        private static IEnumerable<DependencyObject> EnumerateDescendants(DependencyObject root)
        {
            var visited = new HashSet<DependencyObject>();
            foreach (var descendant in EnumerateDescendants(root, visited))
            {
                yield return descendant;
            }
        }

        private static IEnumerable<DependencyObject> EnumerateDescendants(
            DependencyObject root,
            HashSet<DependencyObject> visited)
        {
            if (!visited.Add(root))
            {
                yield break;
            }

            yield return root;
            foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                foreach (var descendant in EnumerateDescendants(child, visited))
                {
                    yield return descendant;
                }
            }

            if (root is not Visual && root is not System.Windows.Media.Media3D.Visual3D)
            {
                yield break;
            }

            for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                foreach (var descendant in EnumerateDescendants(
                             VisualTreeHelper.GetChild(root, index),
                             visited))
                {
                    yield return descendant;
                }
            }
        }

        private sealed class ElementLayoutSnapshot
        {
            private readonly FrameworkElement _element;
            private readonly PropertySnapshot[] _properties;

            private ElementLayoutSnapshot(
                FrameworkElement element,
                PropertySnapshot[] properties)
            {
                _element = element;
                _properties = properties;
            }

            public static ElementLayoutSnapshot Capture(FrameworkElement element) =>
                new(
                    element,
                    SessionLayoutProperties
                        .Select(property => PropertySnapshot.Capture(element, property))
                        .ToArray());

            public void Restore()
            {
                foreach (var property in _properties)
                {
                    property.Restore(_element);
                }
            }
        }

        private sealed class PropertySnapshot
        {
            private readonly DependencyProperty _property;
            private readonly BindingBase? _binding;
            private readonly object _localValue;

            private PropertySnapshot(
                DependencyProperty property,
                BindingBase? binding,
                object localValue)
            {
                _property = property;
                _binding = binding;
                _localValue = localValue;
            }

            public static PropertySnapshot Capture(
                FrameworkElement element,
                DependencyProperty property) =>
                new(
                    property,
                    BindingOperations.GetBindingBase(element, property),
                    element.ReadLocalValue(property));

            public void Restore(FrameworkElement element)
            {
                BindingOperations.ClearBinding(element, _property);
                if (_binding != null)
                {
                    BindingOperations.SetBinding(element, _property, _binding);
                }
                else if (_localValue != DependencyProperty.UnsetValue)
                {
                    element.SetValue(_property, _localValue);
                }
                else
                {
                    element.ClearValue(_property);
                }
            }
        }
    }
}
