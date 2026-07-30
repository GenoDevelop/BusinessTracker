using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.CreateRecipe;

public class CreateRecipeCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<CreateRecipeCommand, Guid>
{
    public async Task<Guid> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        var productExists = await dbContext.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
        }

        var recipe = new ProductRecipe
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            Name = request.Name,
            Description = request.Description ?? string.Empty
        };

        dbContext.ProductRecipes.Add(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);

        return recipe.Id;
    }
}
