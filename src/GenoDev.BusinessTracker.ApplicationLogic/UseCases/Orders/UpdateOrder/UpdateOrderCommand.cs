using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;

public record UpdateOrderData(
    string? Description,
    DateTime OrderDate,
    string? OrderIdentifier,
    string? PaymentIdentifier,
    string? TrackingNumber,
    Carrier? Carrier,
    OrderStatus Status,
    bool CompanyOrder,
    string OrderSource,
    decimal ShippingNetCost,
    decimal ShippingGrossCost,
    decimal ShippingNetClientPrice,
    decimal ShippingGrossClientPrice);

public record UpdateClientData(
    string? ClientName,
    string? Street,
    string? PostCode,
    string? City,
    string? Email,
    string? Phone,
    string? ClientDescription);

public record UpdateOrderCommand(Guid OrderId, UpdateOrderData Order, UpdateClientData Client) : IRequest;
