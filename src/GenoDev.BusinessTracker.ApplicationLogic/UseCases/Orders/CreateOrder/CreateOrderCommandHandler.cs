using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Orders.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IBusinessTrackerDbContext _context;

    public CreateOrderCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Description = request.Order.Description,
            OrderDate = request.Order.OrderDate,
            OrderIdentifier = request.Order.OrderIdentifier,
            PaymentIdentifier = request.Order.PaymentIdentifier,
            TrackingNumber = request.Order.TrackingNumber,
            Carrier = request.Order.Carrier,
            Status = OrderStatus.New,
            CompanyOrder = request.Order.CompanyOrder,
            OrderSource = request.Order.OrderSource,
            ShippingNetCost = request.Order.ShippingNetCost,
            ShippingGrossCost = request.Order.ShippingGrossCost,
            ShippingNetClientPrice = request.Order.ShippingNetClientPrice,
            ShippingGrossClientPrice = request.Order.ShippingGrossClientPrice
        };

        var clientDetails = new ClientDetails
        {
            Id = Guid.NewGuid(),
            OrderId = order.Id,
            ClientName = request.Client.ClientName,
            Street = request.Client.Street,
            PostCode = request.Client.PostCode,
            City = request.Client.City,
            Email = request.Client.Email,
            Phone = request.Client.Phone,
            Description = request.Client.ClientDescription,
            Order = order
        };

        order.ClientDetails = clientDetails;

        _context.Orders.Add(order);
        _context.ClientDetails.Add(clientDetails);

        await _context.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}
