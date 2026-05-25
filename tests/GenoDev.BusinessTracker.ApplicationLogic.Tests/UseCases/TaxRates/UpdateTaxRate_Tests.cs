using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.TaxRates.Update;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.TaxRates;

public class UpdateTaxRate_Tests : BusinessTrackerUnitTestsBase<UpdateTaxRateCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTaxRate_WhenValidDataProvided()
    {
        // Arrange
        var entity = new TaxRate { TaxRateName = "Old Name", VatRate = 0.05m, TaxRateValue = 0.05m };
        ArrangeBusinessTracker_Database(db => db.TaxRates.Add(entity));

        var command = new UpdateTaxRateCommand(entity.Id, "New Name", 0.08m, 0.08m);

        // Act
        var result = await Sut.Handle(command, CancellationToken.None);

        // Assert
        result.TaxRateName.Should().Be("New Name");
        result.VatRate.Should().Be(0.08m);
        result.TaxRateValue.Should().Be(0.08m);

        AssertBusinessTracker_Database(db =>
        {
            var updated = db.TaxRates.Single(x => x.Id == entity.Id);
            updated.TaxRateName.Should().Be("New Name");
            updated.VatRate.Should().Be(0.08m);
            updated.TaxRateValue.Should().Be(0.08m);
        });
    }
}