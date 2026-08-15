using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IValidator<TRequest>? validator = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validator is null)
        {
            return await next(cancellationToken);
        }

        var validationResult = await validator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(failure => new RequestValidationError(
                string.IsNullOrWhiteSpace(failure.PropertyName) ? null : failure.PropertyName,
                failure.ErrorMessage));

            throw new RequestValidationException(errors);
        }

        return await next(cancellationToken);
    }
}
