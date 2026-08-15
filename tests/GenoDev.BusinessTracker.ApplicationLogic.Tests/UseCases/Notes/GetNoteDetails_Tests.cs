using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetDetails;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Notes;

public sealed class GetNoteDetails_Tests : BusinessTrackerUnitTestsBase<GetNoteDetailsQueryHandler>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldReturnCompleteNoteDetails()
    {
        var noteId = Arrange_BusinessTrackerDatabase(db =>
            db.Arrange_Note(
                name: "Instrukcja",
                contentRtf: @"{\rtf1\b Ważne}").Id);

        var result = await Sut.Handle(
            new GetNoteDetailsQuery(noteId),
            TestContext.Current.CancellationToken);

        result.Id.Should().Be(noteId);
        result.Name.Should().Be("Instrukcja");
        result.ContentRtf.Should().Be(@"{\rtf1\b Ważne}");
    }

    [Fact]
    public async Task Handle_ShouldRejectMissingNote()
    {
        var act = () => Sut.Handle(
            new GetNoteDetailsQuery(Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RequestValidationException>()
            .WithMessage("Walidacja żądania nie powiodła się.");
    }
}
