using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;

namespace GenoDev.BusinessTracker.Wpf.Controls
{
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartDragArea, Type = typeof(FrameworkElement))]
    public class DraggablePopup : ContentControl
    {
        private const string PartPopup = "PART_Popup";
        private const string PartDragArea = "PART_DragArea";
        private const string PartResizeThumb = "PART_ResizeThumb";

        private Popup? _popup;
        private FrameworkElement? _dragArea;
        private Thumb? _resizeThumb;
        private Window? _ownerWindow;

        private bool _isLoaded;
        private bool _isApplicationActive = true;
        private bool _isDragging;
        private bool _isInternalPopupStateChange;
        private bool _positionInitialized;
        private bool _applicationEventsAttached;

        private Point _dragStartCursor;
        private Point _dragStartPopup;
        private Point _restorePopupPosition;
        private double _restorePopupWidth;
        private double _restorePopupHeight;
        private bool _hasRestoreBounds;

        static DraggablePopup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DraggablePopup),
                new FrameworkPropertyMetadata(typeof(DraggablePopup)));
        }

        public DraggablePopup()
        {
            ToggleMaximizeCommand = new RelayCommand(
                ToggleMaximize,
                () => IsResizable);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        public IRelayCommand ToggleMaximizeCommand { get; }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(
                nameof(IsOpen),
                typeof(bool),
                typeof(DraggablePopup),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnIsOpenChanged));

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProperty);
            set => SetValue(IsOpenProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(DraggablePopup),
                new PropertyMetadata(string.Empty));

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty IsDragEnabledProperty =
            DependencyProperty.Register(
                nameof(IsDragEnabled),
                typeof(bool),
                typeof(DraggablePopup),
                new PropertyMetadata(true));

        public bool IsDragEnabled
        {
            get => (bool)GetValue(IsDragEnabledProperty);
            set => SetValue(IsDragEnabledProperty, value);
        }

        public static readonly DependencyProperty IsResizableProperty =
            DependencyProperty.Register(
                nameof(IsResizable),
                typeof(bool),
                typeof(DraggablePopup),
                new PropertyMetadata(false, OnIsResizableChanged));

        public bool IsResizable
        {
            get => (bool)GetValue(IsResizableProperty);
            set => SetValue(IsResizableProperty, value);
        }

        public static readonly DependencyProperty IsMaximizedProperty =
            DependencyProperty.Register(
                nameof(IsMaximized),
                typeof(bool),
                typeof(DraggablePopup),
                new PropertyMetadata(false));

        public bool IsMaximized
        {
            get => (bool)GetValue(IsMaximizedProperty);
            private set => SetValue(IsMaximizedProperty, value);
        }

        public static readonly DependencyProperty PopupWidthProperty =
            DependencyProperty.Register(
                nameof(PopupWidth),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(double.NaN));

        public double PopupWidth
        {
            get => (double)GetValue(PopupWidthProperty);
            set => SetValue(PopupWidthProperty, value);
        }

        public static readonly DependencyProperty PopupHeightProperty =
            DependencyProperty.Register(
                nameof(PopupHeight),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(double.NaN));

        public double PopupHeight
        {
            get => (double)GetValue(PopupHeightProperty);
            set => SetValue(PopupHeightProperty, value);
        }

        public static readonly DependencyProperty MinPopupWidthProperty =
            DependencyProperty.Register(
                nameof(MinPopupWidth),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(0d));

        public double MinPopupWidth
        {
            get => (double)GetValue(MinPopupWidthProperty);
            set => SetValue(MinPopupWidthProperty, value);
        }

        public static readonly DependencyProperty MinPopupHeightProperty =
            DependencyProperty.Register(
                nameof(MinPopupHeight),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(0d));

        public double MinPopupHeight
        {
            get => (double)GetValue(MinPopupHeightProperty);
            set => SetValue(MinPopupHeightProperty, value);
        }

        public static readonly DependencyProperty MaxPopupWidthProperty =
            DependencyProperty.Register(
                nameof(MaxPopupWidth),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(double.PositiveInfinity));

        public double MaxPopupWidth
        {
            get => (double)GetValue(MaxPopupWidthProperty);
            set => SetValue(MaxPopupWidthProperty, value);
        }

        public static readonly DependencyProperty MaxPopupHeightProperty =
            DependencyProperty.Register(
                nameof(MaxPopupHeight),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(double.PositiveInfinity));

        public double MaxPopupHeight
        {
            get => (double)GetValue(MaxPopupHeightProperty);
            set => SetValue(MaxPopupHeightProperty, value);
        }

        public static readonly DependencyProperty StaysOpenProperty =
            DependencyProperty.Register(
                nameof(StaysOpen),
                typeof(bool),
                typeof(DraggablePopup),
                new PropertyMetadata(true, OnStaysOpenChanged));

        public bool StaysOpen
        {
            get => (bool)GetValue(StaysOpenProperty);
            set => SetValue(StaysOpenProperty, value);
        }

        public static readonly DependencyProperty OpenAtMouseProperty =
            DependencyProperty.Register(
                nameof(OpenAtMouse),
                typeof(bool),
                typeof(DraggablePopup),
                new PropertyMetadata(true));

        public bool OpenAtMouse
        {
            get => (bool)GetValue(OpenAtMouseProperty);
            set => SetValue(OpenAtMouseProperty, value);
        }

        public static readonly DependencyProperty MouseOffsetXProperty =
            DependencyProperty.Register(
                nameof(MouseOffsetX),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(12d));

        public double MouseOffsetX
        {
            get => (double)GetValue(MouseOffsetXProperty);
            set => SetValue(MouseOffsetXProperty, value);
        }

        public static readonly DependencyProperty MouseOffsetYProperty =
            DependencyProperty.Register(
                nameof(MouseOffsetY),
                typeof(double),
                typeof(DraggablePopup),
                new PropertyMetadata(12d));

        public double MouseOffsetY
        {
            get => (double)GetValue(MouseOffsetYProperty);
            set => SetValue(MouseOffsetYProperty, value);
        }

        public static readonly DependencyProperty HideWhenApplicationInactiveProperty =
            DependencyProperty.Register(
                nameof(HideWhenApplicationInactive),
                typeof(bool),
                typeof(DraggablePopup),
                new PropertyMetadata(true, OnVisibilityRuleChanged));

        /// <summary>
        /// Tymczasowo ukrywa natywne okno Popup, kiedy użytkownik przejdzie
        /// do innej aplikacji. Właściwość IsOpen pozostaje wtedy ustawiona na true,
        /// więc popup wraca w tej samej pozycji po ponownej aktywacji aplikacji.
        /// </summary>
        public bool HideWhenApplicationInactive
        {
            get => (bool)GetValue(HideWhenApplicationInactiveProperty);
            set => SetValue(HideWhenApplicationInactiveProperty, value);
        }

        public static readonly DependencyProperty HideWhenOwnerMinimizedProperty =
            DependencyProperty.Register(
                nameof(HideWhenOwnerMinimized),
                typeof(bool),
                typeof(DraggablePopup),
                new PropertyMetadata(true, OnVisibilityRuleChanged));

        public bool HideWhenOwnerMinimized
        {
            get => (bool)GetValue(HideWhenOwnerMinimizedProperty);
            set => SetValue(HideWhenOwnerMinimizedProperty, value);
        }

        public override void OnApplyTemplate()
        {
            DetachTemplateEvents();

            base.OnApplyTemplate();

            _popup = GetTemplateChild(PartPopup) as Popup;
            _dragArea = GetTemplateChild(PartDragArea) as FrameworkElement;
            _resizeThumb = GetTemplateChild(PartResizeThumb) as Thumb;

            if (_popup != null)
            {
                // Tryb jest stały przez cały czas życia kontrolki. Dzięki temu
                // pierwsze rozpoczęcie przeciągania nie powoduje skoku do (0, 0).
                _popup.Placement = PlacementMode.AbsolutePoint;
                _popup.StaysOpen = StaysOpen;
                _popup.Focusable = true;
            }

            AttachTemplateEvents();
            RefreshPopupState();
        }

        private static void OnIsOpenChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (DraggablePopup)dependencyObject;

            if (e.NewValue is false)
            {
                control.RestoreFromMaximize();
                control._positionInitialized = false;
                control.EndDrag();
            }

            control.RefreshPopupState();
        }

        private static void OnIsResizableChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (DraggablePopup)dependencyObject;
            if (e.NewValue is false)
            {
                control.RestoreFromMaximize();
            }

            control.ToggleMaximizeCommand.NotifyCanExecuteChanged();
        }

        private static void OnStaysOpenChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (DraggablePopup)dependencyObject;

            if (control._popup != null)
            {
                control._popup.StaysOpen = (bool)e.NewValue;
            }
        }

        private static void OnVisibilityRuleChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            ((DraggablePopup)dependencyObject).RefreshPopupState();
        }

        private void RefreshPopupState()
        {
            if (_popup == null)
            {
                return;
            }

            _popup.Placement = PlacementMode.AbsolutePoint;
            _popup.StaysOpen = StaysOpen;
            _popup.Focusable = true;

            var shouldBeVisible = IsOpen && !ShouldTemporarilyHide();

            if (!shouldBeVisible)
            {
                SetNativePopupOpen(false);
                return;
            }

            if (!_positionInitialized)
            {
                PrepareInitialPosition();
            }

            SetNativePopupOpen(true);
        }

        private bool ShouldTemporarilyHide()
        {
            if (!_isLoaded)
            {
                return true;
            }

            if (HideWhenApplicationInactive && !_isApplicationActive)
            {
                return true;
            }

            if (_ownerWindow == null)
            {
                return false;
            }

            if (!_ownerWindow.IsVisible)
            {
                return true;
            }

            return HideWhenOwnerMinimized &&
                   _ownerWindow.WindowState == WindowState.Minimized;
        }

        private void SetNativePopupOpen(bool value)
        {
            if (_popup == null || _popup.IsOpen == value)
            {
                return;
            }

            try
            {
                _isInternalPopupStateChange = true;
                _popup.IsOpen = value;
            }
            finally
            {
                _isInternalPopupStateChange = false;
            }
        }

        private void PrepareInitialPosition()
        {
            if (_popup == null)
            {
                return;
            }

            _popup.Placement = PlacementMode.AbsolutePoint;

            if (OpenAtMouse)
            {
                var cursorPosition = GetCursorPositionInDips(this);

                _popup.HorizontalOffset = cursorPosition.X + MouseOffsetX;
                _popup.VerticalOffset = cursorPosition.Y + MouseOffsetY;
            }

            _positionInitialized = true;
        }

        private void AttachTemplateEvents()
        {
            if (_popup != null)
            {
                _popup.Closed += Popup_Closed;
            }

            if (_dragArea != null)
            {
                _dragArea.PreviewMouseLeftButtonDown += DragArea_MouseLeftButtonDown;
                _dragArea.PreviewMouseMove += DragArea_MouseMove;
                _dragArea.PreviewMouseLeftButtonUp += DragArea_MouseLeftButtonUp;
                _dragArea.LostMouseCapture += DragArea_LostMouseCapture;
            }

            if (_resizeThumb != null)
            {
                _resizeThumb.DragDelta += ResizeThumb_DragDelta;
            }
        }

        private void DetachTemplateEvents()
        {
            if (_popup != null)
            {
                _popup.Closed -= Popup_Closed;
            }

            if (_dragArea != null)
            {
                _dragArea.PreviewMouseLeftButtonDown -= DragArea_MouseLeftButtonDown;
                _dragArea.PreviewMouseMove -= DragArea_MouseMove;
                _dragArea.PreviewMouseLeftButtonUp -= DragArea_MouseLeftButtonUp;
                _dragArea.LostMouseCapture -= DragArea_LostMouseCapture;
            }

            if (_resizeThumb != null)
            {
                _resizeThumb.DragDelta -= ResizeThumb_DragDelta;
            }
        }

        private void Popup_Closed(object? sender, EventArgs e)
        {
            EndDrag();

            // Zamknięcie wykonane przez kontrolkę jest tylko zmianą widoczności
            // natywnego okna. Nie zmieniamy wtedy publicznego IsOpen.
            if (_isInternalPopupStateChange || ShouldTemporarilyHide() || !IsOpen)
            {
                return;
            }

            // Dotyczy m.in. StaysOpen="False" i kliknięcia poza popupem.
            _positionInitialized = false;
            SetCurrentValue(IsOpenProperty, false);
        }

        private void DragArea_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!IsDragEnabled ||
                IsMaximized ||
                e.ChangedButton != MouseButton.Left ||
                _popup == null ||
                _dragArea == null ||
                IsInsideButton(e.OriginalSource as DependencyObject))
            {
                return;
            }

            _dragStartCursor = GetCursorPositionInDips(_dragArea);
            _dragStartPopup = new Point(
                _popup.HorizontalOffset,
                _popup.VerticalOffset);

            _isDragging = _dragArea.CaptureMouse();

            if (_isDragging)
            {
                e.Handled = true;
            }
        }

        private static bool IsInsideButton(DependencyObject? element)
        {
            while (element is not null)
            {
                if (element is ButtonBase)
                {
                    return true;
                }

                element = element is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(element)
                    : LogicalTreeHelper.GetParent(element);
            }

            return false;
        }

        private void ResizeThumb_DragDelta(
            object sender,
            DragDeltaEventArgs e)
        {
            if (!IsResizable || IsMaximized)
            {
                return;
            }

            var currentWidth = double.IsNaN(PopupWidth)
                ? Math.Max(MinPopupWidth, _popup?.Child.RenderSize.Width ?? MinPopupWidth)
                : PopupWidth;
            var currentHeight = double.IsNaN(PopupHeight)
                ? Math.Max(MinPopupHeight, _popup?.Child.RenderSize.Height ?? MinPopupHeight)
                : PopupHeight;

            PopupWidth = Math.Clamp(
                currentWidth + e.HorizontalChange,
                MinPopupWidth,
                MaxPopupWidth);
            PopupHeight = Math.Clamp(
                currentHeight + e.VerticalChange,
                MinPopupHeight,
                MaxPopupHeight);
        }

        private void ToggleMaximize()
        {
            if (!IsResizable || _popup is null)
            {
                return;
            }

            if (IsMaximized)
            {
                RestoreFromMaximize();
                return;
            }

            _restorePopupPosition = new Point(
                _popup.HorizontalOffset,
                _popup.VerticalOffset);
            _restorePopupWidth = PopupWidth;
            _restorePopupHeight = PopupHeight;
            _hasRestoreBounds = true;

            var workArea = GetCurrentMonitorWorkAreaInDips();
            PopupWidth = workArea.Width;
            PopupHeight = workArea.Height;
            _popup.HorizontalOffset = workArea.Left;
            _popup.VerticalOffset = workArea.Top;
            IsMaximized = true;
        }

        private void RestoreFromMaximize()
        {
            if (!IsMaximized)
            {
                return;
            }

            if (_hasRestoreBounds)
            {
                PopupWidth = _restorePopupWidth;
                PopupHeight = _restorePopupHeight;
                if (_popup is not null)
                {
                    _popup.HorizontalOffset = _restorePopupPosition.X;
                    _popup.VerticalOffset = _restorePopupPosition.Y;
                }
            }

            IsMaximized = false;
        }

        private Rect GetCurrentMonitorWorkAreaInDips()
        {
            if (!GetCursorPos(out var cursorPosition))
            {
                return SystemParameters.WorkArea;
            }

            var monitor = MonitorFromPoint(cursorPosition, MonitorDefaultToNearest);
            var monitorInfo = new NativeMonitorInfo
            {
                Size = Marshal.SizeOf<NativeMonitorInfo>()
            };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
            {
                return SystemParameters.WorkArea;
            }

            var source = PresentationSource.FromVisual(_popup?.Child ?? this);
            var transform = source?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
            var topLeft = transform.Transform(new Point(monitorInfo.Work.Left, monitorInfo.Work.Top));
            var bottomRight = transform.Transform(new Point(monitorInfo.Work.Right, monitorInfo.Work.Bottom));
            return new Rect(topLeft, bottomRight);
        }

        private void DragArea_MouseMove(
            object sender,
            MouseEventArgs e)
        {
            if (!_isDragging ||
                _popup == null ||
                _dragArea == null)
            {
                return;
            }

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndDrag();
                return;
            }

            var cursorPosition = GetCursorPositionInDips(_dragArea);

            _popup.HorizontalOffset =
                _dragStartPopup.X + cursorPosition.X - _dragStartCursor.X;

            _popup.VerticalOffset =
                _dragStartPopup.Y + cursorPosition.Y - _dragStartCursor.Y;

            e.Handled = true;
        }

        private void DragArea_MouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (!_isDragging)
            {
                return;
            }

            EndDrag();
            e.Handled = true;
        }

        private void DragArea_LostMouseCapture(
            object sender,
            MouseEventArgs e)
        {
            _isDragging = false;
        }

        private void EndDrag()
        {
            if (!_isDragging)
            {
                return;
            }

            _isDragging = false;

            if (_dragArea?.IsMouseCaptured == true)
            {
                _dragArea.ReleaseMouseCapture();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            AttachOwnerWindow();
            AttachApplicationEvents();

            // Nie polegamy wyłącznie na wcześniejszym zdarzeniu Activated,
            // ponieważ kontrolka mogła zostać załadowana już po jego wystąpieniu.
            _isApplicationActive = _ownerWindow?.IsActive ?? true;

            RefreshPopupState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;

            DetachApplicationEvents();
            DetachOwnerWindow();
            EndDrag();
            RefreshPopupState();
        }

        private void AttachApplicationEvents()
        {
            if (_applicationEventsAttached || Application.Current == null)
            {
                return;
            }

            Application.Current.Activated += Application_Activated;
            Application.Current.Deactivated += Application_Deactivated;
            _applicationEventsAttached = true;
        }

        private void DetachApplicationEvents()
        {
            if (!_applicationEventsAttached || Application.Current == null)
            {
                return;
            }

            Application.Current.Activated -= Application_Activated;
            Application.Current.Deactivated -= Application_Deactivated;
            _applicationEventsAttached = false;
        }

        private void Application_Activated(object? sender, EventArgs e)
        {
            _isApplicationActive = true;
            RefreshPopupState();
        }

        private void Application_Deactivated(object? sender, EventArgs e)
        {
            _isApplicationActive = false;
            EndDrag();
            RefreshPopupState();
        }

        private void AttachOwnerWindow()
        {
            DetachOwnerWindow();

            _ownerWindow = Window.GetWindow(this);

            if (_ownerWindow == null)
            {
                return;
            }

            _ownerWindow.StateChanged += OwnerWindow_StateChanged;
            _ownerWindow.IsVisibleChanged += OwnerWindow_IsVisibleChanged;
            _ownerWindow.Closed += OwnerWindow_Closed;
        }

        private void DetachOwnerWindow()
        {
            if (_ownerWindow == null)
            {
                return;
            }

            _ownerWindow.StateChanged -= OwnerWindow_StateChanged;
            _ownerWindow.IsVisibleChanged -= OwnerWindow_IsVisibleChanged;
            _ownerWindow.Closed -= OwnerWindow_Closed;
            _ownerWindow = null;
        }

        private void OwnerWindow_StateChanged(object? sender, EventArgs e)
        {
            RefreshPopupState();
        }

        private void OwnerWindow_IsVisibleChanged(
            object sender,
            DependencyPropertyChangedEventArgs e)
        {
            RefreshPopupState();
        }

        private void OwnerWindow_Closed(object? sender, EventArgs e)
        {
            SetCurrentValue(IsOpenProperty, false);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public uint Flags;
        }

        private const uint MonitorDefaultToNearest = 2;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out NativePoint point);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(
            NativePoint point,
            uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorInfo(
            IntPtr monitor,
            ref NativeMonitorInfo monitorInfo);

        private static Point GetCursorPositionInDips(Visual referenceVisual)
        {
            if (!GetCursorPos(out var cursorPosition))
            {
                return default;
            }

            var positionInPixels = new Point(
                cursorPosition.X,
                cursorPosition.Y);

            var source = PresentationSource.FromVisual(referenceVisual);
            var compositionTarget = source?.CompositionTarget;

            return compositionTarget == null
                ? positionInPixels
                : compositionTarget.TransformFromDevice.Transform(positionInPixels);
        }
    }
}
