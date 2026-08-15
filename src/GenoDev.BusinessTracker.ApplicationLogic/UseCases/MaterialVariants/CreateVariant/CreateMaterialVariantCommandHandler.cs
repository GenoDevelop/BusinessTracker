using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.CreateVariant;

public class CreateMaterialVariantCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<CreateMaterialVariantCommand, Guid>
{
    public async Task<Guid> Handle(CreateMaterialVariantCommand request, CancellationToken cancellationToken)
    {
        var materialExists = await dbContext.Materials.AnyAsync(x => x.Id == request.MaterialId, cancellationToken);
        if (!materialExists)
        {
            throw Exceptions.RequestValidationException.For("Nie znaleziono materiału.", nameof(request.MaterialId));
        }

        var variant = new MaterialVariant
        {
            Id = Guid.NewGuid(),
            MaterialId = request.MaterialId,
            Name = request.Name,
            Ean = string.IsNullOrWhiteSpace(request.Ean) ? null : request.Ean,
            ManufacturerCode = string.IsNullOrWhiteSpace(request.ManufacturerCode) ? null : request.ManufacturerCode,
            Unit = request.Unit,
            Description = request.Description,
            TotalUsedAmount = 0,
            TotalCompanyAmount = 0,
            TotalPrivateAmount = 0
        };

        dbContext.MaterialVariants.Add(variant);
        await dbContext.SaveChangesAsync(cancellationToken);

        return variant.Id;
    }
}
