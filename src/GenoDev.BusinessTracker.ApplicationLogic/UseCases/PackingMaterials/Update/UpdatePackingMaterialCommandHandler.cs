using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Update;

public sealed class UpdatePackingMaterialCommandHandler : IRequestHandler<UpdatePackingMaterialCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public UpdatePackingMaterialCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdatePackingMaterialCommand request, CancellationToken cancellationToken)
    {
        var packingMaterial = await _context.PackingMaterials
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (packingMaterial == null)
        {
            throw new KeyNotFoundException($"Packing material with ID {request.Id} was not found.");
        }

        packingMaterial.Name = request.Name;
        packingMaterial.Ean = string.IsNullOrWhiteSpace(request.Ean) ? null : request.Ean;
        packingMaterial.ManufacturerCode = string.IsNullOrWhiteSpace(request.ManufacturerCode) ? null : request.ManufacturerCode;
        packingMaterial.Unit = request.Unit;
        packingMaterial.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
