using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public static class ListSelectionBehavior
{
    private const int SelectionSettleDelayMilliseconds = 125;

    private static readonly ConditionalWeakTable<Selector, SelectionState> States = new();

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ListSelectionBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static readonly DependencyProperty SettledSelectedItemProperty =
        DependencyProperty.RegisterAttached(
            "SettledSelectedItem",
            typeof(object),
            typeof(ListSelectionBehavior),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSettledSelectedItemChanged));

    public static readonly RoutedEvent SelectionSettledEvent =
        EventManager.RegisterRoutedEvent(
            "SelectionSettled",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(ListSelectionBehavior));

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    public static object? GetSettledSelectedItem(DependencyObject element) =>
        element.GetValue(SettledSelectedItemProperty);

    public static void SetSettledSelectedItem(
        DependencyObject element,
        object? value) =>
        element.SetValue(SettledSelectedItemProperty, value);

    public static void AddSelectionSettledHandler(
        DependencyObject element,
        RoutedEventHandler handler)
    {
        if (element is Selector selector)
        {
            GetState(selector).Attach();
        }

        if (element is UIElement uiElement)
        {
            uiElement.AddHandler(SelectionSettledEvent, handler);
        }
    }

    public static void RemoveSelectionSettledHandler(
        DependencyObject element,
        RoutedEventHandler handler)
    {
        if (element is UIElement uiElement)
        {
            uiElement.RemoveHandler(SelectionSettledEvent, handler);
        }
    }

    private static void OnIsEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not Selector selector)
        {
            return;
        }

        if ((bool)e.NewValue)
        {
            GetState(selector).Attach();
        }
        else if (States.TryGetValue(selector, out var state))
        {
            state.Detach();
        }
    }

    private static void OnSettledSelectedItemChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not Selector selector)
        {
            return;
        }

        var state = GetState(selector);
        state.Attach();
        state.ApplySettledSelection(e.NewValue);
    }

    private static SelectionState GetState(Selector selector) =>
        States.GetValue(selector, static value => new SelectionState(value));

    private sealed class SelectionState
    {
        private readonly Selector _selector;
        private readonly DispatcherTimer _selectionSettledTimer;

        private object? _lastSettledSelection;
        private bool _isApplyingSettledSelection;
        private bool _isAttached;
        private bool _isSelectionCommitQueued;

        public SelectionState(Selector selector)
        {
            _selector = selector;
            _selectionSettledTimer = new DispatcherTimer(
                DispatcherPriority.Background,
                selector.Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(
                    SelectionSettleDelayMilliseconds)
            };
            _selectionSettledTimer.Tick += SelectionSettledTimer_Tick;
        }

        public void Attach()
        {
            if (_isAttached)
            {
                return;
            }

            _isAttached = true;
            _selector.SelectionChanged += Selector_SelectionChanged;
            _selector.AddHandler(
                UIElement.PreviewMouseLeftButtonUpEvent,
                new MouseButtonEventHandler(Selector_PreviewMouseLeftButtonUp),
                handledEventsToo: true);
            _selector.AddHandler(
                Mouse.LostMouseCaptureEvent,
                new MouseEventHandler(Selector_LostMouseCapture),
                handledEventsToo: true);

            if (_selector.ReadLocalValue(SettledSelectedItemProperty) !=
                DependencyProperty.UnsetValue)
            {
                _lastSettledSelection = GetSettledSelectedItem(_selector);
                ApplySettledSelection(_lastSettledSelection);
            }
            else
            {
                _lastSettledSelection = _selector.SelectedItem;
            }
        }

        public void Detach()
        {
            if (!_isAttached)
            {
                return;
            }

            _isAttached = false;
            _selectionSettledTimer.Stop();
            _selector.SelectionChanged -= Selector_SelectionChanged;
            _selector.RemoveHandler(
                UIElement.PreviewMouseLeftButtonUpEvent,
                new MouseButtonEventHandler(Selector_PreviewMouseLeftButtonUp));
            _selector.RemoveHandler(
                Mouse.LostMouseCaptureEvent,
                new MouseEventHandler(Selector_LostMouseCapture));
        }

        public void ApplySettledSelection(object? selectedItem)
        {
            _selectionSettledTimer.Stop();
            _lastSettledSelection = selectedItem;
            if (ReferenceEquals(_selector.SelectedItem, selectedItem))
            {
                return;
            }

            _isApplyingSettledSelection = true;
            try
            {
                _selector.SelectedItem = selectedItem;
            }
            finally
            {
                _isApplyingSettledSelection = false;
            }
        }

        private void Selector_SelectionChanged(
            object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, _selector) ||
                _isApplyingSettledSelection)
            {
                return;
            }

            _selectionSettledTimer.Stop();
            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                _selectionSettledTimer.Start();
            }
        }

        private void Selector_PreviewMouseLeftButtonUp(
            object sender,
            MouseButtonEventArgs e) =>
            QueueSelectionCommit();

        private void Selector_LostMouseCapture(
            object sender,
            MouseEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Released)
            {
                QueueSelectionCommit();
            }
        }

        private void SelectionSettledTimer_Tick(object? sender, EventArgs e)
        {
            _selectionSettledTimer.Stop();
            CommitSelection();
        }

        private void QueueSelectionCommit()
        {
            _selectionSettledTimer.Stop();
            if (_isSelectionCommitQueued)
            {
                return;
            }

            _isSelectionCommitQueued = true;
            _selector.Dispatcher.BeginInvoke(
                DispatcherPriority.Background,
                () =>
                {
                    _isSelectionCommitQueued = false;
                    if (Mouse.LeftButton == MouseButtonState.Released)
                    {
                        CommitSelection();
                    }
                });
        }

        private void CommitSelection()
        {
            var selectedItem = _selector.SelectedItem;
            if (ReferenceEquals(_lastSettledSelection, selectedItem))
            {
                return;
            }

            _lastSettledSelection = selectedItem;
            _selector.SetCurrentValue(
                SettledSelectedItemProperty,
                selectedItem);
            _selector.RaiseEvent(
                new RoutedEventArgs(SelectionSettledEvent, _selector));
        }
    }
}
