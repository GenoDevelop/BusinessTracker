using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.DeleteRecipe;

public class DeleteRecipeCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<DeleteRecipeCommand>
{
    public async Task Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = await dbContext.ProductRecipes
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (recipe == null)
        {
            throw new KeyNotFoundException($"Recipe with ID {request.Id} was not found.");
        }

        dbContext.ProductRecipes.Remove(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
