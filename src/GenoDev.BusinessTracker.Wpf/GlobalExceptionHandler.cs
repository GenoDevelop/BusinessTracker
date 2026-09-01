using System.Windows;
using System.Windows.Threading;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using System.Diagnostics;

namespace GenoDev.BusinessTracker.Wpf;

internal sealed class GlobalExceptionHandler
{
    private static readonly TimeSpan RepeatedExceptionQuietPeriod = TimeSpan.FromSeconds(5);
    private readonly object _exceptionStateLock = new();
    private int _isShowingException;
    private string? _lastExceptionFingerprint;
    private DateTimeOffset _lastExceptionAt;

    public void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        ShowExceptionOnce(args.Exception);
        args.Handled = true;
    }

    public void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var exception = args.Exception;
        args.SetObserved();

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.HasShutdownStarted)
        {
            dispatcher.BeginInvoke(() => ShowExceptionOnce(exception));
        }
    }

    private void ShowExceptionOnce(Exception exception)
    {
        var diagnosticMessage = $"[{DateTimeOffset.Now:O}] Nieobsłużony wyjątek aplikacji:{Environment.NewLine}{exception}";
        Trace.TraceError(diagnosticMessage);
        Console.Error.WriteLine(diagnosticMessage);

        // A modal error dialog pumps the dispatcher. If the same UI failure occurs again while
        // that dialog is opening, showing another dialog recursively can end in a stack overflow.
        if (Interlocked.CompareExchange(ref _isShowingException, 1, 0) != 0)
        {
            return;
        }

        try
        {
            if (!ShouldShowException(exception)) return;
            ShowException(exception);
        }
        finally
        {
            Interlocked.Exchange(ref _isShowingException, 0);
        }
    }

    private bool ShouldShowException(Exception exception)
    {
        var baseException = exception.GetBaseException();
        var fingerprint = $"{baseException.GetType().FullName}|{baseException.Message}";
        var now = DateTimeOffset.UtcNow;

        lock (_exceptionStateLock)
        {
            var isContinuousRepeat = string.Equals(
                    fingerprint,
                    _lastExceptionFingerprint,
                    StringComparison.Ordinal) &&
                now - _lastExceptionAt < RepeatedExceptionQuietPeriod;

            _lastExceptionFingerprint = fingerprint;
            _lastExceptionAt = now;
            return !isContinuousRepeat;
        }
    }

    private static void ShowException(Exception exception)
    {

        var validationException = FindValidationException(exception);
        if (validationException is not null)
        {
            var message = string.Join(
                Environment.NewLine,
                validationException.Errors.Select(error => error.Message).Distinct());

            MessageBox.Show(
                message,
                "Nie można wykonać operacji",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        MessageBox.Show(
            "Wystąpił nieoczekiwany błąd. Spróbuj ponownie. Jeśli problem będzie się powtarzał, uruchom aplikację ponownie.",
            "Błąd aplikacji",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static RequestValidationException? FindValidationException(Exception exception)
    {
        if (exception is RequestValidationException validationException)
        {
            return validationException;
        }

        if (exception is AggregateException aggregateException)
        {
            return aggregateException.Flatten().InnerExceptions
                .Select(FindValidationException)
                .FirstOrDefault(result => result is not null);
        }

        return exception.InnerException is null
            ? null
            : FindValidationException(exception.InnerException);
    }
}
