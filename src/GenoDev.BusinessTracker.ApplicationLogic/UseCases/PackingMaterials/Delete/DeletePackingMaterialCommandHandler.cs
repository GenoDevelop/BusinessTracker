using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.PackingMaterials.Delete;

public sealed class DeletePackingMaterialCommandHandler : IRequestHandler<DeletePackingMaterialCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public DeletePackingMaterialCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeletePackingMaterialCommand request, CancellationToken cancellationToken)
    {
        var packingMaterial = await _context.PackingMaterials
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (packingMaterial == null)
        {
            throw Exceptions.RequestValidationException.For("Nie znaleziono materiału pakowego.", nameof(request.Id));
        }

        _context.PackingMaterials.Remove(packingMaterial);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
