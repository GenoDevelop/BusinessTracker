using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.UpdateProduction;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class UpdateProductionCommandValidator : AbstractValidator<UpdateProductionCommand>
{
    public UpdateProductionCommandValidator(IBusinessTrackerDbContext db, IValidator<MaterialVariantUsageDto> usageValidator)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Identyfikator produkcji jest wymagany.")
            .MustAsync((id, ct) => db.Productions.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono produkcji.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Ilość produkcji musi być większa od zera.");
        RuleFor(x => x.ProductionDate).NotEmpty().WithMessage("Data produkcji jest wymagana.");
        this.ValidateOptionalDescription(x => x.Description);
        RuleFor(x => x.UsedMaterials).NotNull().WithMessage("Lista zużytych materiałów jest wymagana.")
            .NotEmpty().WithMessage("Produkcja musi zawierać co najmniej jeden użyty materiał.")
            .Must(items => items is not null && items.Select(x => x.MaterialVariantId).Distinct().Count() == items.Count())
            .WithMessage("Ten sam wariant materiału nie może wystąpić w produkcji więcej niż raz.");
        RuleForEach(x => x.UsedMaterials).SetValidator(usageValidator);
        RuleFor(x => x).MustAsync(async (request, ct) =>
            {
                var submittedIds = request.UsedMaterials.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToArray();
                if (submittedIds.Length == 0) return true;
                var validCount = await db.ProductionMaterials.CountAsync(item =>
                    item.ProductionId == request.Id && submittedIds.Contains(item.Id), ct);
                return validCount == submittedIds.Distinct().Count();
            }).WithName(nameof(UpdateProductionCommand.UsedMaterials))
            .WithMessage("Co najmniej jedna pozycja materiałowa nie należy do edytowanej produkcji.");
    }
}