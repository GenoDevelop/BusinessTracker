using System.Windows;
using System.Windows.Threading;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using System.Diagnostics;

namespace GenoDev.BusinessTracker.Wpf;

internal sealed class GlobalExceptionHandler
{
    public void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        ShowException(args.Exception);
        args.Handled = true;
    }

    public void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        var exception = args.Exception;
        args.SetObserved();

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is not null && !dispatcher.HasShutdownStarted)
        {
            dispatcher.BeginInvoke(() => ShowException(exception));
        }
    }

    private static void ShowException(Exception exception)
    {
        Trace.TraceError(exception.ToString());

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
