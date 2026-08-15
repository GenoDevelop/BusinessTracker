using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

public readonly record struct EditorCloseResult(
    bool WasSaved,
    bool WasDeleted,
    Guid? CreatedEntityId)
{
    public static EditorCloseResult Cancelled => default;

    public static EditorCloseResult Saved(Guid? createdEntityId = null) =>
        new(true, false, createdEntityId);

    public static EditorCloseResult Deleted => new(false, true, null);

    public bool RequiresRefresh => WasSaved || WasDeleted;
}

public partial class ViewModelBase : ObservableObject
{
    [ObservableProperty]
    private bool _isBusy;

    /// <summary>
    /// Gives WPF a chance to process input, bindings, and rendering before starting
    /// synchronous query preparation in an asynchronous loading method.
    /// </summary>
    protected static async Task YieldToUiAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
        {
            await Task.Yield();
            return;
        }

        await dispatcher.InvokeAsync(
            static () => { },
            DispatcherPriority.Background);
    }

    /// <summary>
    /// Replaces a data-bound collection and returns the preferred item when present,
    /// otherwise the refreshed instance of the previous selection. Callers can
    /// suppress selection-change side effects while this synchronous replacement
    /// is in progress.
    /// </summary>
    protected static T? ReplaceItemsPreservingSelection<T>(
        ObservableCollection<T> target,
        IEnumerable<T> source,
        T? selectedItem,
        Func<T, Guid> keySelector,
        Guid? preferredItemKey = null)
        where T : class
    {
        var hasSelection = selectedItem is not null;
        var selectedKey = hasSelection
            ? keySelector(selectedItem!)
            : default;

        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }

        if (preferredItemKey is not null)
        {
            var preferredItem = target.FirstOrDefault(item =>
                keySelector(item) == preferredItemKey.Value);

            if (preferredItem is not null)
            {
                return preferredItem;
            }
        }

        if (!hasSelection)
        {
            return null;
        }

        return target.FirstOrDefault(item =>
            keySelector(item) == selectedKey);
    }
}
