using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.RemoveRecipeMaterial;

public class RemoveRecipeMaterialCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<RemoveRecipeMaterialCommand>
{
    public async Task Handle(RemoveRecipeMaterialCommand request, CancellationToken cancellationToken)
    {
        var recipeMaterial = await dbContext.ProductRecipeMaterials
            .FirstOrDefaultAsync(rm => rm.Id == request.Id, cancellationToken);

        if (recipeMaterial == null)
        {
            throw new KeyNotFoundException($"Recipe material with ID {request.Id} was not found.");
        }

        dbContext.ProductRecipeMaterials.Remove(recipeMaterial);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
