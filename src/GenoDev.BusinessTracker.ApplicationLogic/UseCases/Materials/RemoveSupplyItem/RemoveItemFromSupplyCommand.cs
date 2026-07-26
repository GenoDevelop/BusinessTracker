using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.RemoveSupplyItem;

public record RemoveItemFromSupplyCommand(Guid Id) : IRequest;
