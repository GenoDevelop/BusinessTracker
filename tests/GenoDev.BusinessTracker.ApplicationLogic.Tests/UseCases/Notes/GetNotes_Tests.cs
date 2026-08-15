using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetAll;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Notes;

public sealed class GetNotes_Tests : BusinessTrackerUnitTestsBase<GetNotesQueryHandler>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldFilterSortAndPageNotesOnServer()
    {
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Note(name: "Plan zakupów");
            db.Arrange_Note(name: "Plan produkcji");
            db.Arrange_Note(name: "Inne");
        });

        var result = await Sut.Handle(
            new GetNotesQuery(
                PageIndex: 0,
                PageSize: 1,
                SortBy: NoteSortBy.Name,
                IsDescending: true,
                NameFilter: "plan"),
            TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Plan zakupów");
        result.Items[0].Id.Should().NotBeEmpty();
        result.TotalCount.Should().Be(2);
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldUseZeroBasedPaging()
    {
        Arrange_BusinessTrackerDatabase(db =>
        {
            db.Arrange_Note(name: "A");
            db.Arrange_Note(name: "B");
            db.Arrange_Note(name: "C");
        });

        var result = await Sut.Handle(
            new GetNotesQuery(1, 1),
            TestContext.Current.CancellationToken);

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("B");
        result.TotalCount.Should().Be(3);
        result.HasNextPage.Should().BeTrue();
    }
}
