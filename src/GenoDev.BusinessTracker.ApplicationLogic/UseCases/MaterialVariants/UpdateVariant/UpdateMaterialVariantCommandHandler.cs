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
            throw Exceptions.RequestValidationException.For("Nie znaleziono wariantu materiału.", nameof(request.Id));
        }

        variant.Name = request.Name;
        variant.Ean = request.Ean;
        variant.ManufacturerCode = request.ManufacturerCode;
        variant.Unit = request.Unit;
        variant.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
