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
            throw Exceptions.RequestValidationException.For("Nie znaleziono receptury.", nameof(request.Id));
        }

        dbContext.ProductRecipes.Remove(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
