namespace GenoDev.BusinessTracker.ApplicationLogic.Exceptions;

public sealed record RequestValidationError(string? Source, string Message);

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IEnumerable<RequestValidationError> errors)
        : base("Request validation failed.")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<RequestValidationError> Errors { get; }
}
