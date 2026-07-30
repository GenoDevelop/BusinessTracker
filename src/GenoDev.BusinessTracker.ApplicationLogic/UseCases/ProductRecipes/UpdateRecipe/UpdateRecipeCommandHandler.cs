using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateRecipe;

public class UpdateRecipeCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateRecipeCommand>
{
    public async Task Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.ProductRecipes
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (recipe == null)
        {
            throw new KeyNotFoundException($"Recipe with ID {request.Id} was not found.");
        }

        var productExists = await dbContext.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} was not found.");
        }

        recipe.ProductId = request.ProductId;
        recipe.Name = request.Name;
        recipe.Description = request.Description ?? string.Empty;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
