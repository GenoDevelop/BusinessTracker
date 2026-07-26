using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Entities;
using MediatR;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Create;

public sealed class CreateFixedAssetCommandHandler : IRequestHandler<CreateFixedAssetCommand, Guid>
{
    private readonly IBusinessTrackerDbContext _context;

    public CreateFixedAssetCommandHandler(IBusinessTrackerDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateFixedAssetCommand request, CancellationToken cancellationToken)
    {
        var fixedAsset = new FixedAsset
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Ean = string.IsNullOrWhiteSpace(request.Ean) ? null : request.Ean,
            ManufacturerCode = string.IsNullOrWhiteSpace(request.ManufacturerCode) ? null : request.ManufacturerCode,
            Unit = request.Unit,
            Description = request.Description
        };

        _context.FixedAssets.Add(fixedAsset);
        await _context.SaveChangesAsync(cancellationToken);

        return fixedAsset.Id;
    }
}
