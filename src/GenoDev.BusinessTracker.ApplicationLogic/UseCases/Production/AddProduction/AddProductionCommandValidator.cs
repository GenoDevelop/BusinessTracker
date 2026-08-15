using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.AddProduction;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class AddProductionCommandValidator : AbstractValidator<AddProductionCommand>
{
    public AddProductionCommandValidator(IBusinessTrackerDbContext db, IValidator<MaterialVariantUsageDto> usageValidator)
    {
        RuleFor(x => x.ProductId).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Produkt jest wymagany.")
            .MustAsync((id, ct) => db.Products.AnyAsync(item => item.Id == id, ct)).WithMessage("Nie znaleziono produktu.");
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Ilość produkcji musi być większa od zera.");
        RuleFor(x => x.ProductionDate).NotEmpty().WithMessage("Data produkcji jest wymagana.");
        this.ValidateOptionalDescription(x => x.Description);
        RuleFor(x => x.UsedMaterials).NotNull().WithMessage("Lista zużytych materiałów jest wymagana.")
            .NotEmpty().WithMessage("Produkcja musi zawierać co najmniej jeden użyty materiał.")
            .Must(items => items is not null && items.Select(x => x.MaterialVariantId).Distinct().Count() == items.Count())
            .WithMessage("Ten sam wariant materiału nie może wystąpić w produkcji więcej niż raz.");
        RuleForEach(x => x.UsedMaterials).SetValidator(usageValidator);
    }

    public sealed class MaterialVariantUsageDtoValidator : AbstractValidator<MaterialVariantUsageDto>
    {
        public MaterialVariantUsageDtoValidator(IBusinessTrackerDbContext db)
        {
            RuleFor(x => x.MaterialVariantId).Cascade(CascadeMode.Stop).NotEmpty()
                .WithMessage("Wariant materiału jest wymagany.")
                .MustAsync((id, ct) => db.MaterialVariants.AnyAsync(item => item.Id == id, ct))
                .WithMessage("Nie znaleziono wariantu materiału.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Zużycie materiału musi być większe od zera.");
        }
    }
}