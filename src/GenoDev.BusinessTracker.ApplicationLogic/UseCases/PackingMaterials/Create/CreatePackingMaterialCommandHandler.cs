using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Create;

public sealed class CreatePackingMaterialCommandHandler : IRequestHandler<CreatePackingMaterialCommand, Guid>
{
    private readonly IBusinessTrackerDbContext _context;

    public CreatePackingMaterialCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreatePackingMaterialCommand request, CancellationToken cancellationToken)
    {
        var packingMaterial = new PackingMaterial
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Ean = string.IsNullOrWhiteSpace(request.Ean) ? null : request.Ean,
            ManufacturerCode = string.IsNullOrWhiteSpace(request.ManufacturerCode) ? null : request.ManufacturerCode,
            Unit = request.Unit,
            Description = request.Description
        };

        _context.PackingMaterials.Add(packingMaterial);
        await _context.SaveChangesAsync(cancellationToken);

        return packingMaterial.Id;
    }
}
