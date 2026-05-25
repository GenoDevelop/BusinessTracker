using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.TaxRates;

public class CreateTaxRate_Tests : BusinessTrackerUnitTestsBase<CreateTaxRateCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateTaxRate_WhenValidDataProvided()
    {
        // Arrange
        var command = new CreateTaxRateCommand("VAT 23%", 0.23m, 0.23m);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TaxRateName.Should().Be("VAT 23%");
        result.VatRate.Should().Be(0.23m);
        result.TaxRateValue.Should().Be(0.23m);

        AssertBusinessTracker_Database(db =>
        {
            var entity = db.TaxRates.Single(x => x.Id == result.Id);
            entity.TaxRateName.Should().Be("VAT 23%");
            entity.VatRate.Should().Be(0.23m);
            entity.TaxRateValue.Should().Be(0.23m);
        });
    }
}
