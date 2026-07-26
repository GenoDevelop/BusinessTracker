using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Update;

public sealed class UpdateFixedAssetCommandHandler : IRequestHandler<UpdateFixedAssetCommand>
{
    private readonly IBusinessTrackerDbContext _context;

    public UpdateFixedAssetCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var fixedAsset = await _context.FixedAssets
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (fixedAsset == null)
        {
            throw new KeyNotFoundException($"Fixed asset with ID {request.Id} was not found.");
        }

        fixedAsset.Name = request.Name;
        fixedAsset.Ean = string.IsNullOrWhiteSpace(request.Ean) ? null : request.Ean;
        fixedAsset.ManufacturerCode = string.IsNullOrWhiteSpace(request.ManufacturerCode) ? null : request.ManufacturerCode;
        fixedAsset.Unit = request.Unit;
        fixedAsset.Description = request.Description;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
