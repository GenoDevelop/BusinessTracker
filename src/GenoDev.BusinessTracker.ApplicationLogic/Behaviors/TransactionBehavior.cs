using GenoDev.Utilities.Core;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.Behaviors;

public sealed class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return TransactionHelper.Wrap(() => next(cancellationToken));
    }
}
