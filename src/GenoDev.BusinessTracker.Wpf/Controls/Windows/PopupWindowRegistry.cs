using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GenoDev.BusinessTracker.Wpf.Controls;

public sealed class PopupWindowEntry : IDisposable, INotifyPropertyChanged
{
    private readonly DependencyPropertyDescriptor? _titleDescriptor;
    private readonly DependencyPropertyDescriptor? _topmostDescriptor;

    internal PopupWindowEntry(PopupWindow window)
    {
        Window = window;
        _titleDescriptor = DependencyPropertyDescriptor.FromProperty(
            System.Windows.Window.TitleProperty,
            typeof(System.Windows.Window));
        _titleDescriptor?.AddValueChanged(window, OnTitleChanged);
        _topmostDescriptor = DependencyPropertyDescriptor.FromProperty(
            System.Windows.Window.TopmostProperty,
            typeof(System.Windows.Window));
        _topmostDescriptor?.AddValueChanged(window, OnTopmostChanged);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PopupWindow Window { get; }

    public string Title => string.IsNullOrWhiteSpace(Window.Title)
        ? "Okno bez tytułu"
        : Window.Title;

    public bool IsPinned => Window.Topmost;

    public void BringToFront() => Window.BringToFront();

    public void TogglePin() => Window.TogglePinned();

    public void Close() => Window.CloseFromWindowMenu();

    public void Dispose()
    {
        _titleDescriptor?.RemoveValueChanged(Window, OnTitleChanged);
        _topmostDescriptor?.RemoveValueChanged(Window, OnTopmostChanged);
    }

    private void OnTitleChanged(object? sender, EventArgs e) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));

    private void OnTopmostChanged(object? sender, EventArgs e) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsPinned)));
}

public static class PopupWindowRegistry
{
    private static readonly ObservableCollection<PopupWindowEntry> MutableWindows = new();
    private static readonly Dictionary<PopupWindow, PopupWindowEntry> EntriesByWindow = new();

    static PopupWindowRegistry()
    {
        Windows = new ReadOnlyObservableCollection<PopupWindowEntry>(MutableWindows);
    }

    public static ReadOnlyObservableCollection<PopupWindowEntry> Windows { get; }

    internal static void Register(PopupWindow window)
    {
        if (EntriesByWindow.ContainsKey(window))
        {
            return;
        }

        var entry = new PopupWindowEntry(window);
        EntriesByWindow.Add(window, entry);
        MutableWindows.Add(entry);
    }

    internal static void Unregister(PopupWindow window)
    {
        if (!EntriesByWindow.Remove(window, out var entry))
        {
            return;
        }

        MutableWindows.Remove(entry);
        entry.Dispose();
    }
}
