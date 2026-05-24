using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class GetAllSuppliers_Tests : BusinessTrackerUnitTestsBase<GetAllSuppliersQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, AutoFixture.IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedSuppliers()
    {
        // Arrange
        ArrangeBusinessTracker_Database(db =>
        {
            db.Suppliers.AddRange(Enumerable.Range(1, 10).Select(i => new Supplier
            {
                SupplierName = $"Supplier {i:D2}",
                Description = $"Description {i}"
            }));
        });

        var query = new GetAllSuppliersQuery(1, 3, SupplierSortBy.Name, false); // Page 1, Size 3 (0-based, so items 4, 5, 6)

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.Items[0].SupplierName.Should().Be("Supplier 04");
        result.Items[1].SupplierName.Should().Be("Supplier 05");
        result.Items[2].SupplierName.Should().Be("Supplier 06");
    }

    [Fact]
    public async Task Handle_ShouldSortByNameDescending()
    {
        // Arrange
        ArrangeBusinessTracker_Database(db =>
        {
            db.Suppliers.Add(new Supplier { SupplierName = "A" });
            db.Suppliers.Add(new Supplier { SupplierName = "C" });
            db.Suppliers.Add(new Supplier { SupplierName = "B" });
        });

        var query = new GetAllSuppliersQuery(0, 10, SupplierSortBy.Name, true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.SupplierName).Should().ContainInOrder("C", "B", "A");
    }
}
