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
            throw new KeyNotFoundException($"Recipe material with ID {request.Id} was not found.");
        }

        var alreadyExists = await dbContext.ProductRecipeMaterials
            .AnyAsync(rm => rm.Id != request.Id && rm.ProductRecipeId == recipeMaterial.ProductRecipeId && rm.MaterialId == request.MaterialId, cancellationToken);

        if (alreadyExists)
        {
            throw new InvalidOperationException($"Material with ID {request.MaterialId} is already added to this recipe.");
        }

        recipeMaterial.MaterialId = request.MaterialId;
        recipeMaterial.Description = request.Description;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
