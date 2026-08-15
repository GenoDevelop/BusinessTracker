using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments;

internal static class StockAdjustmentAmountHelper
{
    public static async Task ApplyAsync(
        IBusinessTrackerDbContext db,
        StockAdjustmentItemType itemType,
        Guid itemId,
        double amount,
        bool isPrivate,
        CancellationToken cancellationToken)
    {
        switch (itemType)
        {
            case StockAdjustmentItemType.MaterialVariant:
            {
                var item = await db.MaterialVariants.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
                           ?? throw Exceptions.RequestValidationException.For("Nie znaleziono wariantu materiału.", nameof(itemId));
                if (isPrivate) item.TotalPrivateAmount += amount;
                else item.TotalCompanyAmount += amount;
                break;
            }
            case StockAdjustmentItemType.PackingMaterial:
            {
                var item = await db.PackingMaterials.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
                           ?? throw Exceptions.RequestValidationException.For("Nie znaleziono materiału pakowego.", nameof(itemId));
                if (isPrivate) item.TotalPrivateAmount += amount;
                else item.TotalCompanyAmount += amount;
                break;
            }
            case StockAdjustmentItemType.FixedAsset:
            {
                var item = await db.FixedAssets.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
                           ?? throw Exceptions.RequestValidationException.For("Nie znaleziono środka trwałego.", nameof(itemId));
                if (isPrivate) item.TotalPrivateAmount += amount;
                else item.TotalCompanyAmount += amount;
                break;
            }
            case StockAdjustmentItemType.Product:
            {
                if (isPrivate || !double.IsFinite(amount) || amount != Math.Truncate(amount) ||
                    amount < int.MinValue || amount > int.MaxValue)
                    throw Exceptions.RequestValidationException.For("Produkt nie może mieć stanu prywatnego, a jego ilość musi być liczbą całkowitą.");

                var item = await db.Products.FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken)
                           ?? throw Exceptions.RequestValidationException.For("Nie znaleziono produktu.", nameof(itemId));
                var adjustedAmount = (long)item.TotalAmount + (int)amount;
                if (adjustedAmount is < int.MinValue or > int.MaxValue)
                    throw Exceptions.RequestValidationException.For("Korekta spowodowałaby przekroczenie obsługiwanego zakresu ilości produktu.");
                item.TotalAmount = (int)adjustedAmount;
                break;
            }
            default:
                throw Exceptions.RequestValidationException.For("Typ pozycji korekty jest nieprawidłowy.", nameof(itemType));
        }
    }
}
