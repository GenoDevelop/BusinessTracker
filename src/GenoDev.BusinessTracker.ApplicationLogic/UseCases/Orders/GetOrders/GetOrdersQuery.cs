using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.GetOrders;

public record ClientDetailsDto(
    string? ClientName,
    string? Street,
    string? PostCode,
    string? City,
    string? Email,
    string? Phone,
    string? Description);

public record OrderListDto(
    Guid Id,
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
    decimal ShippingGrossClientPrice,
    decimal TotalNetPrice,
    decimal TotalGrossPrice,
    ClientDetailsDto? ClientDetails);

public record GetOrdersQuery(
    int PageIndex,
    int PageSize,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : IRequest<PagedList<OrderListDto>>;
