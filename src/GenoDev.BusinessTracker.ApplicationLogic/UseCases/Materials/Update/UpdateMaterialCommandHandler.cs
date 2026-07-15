using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Update;

public class UpdateMaterialCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<UpdateMaterialCommand>
{
    public async Task Handle(UpdateMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await dbContext.Materials
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (material == null) return;

        material.Name = request.Name;
        material.Ean = request.Ean;
        material.Description = request.Description;
        material.Unit = request.Unit;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
