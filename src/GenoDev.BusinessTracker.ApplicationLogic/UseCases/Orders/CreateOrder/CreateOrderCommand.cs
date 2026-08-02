using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;

public record OrderData(
    string? Description,
    DateTime OrderDate,
    string? OrderIdentifier,
    string? PaymentIdentifier,
    string? TrackingNumber,
    Carrier? Carrier,
    bool CompanyOrder,
    string OrderSource,
    decimal ShippingNetCost,
    decimal ShippingGrossCost,
    decimal ShippingNetClientPrice,
    decimal ShippingGrossClientPrice);

public record ClientData(
    string? ClientName,
    string? Street,
    string? PostCode,
    string? City,
    string? Email,
    string? Phone,
    string? ClientDescription);

public record CreateOrderCommand(OrderData Order, ClientData Client) : IRequest<Guid>;
