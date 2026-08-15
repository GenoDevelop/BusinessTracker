using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Delete;

public class DeleteStockAdjustmentCommandHandler(IBusinessTrackerDbContext db) : IRequestHandler<DeleteStockAdjustmentCommand>
{
    public async Task Handle(DeleteStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await db.StockAdjustments.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                         ?? throw Exceptions.RequestValidationException.For("Nie znaleziono korekty stanu.", nameof(request.Id));
        await StockAdjustmentAmountHelper.ApplyAsync(
            db, adjustment.ItemType, adjustment.GetItemId(), -adjustment.Amount, adjustment.IsPrivate, cancellationToken);
        db.StockAdjustments.Remove(adjustment);
        await db.SaveChangesAsync(cancellationToken);
    }
}
