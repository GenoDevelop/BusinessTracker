using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.UpdateVariant;

public class UpdateMaterialVariantCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateMaterialVariantCommand>
{
    public async Task Handle(UpdateMaterialVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await dbContext.MaterialVariants
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (variant == null)
        {
            throw new InvalidOperationException($"Material variant with ID {request.Id} does not exist.");
        }

        variant.Name = request.Name;
        variant.Ean = request.Ean;
        variant.ManufacturerCode = request.ManufacturerCode;
        variant.Unit = request.Unit;
        variant.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
