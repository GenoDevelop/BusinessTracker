using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GenoDev.BusinessTracker.Wpf.Controls;

[TemplatePart(Name = ContentCachePartName, Type = typeof(Grid))]
public sealed class TransitioningContentControl : ContentControl
{
    private const string ContentCachePartName = "PART_ContentCache";

    private readonly Dictionary<object, ContentPresenter> _presenters =
        new(ReferenceEqualityComparer.Instance);
    private readonly DispatcherTimer _contentSwitchTimer;

    private Grid? _contentCache;
    private object? _currentContent;
    private object? _pendingContent;
    private long _transitionVersion;

    public TransitioningContentControl()
    {
        _contentSwitchTimer = new DispatcherTimer(
            DispatcherPriority.Render,
            Dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(
                TabSelectionAnimation.AnimationMilliseconds / 2d)
        };
        _contentSwitchTimer.Tick += ContentSwitchTimer_Tick;
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentCache = GetTemplateChild(ContentCachePartName) as Grid;
        _presenters.Clear();
        _currentContent = null;

        if (Content is not null)
        {
            ShowImmediately(Content);
        }
    }

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);

        ++_transitionVersion;
        _contentSwitchTimer.Stop();
        NormalizePresenters();

        if (_contentCache is null ||
            !IsLoaded ||
            _currentContent is null ||
            newContent is null ||
            !SystemParameters.ClientAreaAnimation)
        {
            _pendingContent = null;
            ShowImmediately(newContent);
            return;
        }

        if (ReferenceEquals(_currentContent, newContent))
        {
            _pendingContent = null;
            return;
        }

        _pendingContent = newContent;
        _contentSwitchTimer.Start();
    }

    private void ContentSwitchTimer_Tick(object? sender, EventArgs e)
    {
        _contentSwitchTimer.Stop();

        var content = _pendingContent;
        _pendingContent = null;
        if (content is null ||
            _contentCache is null ||
            ReferenceEquals(_currentContent, content))
        {
            return;
        }

        var previousPresenter = ResolvePresenter(_currentContent);
        var selectedPresenter = GetOrCreatePresenter(content);
        var transitionVersion = _transitionVersion;

        selectedPresenter.Visibility = Visibility.Visible;
        selectedPresenter.Opacity = 1d;
        Panel.SetZIndex(selectedPresenter, 0);

        if (previousPresenter is not null)
        {
            previousPresenter.Visibility = Visibility.Visible;
            previousPresenter.Opacity = 1d;
            Panel.SetZIndex(previousPresenter, 1);
        }

        _currentContent = content;

        // Let WPF construct and measure a first-time view while the previous
        // live layer still covers it. The fade starts only after that work.
        Dispatcher.BeginInvoke(
            DispatcherPriority.Loaded,
            () => FadeOutPreviousPresenter(
                previousPresenter,
                selectedPresenter,
                transitionVersion));
    }

    private void FadeOutPreviousPresenter(
        ContentPresenter? previousPresenter,
        ContentPresenter selectedPresenter,
        long transitionVersion)
    {
        if (transitionVersion != _transitionVersion)
        {
            return;
        }

        if (previousPresenter is null ||
            ReferenceEquals(previousPresenter, selectedPresenter))
        {
            NormalizePresenters();
            return;
        }

        var fadeOut = new DoubleAnimation(
            1d,
            0d,
            TimeSpan.FromMilliseconds(
                TabSelectionAnimation.AnimationMilliseconds))
        {
            // Start halfway through the indicator movement, while retaining
            // the quicker initial fade and gentle EaseOut finish.
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        fadeOut.Completed += (_, _) =>
        {
            if (transitionVersion == _transitionVersion)
            {
                NormalizePresenters();
            }
        };

        previousPresenter.BeginAnimation(
            UIElement.OpacityProperty,
            fadeOut,
            HandoffBehavior.SnapshotAndReplace);
    }

    private void ShowImmediately(object? content)
    {
        _currentContent = content;
        if (content is null || _contentCache is null)
        {
            NormalizePresenters();
            return;
        }

        var presenter = GetOrCreatePresenter(content);
        presenter.Visibility = Visibility.Visible;
        presenter.Opacity = 1d;
        Panel.SetZIndex(presenter, 0);
        NormalizePresenters();
    }

    private ContentPresenter GetOrCreatePresenter(object content)
    {
        if (_presenters.TryGetValue(content, out var presenter))
        {
            return presenter;
        }

        presenter = new ContentPresenter
        {
            Content = content,
            ContentTemplate = ContentTemplate,
            ContentTemplateSelector = ContentTemplateSelector,
            ContentStringFormat = ContentStringFormat,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Visibility = Visibility.Collapsed
        };
        _presenters.Add(content, presenter);
        _contentCache!.Children.Add(presenter);
        return presenter;
    }

    private ContentPresenter? ResolvePresenter(object? content) =>
        content is not null && _presenters.TryGetValue(content, out var presenter)
            ? presenter
            : null;

    private void NormalizePresenters()
    {
        foreach (var (content, presenter) in _presenters)
        {
            presenter.BeginAnimation(UIElement.OpacityProperty, null);
            presenter.Opacity = 1d;
            presenter.Visibility = ReferenceEquals(content, _currentContent)
                ? Visibility.Visible
                : Visibility.Collapsed;
            Panel.SetZIndex(presenter, 0);
        }
    }
}
