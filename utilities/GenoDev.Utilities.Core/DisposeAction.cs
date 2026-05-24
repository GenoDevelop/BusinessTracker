namespace GenoDev.Utilities.Core;

/// <summary>
/// Represents a disposable action that executes a provided callback when disposed.
/// </summary>
public sealed class DisposeAction : IDisposable
{
    private readonly Action _dispose;
    private bool _disposed;

    /// <summary>
    /// Represents an object that executes a specified action when disposed.
    /// </summary>
    public DisposeAction(Action dispose) => _dispose = dispose;

    /// <summary>
    /// Releases resources and executes the associated disposal action.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _dispose();
        _disposed = true;
    }
}

/// <summary>
/// Represents an asynchronously disposable action that executes a provided task when disposed asynchronously.
/// </summary>
public sealed class AsyncDisposeAction : IAsyncDisposable
{
    private readonly Task _dispose;
    private bool _disposed;

    /// <summary>
    /// Represents an asynchronously disposable action that executes a provided task when disposed asynchronously.
    /// </summary>
    public AsyncDisposeAction(Task dispose) => _dispose = dispose;

    /// <summary>
    /// Releases resources and executes the associated asynchronous disposal task.
    /// </summary>
    /// <returns>A ValueTask that represents the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        await _dispose;
        _disposed = true;
    }
}