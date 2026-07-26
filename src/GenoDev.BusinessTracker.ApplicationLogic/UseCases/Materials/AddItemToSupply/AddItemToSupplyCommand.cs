using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddItemToSupply;

public record AddItemToSupplyCommand(
    Guid SupplyId,
    SupplyItemType ItemType,
    Guid ItemId,
    int SetsAmount,
    double UnitsInSet,
    decimal SetNetPrice,
    decimal SetGrossPrice,
    bool PrivateSupply) : IRequest<Unit>;
