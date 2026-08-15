using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.FixedAssets.Delete;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class DeleteFixedAssetCommandValidator : AbstractValidator<DeleteFixedAssetCommand>
{
    public DeleteFixedAssetCommandValidator(IBusinessTrackerDbContext db)
    {
        RuleFor(x => x.Id)
            .Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator środka trwałego jest wymagany.")
            .MustAsync((id, ct) => db.FixedAssets.AnyAsync(item => item.Id == id, ct))
            .WithMessage("Nie znaleziono środka trwałego.");
    }
}