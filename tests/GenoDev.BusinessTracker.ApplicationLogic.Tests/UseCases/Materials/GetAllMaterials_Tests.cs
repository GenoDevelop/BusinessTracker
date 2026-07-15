using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Materials.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Materials;

public class GetAllMaterials_Tests : BusinessTrackerUnitTestsBase<GetMaterialsQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaginatedMaterials()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            for (var i = 1; i <= 10; i++)
            {
                db.Arrange_Material(name: $"Material {i:D2}");
            }
        });

        var query = new GetMaterialsQuery(1, 3, MaterialSortBy.Name, false); // Page 1, Size 3 (0-based, so items 4, 5, 6)

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(10);
        result.HasNextPage.Should().BeTrue();
        result.Items[0].Name.Should().Be("Material 04");
        result.Items[1].Name.Should().Be("Material 05");
        result.Items[2].Name.Should().Be("Material 06");
    }

    [Fact]
    public async Task Handle_ShouldSortByNameDescending()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "A");
            db.Arrange_Material(name: "C");
            db.Arrange_Material(name: "B");
        });

        var query = new GetMaterialsQuery(0, 10, MaterialSortBy.Name, true);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Name).Should().ContainInOrder("C", "B", "A");
    }

    [Fact]
    public async Task Handle_ShouldSortByEan()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "M1", ean: "333");
            db.Arrange_Material(name: "M2", ean: "111");
            db.Arrange_Material(name: "M3", ean: "222");
        });

        var query = new GetMaterialsQuery(0, 10, MaterialSortBy.Ean, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Ean).Should().ContainInOrder("111", "222", "333");
    }

    [Fact]
    public async Task Handle_ShouldSortByDescription()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "M1", description: "C");
            db.Arrange_Material(name: "M2", description: "A");
            db.Arrange_Material(name: "M3", description: "B");
        });

        var query = new GetMaterialsQuery(0, 10, MaterialSortBy.Description, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Description).Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public async Task Handle_ShouldSortByUnit()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "M1", unit: "kg");
            db.Arrange_Material(name: "M2", unit: "cm");
            db.Arrange_Material(name: "M3", unit: "m");
        });

        var query = new GetMaterialsQuery(0, 10, MaterialSortBy.Unit, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Unit).Should().ContainInOrder("cm", "kg", "m");
    }

    [Fact]
    public async Task Handle_ShouldSortByAmount()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "M1", amount: 30);
            db.Arrange_Material(name: "M2", amount: 10);
            db.Arrange_Material(name: "M3", amount: 20);
        });

        var query = new GetMaterialsQuery(0, 10, MaterialSortBy.Amount, false);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Select(x => x.Amount).Should().ContainInOrder(10, 20, 30);
    }

    [Fact]
    public async Task Handle_ShouldCompleteAllData()
    {
        // Arrange
        var id = Guid.NewGuid();
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(
                id: id,
                name: "Full Material",
                ean: "1234567890",
                description: "Test Description",
                unit: "kg",
                amount: 123.45);
        });

        var query = new GetMaterialsQuery(0, 10);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        var item = result.Items[0];
        item.Id.Should().Be(id);
        item.Name.Should().Be("Full Material");
        item.Ean.Should().Be("1234567890");
        item.Description.Should().Be("Test Description");
        item.Unit.Should().Be("kg");
        item.Amount.Should().Be(123.45);
    }

    [Fact]
    public async Task Handle_ShouldFilterByName()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "Apple");
            db.Arrange_Material(name: "Banana");
            db.Arrange_Material(name: "Cherry");
        });

        var query = new GetMaterialsQuery(0, 10, NameFilter: "an");

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("Banana");
    }

    [Fact]
    public async Task Handle_ShouldFilterByAmount_GreaterThan()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "M1", amount: 10);
            db.Arrange_Material(name: "M2", amount: 20);
            db.Arrange_Material(name: "M3", amount: 30);
        });

        var query = new GetMaterialsQuery(0, 10, AmountFilter: 20, AmountOperator: NumericOperator.GreaterThan);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("M3");
    }

    [Fact]
    public async Task Handle_ShouldFilterByAmount_Equal()
    {
        // Arrange
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Material(name: "M1", amount: 10.5);
            db.Arrange_Material(name: "M2", amount: 20.0);
        });

        var query = new GetMaterialsQuery(0, 10, AmountFilter: 10.5, AmountOperator: NumericOperator.Equal);

        // Act
        var result = await Sut.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be("M1");
    }
}
