using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipeMaterial;

public class UpdateRecipeMaterialCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateRecipeMaterialCommand>
{
    public async Task Handle(UpdateRecipeMaterialCommand request, CancellationToken cancellationToken)
    {
        var recipeMaterial = await dbContext.ProductRecipeMaterials
            .FirstOrDefaultAsync(rm => rm.Id == request.Id, cancellationToken);

        if (recipeMaterial == null)
        {
            throw Exceptions.RequestValidationException.For("Nie znaleziono składnika receptury.", nameof(request.Id));
        }

        var alreadyExists = await dbContext.ProductRecipeMaterials
            .AnyAsync(rm => rm.Id != request.Id && rm.ProductRecipeId == recipeMaterial.ProductRecipeId && rm.MaterialId == request.MaterialId, cancellationToken);

        if (alreadyExists)
        {
            throw Exceptions.RequestValidationException.For("Ten materiał jest już dodany do receptury.", nameof(request.MaterialId));
        }

        recipeMaterial.MaterialId = request.MaterialId;
        recipeMaterial.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
