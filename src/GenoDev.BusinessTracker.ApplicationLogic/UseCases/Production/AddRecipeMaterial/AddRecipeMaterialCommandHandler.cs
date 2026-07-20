using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddRecipeMaterial;

public class AddRecipeMaterialCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<AddRecipeMaterialCommand>
{
    public async Task Handle(AddRecipeMaterialCommand request, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.ProductRecipes
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, cancellationToken);

        if (recipe == null)
        {
            throw new KeyNotFoundException($"Recipe with ID {request.RecipeId} was not found.");
        }

        var material = await dbContext.Materials
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (material == null)
        {
            throw new KeyNotFoundException($"Material with ID {request.MaterialId} was not found.");
        }

        var recipeMaterial = new ProductRecipeMaterial
        {
            Id = Guid.NewGuid(),
            ProductRecipeId = request.RecipeId,
            MaterialId = request.MaterialId,
            RequiredAmount = request.Amount
        };

        dbContext.ProductRecipeMaterials.Add(recipeMaterial);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
