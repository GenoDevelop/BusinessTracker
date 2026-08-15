using AutoFixture;
using FluentAssertions;
using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Delete;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.GetStockAdjustments;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.StockAdjustments.Update;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.StockAdjustments;

public class StockAdjustmentValidators_Tests : BusinessTrackerUnitTestsBase<CreateStockAdjustmentsCommandValidator>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<IValidator<UpdateStockAdjustmentCommand>, UpdateStockAdjustmentCommandValidator>();
        services.AddTransient<IValidator<DeleteStockAdjustmentCommand>, DeleteStockAdjustmentCommandValidator>();
        services.AddTransient<IValidator<GetStockAdjustmentsQuery>, GetStockAdjustmentsQueryValidator>();
    }

    [Fact]
    public async Task CreateValidator_ShouldRejectEmptyItemsAndInvalidProductRules()
    {
        var emptyResult = await Sut.ValidateAsync(
            new CreateStockAdjustmentsCommand(default, [], new string('x', 4001)), TestContext.Current.CancellationToken);
        var productResult = await Sut.ValidateAsync(
            new CreateStockAdjustmentsCommand(DateOnly.FromDateTime(DateTime.Today),
                [new StockAdjustmentInput(StockAdjustmentItemType.Product, Guid.NewGuid(), 1.5, true)]),
            TestContext.Current.CancellationToken);

        emptyResult.Errors.Select(x => x.PropertyName).Should().Contain([
            nameof(CreateStockAdjustmentsCommand.Date), nameof(CreateStockAdjustmentsCommand.Items),
            nameof(CreateStockAdjustmentsCommand.Description)]);
        productResult.Errors.Should().Contain(x => x.ErrorMessage == "Ilość produktu musi być liczbą całkowitą.");
        productResult.Errors.Should().Contain(x => x.ErrorMessage == "Produkty nie mają stanu prywatnego.");
    }

    [Fact]
    public async Task QueryValidator_ShouldRejectInvalidPagingEnumsAndDateRange()
    {
        var validator = _sp.GetRequiredService<IValidator<GetStockAdjustmentsQuery>>();
        var result = await validator.ValidateAsync(new GetStockAdjustmentsQuery(
            PageIndex: -1, PageSize: 0, SortBy: (StockAdjustmentSortBy)999,
            AmountOperator: (NumericOperator)999,
            StartDate: new DateOnly(2026, 8, 16), EndDate: new DateOnly(2026, 8, 15)),
            TestContext.Current.CancellationToken);

        result.Errors.Should().HaveCountGreaterThanOrEqualTo(5);
        result.Errors.Should().OnlyContain(x => !string.IsNullOrWhiteSpace(x.ErrorMessage));
    }
}
