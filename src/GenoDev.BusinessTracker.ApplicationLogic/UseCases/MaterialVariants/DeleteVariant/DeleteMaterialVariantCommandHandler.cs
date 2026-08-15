using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.DeleteVariant;

public class DeleteMaterialVariantCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<DeleteMaterialVariantCommand>
{
    public async Task Handle(DeleteMaterialVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await dbContext.MaterialVariants
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (variant == null)
        {
            throw Exceptions.RequestValidationException.For("Nie znaleziono wariantu materiału.", nameof(request.Id));
        }

        dbContext.MaterialVariants.Remove(variant);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
