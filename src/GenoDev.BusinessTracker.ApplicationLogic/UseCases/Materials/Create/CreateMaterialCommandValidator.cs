using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.Create;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class CreateMaterialCommandValidator : AbstractValidator<CreateMaterialCommand>
{
    public CreateMaterialCommandValidator()
    {
        this.ValidateRequiredName(x => x.Name, "Nazwa");
        this.ValidateOptionalDescription(x => x.Description);
    }
}