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
            throw Exceptions.RequestValidationException.For("Nie znaleziono receptury.", nameof(request.Id));
        }

        var productExists = await dbContext.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);
        if (!productExists)
        {
            throw Exceptions.RequestValidationException.For("Nie znaleziono produktu.", nameof(request.ProductId));
        }

        recipe.ProductId = request.ProductId;
        recipe.Name = request.Name;
        recipe.Description = request.Description ?? string.Empty;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
