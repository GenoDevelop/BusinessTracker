using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Suppliers.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Suppliers;

public class GetAllSuppliers_Tests : BusinessTrackerUnitTestsBase<GetSuppliersQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedSuppliers()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            for (var i = 1; i <= 10; i++)
            {
                db.Arrange_Supplier(name: $"Supplier {i:D2}");
            }
        });

        var query = new GetSuppliersQuery(1, 3, SupplierSortBy.Name, false); // Page 1, Size 3 (0-based, so items 4, 5, 6)

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.Items[0].Name.Should().Be("Supplier 04");
        result.Items[1].Name.Should().Be("Supplier 05");
        result.Items[2].Name.Should().Be("Supplier 06");
    }

    [Fact]
    public async Task Handle_ShouldSortByNameDescending()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Supplier(name: "A");
            db.Arrange_Supplier(name: "C");
            db.Arrange_Supplier(name: "B");
        });

        var query = new GetSuppliersQuery(0, 10, SupplierSortBy.Name, true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Name).Should().ContainInOrder("C", "B", "A");
    }

    [Fact]
    public async Task Handle_ShouldSortByNip()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Supplier(name: "S1", nip: "333");
            db.Arrange_Supplier(name: "S2", nip: "111");
            db.Arrange_Supplier(name: "S3", nip: "222");
        });

        var query = new GetSuppliersQuery(0, 10, SupplierSortBy.Nip, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Nip).Should().ContainInOrder("111", "222", "333");
    }

    [Fact]
    public async Task Handle_ShouldSortByDescription()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Supplier(name: "S1", description: "C");
            db.Arrange_Supplier(name: "S2", description: "A");
            db.Arrange_Supplier(name: "S3", description: "B");
        });

        var query = new GetSuppliersQuery(0, 10, SupplierSortBy.Description, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Description).Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public async Task Handle_ShouldSortByWebsiteUrl()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Supplier(name: "S1", websiteUrl: "http://c.com");
            db.Arrange_Supplier(name: "S2", websiteUrl: "http://a.com");
            db.Arrange_Supplier(name: "S3", websiteUrl: "http://b.com");
        });

        var query = new GetSuppliersQuery(0, 10, SupplierSortBy.WebsiteUrl, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.WebsiteUrl).Should().ContainInOrder("http://a.com", "http://b.com", "http://c.com");
    }

    [Fact]
    public async Task Handle_ShouldCompleteAllData()
    {
        // Arrange
        var id = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Supplier(
                id: id,
                name: "Full Supplier",
                nip: "1234567890",
                description: "Test Description",
                websiteUrl: "http://test.com");
        });

        var query = new GetSuppliersQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(id);
        item.Name.Should().Be("Full Supplier");
        item.Nip.Should().Be("1234567890");
        item.Description.Should().Be("Test Description");
        item.WebsiteUrl.Should().Be("http://test.com");
    }
}
