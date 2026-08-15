using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;
using GenoDev.BusinessTracker.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Update;

public class UpdateStockAdjustmentCommandHandler(IBusinessTrackerDbContext db) : IRequestHandler<UpdateStockAdjustmentCommand>
{
    public async Task Handle(UpdateStockAdjustmentCommand request, CancellationToken cancellationToken)
    {
        var adjustment = await db.StockAdjustments.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                         ?? throw Exceptions.RequestValidationException.For("Nie znaleziono korekty stanu.", nameof(request.Id));

        await StockAdjustmentAmountHelper.ApplyAsync(
            db, adjustment.ItemType, adjustment.GetItemId(), -adjustment.Amount, adjustment.IsPrivate, cancellationToken);
        await StockAdjustmentAmountHelper.ApplyAsync(
            db, request.ItemType, request.ItemId, request.Amount,
            request.ItemType != StockAdjustmentItemType.Product && request.IsPrivate, cancellationToken);

        adjustment.ItemType = request.ItemType;
        adjustment.Amount = request.Amount;
        adjustment.IsPrivate = request.ItemType != StockAdjustmentItemType.Product && request.IsPrivate;
        adjustment.Date = request.Date;
        adjustment.Description = request.Description;
        CreateStockAdjustmentsCommandHandler.AssignItem(adjustment, request.ItemId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
