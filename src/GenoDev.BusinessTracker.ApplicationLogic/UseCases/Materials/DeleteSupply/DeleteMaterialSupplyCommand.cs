using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteSupply;

public record DeleteMaterialSupplyCommand(Guid Id) : IRequest;
