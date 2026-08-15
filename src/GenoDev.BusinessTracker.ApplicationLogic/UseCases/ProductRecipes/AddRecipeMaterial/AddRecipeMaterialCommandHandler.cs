using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddRecipeMaterial;

public class AddRecipeMaterialCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<AddRecipeMaterialCommand, Guid>
{
    public async Task<Guid> Handle(AddRecipeMaterialCommand request, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.ProductRecipes
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, cancellationToken);

        if (recipe == null)
            throw Exceptions.RequestValidationException.For("Nie znaleziono receptury.", nameof(request.RecipeId));

        var material = await dbContext.Materials
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, cancellationToken);

        if (material == null)
            throw Exceptions.RequestValidationException.For("Nie znaleziono materiału.", nameof(request.MaterialId));

        var alreadyExists = await dbContext.ProductRecipeMaterials
            .AnyAsync(rm => rm.ProductRecipeId == request.RecipeId && rm.MaterialId == request.MaterialId, cancellationToken);

        if (alreadyExists)
        {
            throw Exceptions.RequestValidationException.For("Ten materiał jest już dodany do receptury.", nameof(request.MaterialId));
        }

        var recipeMaterial = new ProductRecipeMaterial
        {
            Id = Guid.NewGuid(),
            ProductRecipeId = request.RecipeId,
            MaterialId = request.MaterialId,
            Description = request.Description
        };

        dbContext.ProductRecipeMaterials.Add(recipeMaterial);
        await dbContext.SaveChangesAsync(cancellationToken);
        return recipeMaterial.Id;
    }
}
