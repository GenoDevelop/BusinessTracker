using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed class GetProductImageContentQueryValidator : AbstractValidator<GetProductImageContentQuery>
{
    public GetProductImageContentQueryValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.ImageId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Identyfikator zdjęcia jest wymagany.")
            .MustAsync((id, ct) => dbContext.ProductImages.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono zdjęcia.");
    }
}
