using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Delete;

public class DeleteMaterialCommandHandler(IBusinessTrackerDbContext dbContext)
    : IRequestHandler<DeleteMaterialCommand>
{
    public async Task Handle(DeleteMaterialCommand request, CancellationToken cancellationToken)
    {
        var material = await dbContext.Materials
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (material == null) return;

        dbContext.Materials.Remove(material);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
