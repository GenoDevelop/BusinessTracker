using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class CreateSupplier_Tests : BusinessTrackerUnitTestsBase<CreateSupplierCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateSupplierWithAllData()
    {
        // Arrange
        var command = new CreateSupplierCommand(
            Name: "Full Supplier",
            Nip: "1234567890",
            Description: "Multi-line\nDescription",
            WebsiteUrl: "https://example.com");

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.FirstOrDefault(x => x.Id == resultId);
            supplier.Should().NotBeNull();
            supplier!.Name.Should().Be(command.Name);
            supplier.Nip.Should().Be(command.Nip);
            supplier.Description.Should().Be(command.Description);
            supplier.WebsiteUrl.Should().Be(command.WebsiteUrl);
            supplier.MaterialSupplies.Should().BeEmpty();
        });
    }

    [Fact]
    public async Task Handle_ShouldCreateSupplierWithOnlyRequiredData()
    {
        // Arrange
        var command = new CreateSupplierCommand(
            Name: "Minimal Supplier",
            Nip: null,
            Description: null,
            WebsiteUrl: null);

        // Act
        var resultId = await Sut.Handle(command, CancellationToken.None);

        // Assert
        AssertBusinessTracker_Database(db =>
        {
            var supplier = db.Suppliers.FirstOrDefault(x => x.Id == resultId);
            supplier.Should().NotBeNull();
            supplier!.Name.Should().Be(command.Name);
            supplier.Nip.Should().BeNull();
            supplier.Description.Should().BeNull();
            supplier.WebsiteUrl.Should().BeNull();
        });
    }
}
