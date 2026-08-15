using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Delete;

public sealed class DeleteStockAdjustmentCommandValidator : AbstractValidator<DeleteStockAdjustmentCommand>
{
    public DeleteStockAdjustmentCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Korekta jest wymagana.")
            .MustAsync((id, ct) => db.StockAdjustments.AnyAsync(x => x.Id == id, ct)).WithMessage("Nie znaleziono korekty stanu.");
    }
}
