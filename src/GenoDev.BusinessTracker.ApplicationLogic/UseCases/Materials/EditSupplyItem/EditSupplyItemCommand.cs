using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.EditSupplyItem;

public record EditSupplyItemCommand(
    Guid Id,
    StorageItemType ItemType,
    Guid ItemId,
    int SetsAmount,
    double UnitsInSet,
    decimal SetNetPrice,
    decimal SetGrossPrice,
    bool PrivateSupply) : IRequest;
