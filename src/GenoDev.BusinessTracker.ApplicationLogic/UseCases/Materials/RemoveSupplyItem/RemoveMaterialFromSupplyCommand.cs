using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;

public record RemoveMaterialFromSupplyCommand(Guid Id) : IRequest;
