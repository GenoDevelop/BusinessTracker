using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Production.GetProductionSummary;

namespace GenoDev.BusinessTracker.ApplicationLogic.Validation;

public sealed class GetProductionSummaryQueryValidator : AbstractValidator<GetProductionSummaryQuery>
{
    public GetProductionSummaryQueryValidator() => this.ValidatePaging(x => x.PageIndex, x => x.PageSize);
}