using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.Create;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Notes;

public sealed class CreateNote_Tests : BusinessTrackerUnitTestsBase<CreateNoteCommandHandler>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
    }

    [Fact]
    public async Task Handle_ShouldCreateEmptyNoteAndReturnItsId()
    {
        var command = new CreateNoteCommand("Plan tygodnia");

        var result = await Sut.Handle(
            command,
            TestContext.Current.CancellationToken);

        Assert_BusinessTrackerDatabase(db =>
        {
            var note = db.Notes.Single(x => x.Id == result);
            note.Name.Should().Be(command.Name);
            note.ContentRtf.Should().BeEmpty();
        });
    }
}
