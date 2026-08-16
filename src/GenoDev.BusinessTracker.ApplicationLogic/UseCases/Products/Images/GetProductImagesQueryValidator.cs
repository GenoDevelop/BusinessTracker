using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenoDev.BusinessTracker.ApplicationLogic.UseCases.Products.Images;

public sealed class GetProductImagesQueryValidator : AbstractValidator<GetProductImagesQuery>
{
    public GetProductImagesQueryValidator(IBusinessTrackerDbContext dbContext)
    {
        RuleFor(x => x.ProductId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Identyfikator produktu jest wymagany.")
            .MustAsync((id, ct) => dbContext.Products.AnyAsync(x => x.Id == id, ct))
            .WithMessage("Nie znaleziono produktu.");
    }
}
