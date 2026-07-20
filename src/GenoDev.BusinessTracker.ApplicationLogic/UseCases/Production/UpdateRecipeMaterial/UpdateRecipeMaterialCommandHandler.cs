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

        recipeMaterial.RequiredAmount = request.Amount;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
