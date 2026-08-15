using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;
using Microsoft.EntityFrameworkCore;
using GenoDev.BusinessTracker.ApplicationLogic.Validation;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Update;

public sealed class UpdateStockAdjustmentCommandValidator : AbstractValidator<UpdateStockAdjustmentCommand>
{
    public UpdateStockAdjustmentCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Korekta jest wymagana.")
            .MustAsync((id, ct) => db.StockAdjustments.AnyAsync(x => x.Id == id, ct)).WithMessage("Nie znaleziono korekty stanu.");
        RuleFor(x => x.Date).NotEqual(default(DateOnly)).WithMessage("Data korekty jest wymagana.");
        RuleFor(x => new StockAdjustmentInput(x.ItemType, x.ItemId, x.Amount, x.IsPrivate))
            .SetValidator(new StockAdjustmentInputValidator(db));
        this.ValidateOptionalDescription(x => x.Description);
    }
}
