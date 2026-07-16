using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;

public record EditMaterialSupplyItemCommand(
    Guid Id,
    Guid MaterialId,
    int SetsAmount,
    double UnitsInSet,
    decimal SetNetPrice,
    decimal SetGrossPrice) : IRequest;
