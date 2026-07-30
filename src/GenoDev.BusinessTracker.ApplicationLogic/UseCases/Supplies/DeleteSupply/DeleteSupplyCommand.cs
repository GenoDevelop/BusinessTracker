using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;

public record DeleteSupplyCommand(Guid Id) : IRequest;
