using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Exceptions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.UpdateContent;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Notes;

public sealed class UpdateNoteContent_Tests : BusinessTrackerUnitTestsBase<UpdateNoteContentCommandHandler>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldUpdateOnlyNoteContent()
    {
        var noteId = Arrange_BusinessTrackerDatabase(db =>
            db.Arrange_Note(name: "Stała nazwa", contentRtf: "stara treść").Id);
        const string updatedContent = @"{\rtf1\ul Nowa treść}";

        await Sut.Handle(
            new UpdateNoteContentCommand(noteId, updatedContent),
            TestContext.Current.CancellationToken);

        Assert_BusinessTrackerDatabase(db =>
        {
            var note = db.Notes.Single(x => x.Id == noteId);
            note.Name.Should().Be("Stała nazwa");
            note.ContentRtf.Should().Be(updatedContent);
        });
    }

    [Fact]
    public async Task Handle_ShouldRejectMissingNote()
    {
        var act = () => Sut.Handle(
            new UpdateNoteContentCommand(Guid.NewGuid(), "treść"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RequestValidationException>()
            .WithMessage("Walidacja żądania nie powiodła się.");
    }
}
