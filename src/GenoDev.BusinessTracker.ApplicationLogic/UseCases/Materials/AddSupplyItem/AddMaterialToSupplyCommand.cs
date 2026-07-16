using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddSupplyItem;

public record AddMaterialToSupplyCommand(
    Guid MaterialSupplyId,
    Guid MaterialId,
    int SetsAmount,
    double UnitsInSet,
    decimal SetNetPrice,
    decimal SetGrossPrice) : IRequest<Unit>;
