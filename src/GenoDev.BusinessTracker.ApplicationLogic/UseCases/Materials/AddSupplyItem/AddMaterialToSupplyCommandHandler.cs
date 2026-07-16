using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.AddSupplyItem;

public class AddMaterialToSupplyCommandHandler : IRequestHandler<AddMaterialToSupplyCommand, Unit>
{
    private readonly IBusinessTrackerDbContext _context;

    public AddMaterialToSupplyCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Unit> Handle(AddMaterialToSupplyCommand request, CancellationToken cancellationToken)
    {
        var supply = await _context.MaterialSupplies
            .FirstOrDefaultAsync(s => s.Id == request.MaterialSupplyId, cancellationToken);

        if (supply == null)
        {
            throw new KeyNotFoundException($"Material supply with ID {request.MaterialSupplyId} not found.");
        }

        var material = await _context.Materials
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Material with ID {request.MaterialId} not found.");
        }

        if (supply.Status == MaterialSupplyStatus.Received)
        {
            material.Amount += request.SetsAmount * request.UnitsInSet;
        }

        var item = new MaterialSupplyItem
        {
            Id = Guid.NewGuid(),
            MaterialSupplyId = request.MaterialSupplyId,
            MaterialId = request.MaterialId,
            SetsAmount = request.SetsAmount,
            UnitsInSet = request.UnitsInSet,
            SetNetPrice = request.SetNetPrice,
            SetGrossPrice = request.SetGrossPrice
        };

        _context.MaterialSupplyItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
