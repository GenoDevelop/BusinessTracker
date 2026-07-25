using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Create;

public class CreateMaterialCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<CreateMaterialCommand, Guid>
{
    public async Task<Guid> Handle(CreateMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = new Material
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Ean = string.IsNullOrWhiteSpace(request.Ean) ? null : request.Ean,
            Description = request.Description,
            Unit = request.Unit,
            Amount = 0
        };

        dbContext.Materials.Add(material);
        await dbContext.SaveChangesAsync(cancellationToken);

        return material.Id;
    }
}
