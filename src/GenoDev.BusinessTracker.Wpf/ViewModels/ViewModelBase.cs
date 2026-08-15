using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace GenoDev.BusinessTracker.Wpf.ViewModels;

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
    /// Replaces a data-bound collection and returns the refreshed instance of the
    /// previously selected item. Callers can suppress selection-change side effects
    /// while this synchronous replacement is in progress.
    /// </summary>
    protected static T? ReplaceItemsPreservingSelection<T, TKey>(
        ObservableCollection<T> target,
        IEnumerable<T> source,
        T? selectedItem,
        Func<T, TKey> keySelector)
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

        if (!hasSelection)
        {
            return null;
        }

        return target.FirstOrDefault(item =>
            EqualityComparer<TKey>.Default.Equals(
                keySelector(item),
                selectedKey!));
    }
}
