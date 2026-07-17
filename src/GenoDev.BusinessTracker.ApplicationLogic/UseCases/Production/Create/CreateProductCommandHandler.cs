using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.Create;

public class CreateProductCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Identifier = request.Identifier,
            Description = request.Description,
            Amount = 0
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
