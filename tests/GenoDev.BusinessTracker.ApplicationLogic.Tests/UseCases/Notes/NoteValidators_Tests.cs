using AutoFixture;
using FluentAssertions;
using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.Create;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetAll;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.GetDetails;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Notes.UpdateContent;
using GenoDev.BusinessTracker.ApplicationLogic.Validation;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Notes;

public sealed class NoteValidators_Tests : BusinessTrackerUnitTestsBase<GetNoteDetailsQueryValidator>
{
    protected override void RegisterMockedDependencies(
        IServiceCollection services,
        IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<IValidator<CreateNoteCommand>, CreateNoteCommandValidator>();
        services.AddTransient<IValidator<GetNotesQuery>, GetNotesQueryValidator>();
        services.AddTransient<IValidator<UpdateNoteContentCommand>, UpdateNoteContentCommandValidator>();
    }

    [Fact]
    public async Task CreateValidator_ShouldRejectEmptyAndTooLongName()
    {
        var validator = _sp.GetRequiredService<IValidator<CreateNoteCommand>>();

        var emptyResult = await validator.ValidateAsync(
            new CreateNoteCommand(string.Empty),
            TestContext.Current.CancellationToken);
        var tooLongResult = await validator.ValidateAsync(
            new CreateNoteCommand(new string('x', 201)),
            TestContext.Current.CancellationToken);

        emptyResult.Errors.Should().Contain(x => x.PropertyName == nameof(CreateNoteCommand.Name));
        tooLongResult.Errors.Should().Contain(x => x.PropertyName == nameof(CreateNoteCommand.Name));
    }

    [Fact]
    public async Task QueryValidator_ShouldRejectInvalidPagingAndSort()
    {
        var validator = _sp.GetRequiredService<IValidator<GetNotesQuery>>();

        var result = await validator.ValidateAsync(
            new GetNotesQuery(-1, 0, (NoteSortBy)999),
            TestContext.Current.CancellationToken);

        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public async Task DetailAndUpdateValidators_ShouldRejectMissingNoteAndNullContent()
    {
        var detailResult = await Sut.ValidateAsync(
            new GetNoteDetailsQuery(Guid.NewGuid()),
            TestContext.Current.CancellationToken);
        var updateValidator = _sp.GetRequiredService<IValidator<UpdateNoteContentCommand>>();
        var updateResult = await updateValidator.ValidateAsync(
            new UpdateNoteContentCommand(Guid.NewGuid(), null!),
            TestContext.Current.CancellationToken);

        detailResult.Errors.Should().Contain(x => x.ErrorMessage == "Nie znaleziono notatki.");
        updateResult.Errors.Should().Contain(x => x.ErrorMessage == "Nie znaleziono notatki.");
        updateResult.Errors.Should().Contain(x => x.PropertyName == nameof(UpdateNoteContentCommand.ContentRtf));
    }
}
