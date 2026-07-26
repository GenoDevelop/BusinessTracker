using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Delete;

public sealed class DeleteFixedAssetCommandHandler : IRequestHandler<DeleteFixedAssetCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public DeleteFixedAssetCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var fixedAsset = await _context.FixedAssets
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (fixedAsset == null)
        {
            throw new KeyNotFoundException($"Fixed asset with ID {request.Id} was not found.");
        }

        _context.FixedAssets.Remove(fixedAsset);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
