using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteProduction;

public record DeleteProductionCommand(Guid Id) : IRequest<Unit>;
