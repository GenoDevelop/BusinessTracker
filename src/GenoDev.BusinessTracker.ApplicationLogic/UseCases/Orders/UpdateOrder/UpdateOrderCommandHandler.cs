using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Sales.UpdateOrder;

public class UpdateOrderCommandHandler : IRequestHandler<UpdateOrderCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public UpdateOrderCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.ClientDetails)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw Exceptions.RequestValidationException.For("Nie znaleziono zamówienia.", nameof(request.OrderId));

        order.Description = request.Order.Description;
        order.OrderDate = request.Order.OrderDate;
        order.OrderIdentifier = request.Order.OrderIdentifier;
        order.PaymentIdentifier = request.Order.PaymentIdentifier;
        order.TrackingNumber = request.Order.TrackingNumber;
        order.Carrier = request.Order.Carrier;
        order.Status = request.Order.Status;
        order.CompanyOrder = request.Order.CompanyOrder;
        order.OrderSource = request.Order.OrderSource;
        order.ShippingNetCost = request.Order.ShippingNetCost;
        order.ShippingGrossCost = request.Order.ShippingGrossCost;
        order.ShippingNetClientPrice = request.Order.ShippingNetClientPrice;
        order.ShippingGrossClientPrice = request.Order.ShippingGrossClientPrice;

        if (order.ClientDetails == null)
        {
            order.ClientDetails = new ClientDetails
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Order = order
            };
            _context.ClientDetails.Add(order.ClientDetails);
        }

        order.ClientDetails.ClientName = request.Client.ClientName;
        order.ClientDetails.Street = request.Client.Street;
        order.ClientDetails.PostCode = request.Client.PostCode;
        order.ClientDetails.City = request.Client.City;
        order.ClientDetails.Email = request.Client.Email;
        order.ClientDetails.Phone = request.Client.Phone;
        order.ClientDetails.Description = request.Client.ClientDescription;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
