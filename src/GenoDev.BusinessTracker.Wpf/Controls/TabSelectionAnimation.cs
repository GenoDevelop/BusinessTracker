using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public static class TabSelectionAnimation
{
    internal const int AnimationMilliseconds = 125;

    private static readonly DependencyProperty StateProperty =
        DependencyProperty.RegisterAttached(
            "State",
            typeof(AnimationState),
            typeof(TabSelectionAnimation));

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TabSelectionAnimation),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject element) =>
        (bool)element.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject element, bool value) =>
        element.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(
        DependencyObject dependencyObject,
        DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not TabControl tabControl)
        {
            return;
        }

        (tabControl.GetValue(StateProperty) as AnimationState)?.Detach();
        tabControl.ClearValue(StateProperty);

        if (!(bool)e.NewValue)
        {
            return;
        }

        var state = new AnimationState(tabControl);
        tabControl.SetValue(StateProperty, state);
        state.Attach();
    }

    private sealed class AnimationState(TabControl tabControl)
    {
        private readonly TabControl _tabControl = tabControl;
        private ScaleTransform _surfaceScale = new();
        private TranslateTransform _surfaceTranslation = new();
        private ScaleTransform _indicatorScale = new();
        private TranslateTransform _indicatorTranslation = new();

        private FrameworkElement? _headerHost;
        private FrameworkElement? _selectionSurface;
        private FrameworkElement? _activeIndicator;
        private Rect? _lastSurfaceTarget;
        private Rect? _lastIndicatorTarget;
        private long _animationVersion;
        private bool _isAnimating;
        private bool _selectionUpdatePending;

        public void Attach()
        {
            _tabControl.Loaded += TabControl_Loaded;
            _tabControl.LayoutUpdated += TabControl_LayoutUpdated;
            _tabControl.SelectionChanged += TabControl_SelectionChanged;

            if (_tabControl.IsLoaded)
            {
                QueueUpdate(animate: false);
            }
        }

        public void Detach()
        {
            _tabControl.Loaded -= TabControl_Loaded;
            _tabControl.LayoutUpdated -= TabControl_LayoutUpdated;
            _tabControl.SelectionChanged -= TabControl_SelectionChanged;
        }

        private void TabControl_Loaded(object sender, RoutedEventArgs e) =>
            QueueUpdate(animate: false);

        private void TabControl_LayoutUpdated(object? sender, EventArgs e)
        {
            if (!_isAnimating && !_selectionUpdatePending)
            {
                UpdateSelection(animate: false);
            }
        }

        private void TabControl_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, _tabControl))
            {
                return;
            }

            QueueUpdate(
                animate: e.RemovedItems.Count > 0 &&
                         e.AddedItems.Count > 0 &&
                         SystemParameters.ClientAreaAnimation);
        }

        private void QueueUpdate(bool animate)
        {
            _selectionUpdatePending |= animate;
            _tabControl.Dispatcher.BeginInvoke(
                DispatcherPriority.Render,
                () =>
                {
                    _selectionUpdatePending = false;
                    UpdateSelection(animate);
                });
        }

        private void UpdateSelection(bool animate)
        {
            if (!ResolveTemplateParts() ||
                _tabControl.SelectedItem is null ||
                ResolveTabItem(_tabControl.SelectedItem) is not { } selectedTab)
            {
                return;
            }

            selectedTab.ApplyTemplate();
            var surfaceTarget = ResolveTarget(
                selectedTab,
                "SelectionSurfaceAnchor");
            var indicatorTarget = ResolveTarget(
                selectedTab,
                "ActiveIndicatorAnchor");

            if (surfaceTarget is null || indicatorTarget is null)
            {
                return;
            }

            if (!animate &&
                _lastSurfaceTarget is { } lastSurfaceTarget &&
                _lastIndicatorTarget is { } lastIndicatorTarget &&
                AreClose(lastSurfaceTarget, surfaceTarget.Value) &&
                AreClose(lastIndicatorTarget, indicatorTarget.Value))
            {
                return;
            }

            var shouldAnimate = animate &&
                                _lastSurfaceTarget is not null &&
                                _lastIndicatorTarget is not null;

            var animationVersion = ++_animationVersion;
            _isAnimating = shouldAnimate;

            PositionElement(
                _selectionSurface!,
                _surfaceScale,
                _surfaceTranslation,
                surfaceTarget.Value,
                shouldAnimate,
                animationVersion,
                completesTransition: false);
            PositionElement(
                _activeIndicator!,
                _indicatorScale,
                _indicatorTranslation,
                indicatorTarget.Value,
                shouldAnimate,
                animationVersion,
                completesTransition: true);

            _lastSurfaceTarget = surfaceTarget;
            _lastIndicatorTarget = indicatorTarget;
        }

        private static bool AreClose(Rect first, Rect second) =>
            Math.Abs(first.X - second.X) < 0.1d &&
            Math.Abs(first.Y - second.Y) < 0.1d &&
            Math.Abs(first.Width - second.Width) < 0.1d &&
            Math.Abs(first.Height - second.Height) < 0.1d;

        private bool ResolveTemplateParts()
        {
            _tabControl.ApplyTemplate();

            var headerHost = _tabControl.Template.FindName(
                "PART_HeaderAnimationHost",
                _tabControl) as FrameworkElement;
            var selectionSurface = _tabControl.Template.FindName(
                "PART_SelectionSurface",
                _tabControl) as FrameworkElement;
            var activeIndicator = _tabControl.Template.FindName(
                "PART_ActiveIndicator",
                _tabControl) as FrameworkElement;

            if (headerHost is null ||
                selectionSurface is null ||
                activeIndicator is null)
            {
                return false;
            }

            if (!ReferenceEquals(_selectionSurface, selectionSurface))
            {
                _selectionSurface = selectionSurface;
                _surfaceScale = new ScaleTransform();
                _surfaceTranslation = new TranslateTransform();
                _selectionSurface.RenderTransform = CreateTransformGroup(
                    _surfaceScale,
                    _surfaceTranslation);
                _selectionSurface.RenderTransformOrigin = new Point();
                _lastSurfaceTarget = null;
            }

            if (!ReferenceEquals(_activeIndicator, activeIndicator))
            {
                _activeIndicator = activeIndicator;
                _indicatorScale = new ScaleTransform();
                _indicatorTranslation = new TranslateTransform();
                _activeIndicator.RenderTransform = CreateTransformGroup(
                    _indicatorScale,
                    _indicatorTranslation);
                _activeIndicator.RenderTransformOrigin = new Point();
                _lastIndicatorTarget = null;
            }

            _headerHost = headerHost;
            return true;
        }

        private static TransformGroup CreateTransformGroup(
            ScaleTransform scale,
            TranslateTransform translation)
        {
            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(scale);
            transformGroup.Children.Add(translation);
            return transformGroup;
        }

        private TabItem? ResolveTabItem(object item) =>
            item as TabItem ??
            _tabControl.ItemContainerGenerator.ContainerFromItem(item) as TabItem;

        private Rect? ResolveTarget(TabItem selectedTab, string anchorName)
        {
            if (_headerHost is null ||
                selectedTab.Template.FindName(anchorName, selectedTab) is not
                    FrameworkElement anchor ||
                anchor.ActualWidth <= 0d ||
                anchor.ActualHeight <= 0d)
            {
                return null;
            }

            try
            {
                var origin = anchor.TranslatePoint(new Point(), _headerHost);
                return new Rect(
                    origin.X,
                    origin.Y,
                    anchor.ActualWidth,
                    anchor.ActualHeight);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private void PositionElement(
            FrameworkElement element,
            ScaleTransform scale,
            TranslateTransform translation,
            Rect target,
            bool animate,
            long animationVersion,
            bool completesTransition)
        {
            var currentX = translation.X;
            var currentY = translation.Y;
            var currentWidth = element.ActualWidth > 0d
                ? element.ActualWidth * scale.ScaleX
                : target.Width;
            var currentHeight = element.ActualHeight > 0d
                ? element.ActualHeight * scale.ScaleY
                : target.Height;

            StopAnimations(scale, translation);
            element.Width = target.Width;
            element.Height = target.Height;
            scale.ScaleX = 1d;
            scale.ScaleY = 1d;
            translation.X = target.X;
            translation.Y = target.Y;
            element.Opacity = 1d;

            if (!animate)
            {
                _isAnimating = false;
                return;
            }

            var duration = TimeSpan.FromMilliseconds(AnimationMilliseconds);
            var easing = new CubicEase { EasingMode = EasingMode.EaseInOut };

            scale.BeginAnimation(
                ScaleTransform.ScaleXProperty,
                CreateAnimation(
                    currentWidth / target.Width,
                    1d,
                    duration,
                    easing),
                HandoffBehavior.SnapshotAndReplace);
            scale.BeginAnimation(
                ScaleTransform.ScaleYProperty,
                CreateAnimation(
                    currentHeight / target.Height,
                    1d,
                    duration,
                    easing),
                HandoffBehavior.SnapshotAndReplace);
            translation.BeginAnimation(
                TranslateTransform.YProperty,
                CreateAnimation(currentY, target.Y, duration, easing),
                HandoffBehavior.SnapshotAndReplace);

            var horizontalAnimation = CreateAnimation(
                currentX,
                target.X,
                duration,
                easing);
            if (completesTransition)
            {
                horizontalAnimation.Completed += (_, _) =>
                {
                    if (animationVersion != _animationVersion)
                    {
                        return;
                    }

                    _isAnimating = false;
                    UpdateSelection(animate: false);
                };
            }

            translation.BeginAnimation(
                TranslateTransform.XProperty,
                horizontalAnimation,
                HandoffBehavior.SnapshotAndReplace);
        }

        private static void StopAnimations(
            ScaleTransform scale,
            TranslateTransform translation)
        {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            translation.BeginAnimation(TranslateTransform.XProperty, null);
            translation.BeginAnimation(TranslateTransform.YProperty, null);
        }

        private static DoubleAnimation CreateAnimation(
            double from,
            double to,
            Duration duration,
            IEasingFunction easing) =>
            new(from, to, duration)
            {
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            };
    }
}
