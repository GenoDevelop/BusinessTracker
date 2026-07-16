using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetSupplyDetails;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;
using AutoFixture;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class GetMaterialSupplyDetailsQueryHandler_Tests : BusinessTrackerUnitTestsBase<GetMaterialSupplyDetailsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnDetailedInformationCorrectly()
    {
        // Arrange
        Guid supplyId = Guid.Empty;
        Arrange_BusinessTrackerDatabase(db =>
        {
            var supplier = db.Arrange_Supplier(name: "Test Supplier");
            var supply = db.Arrange_MaterialSupply(
                supplier: supplier,
                orderDate: new DateTime(2026, 7, 16),
                description: "Test Description",
                invoiceNo: "INV/123");
            supplyId = supply.Id;

            db.Arrange_MaterialSupplyItem(supply, setsAmount: 2, setNetPrice: 10m, setGrossPrice: 12.3m);
            db.Arrange_MaterialSupplyItem(supply, setsAmount: 1, setNetPrice: 50m, setGrossPrice: 61.5m);
        });

        var query = new GetMaterialSupplyDetailsQuery(supplyId);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SupplierName.Should().Be("Test Supplier");
        result.OrderDate.Should().Be(new DateTime(2026, 7, 16));
        result.Description.Should().Be("Test Description");
        result.InvoiceNo.Should().Be("INV/123");
        result.TotalNetPrice.Should().Be(70m); // (2 * 10) + (1 * 50)
        result.TotalGrossPrice.Should().Be(86.1m); // (2 * 12.3) + (1 * 61.5) = 24.6 + 61.5 = 86.1
    }

    [Fact]
    public async Task Handle_ShouldReturnNullIfSupplyNotFound()
    {
        // Arrange
        var query = new GetMaterialSupplyDetailsQuery(Guid.NewGuid());

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
