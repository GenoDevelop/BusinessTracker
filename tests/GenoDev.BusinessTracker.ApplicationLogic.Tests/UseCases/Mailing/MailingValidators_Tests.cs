using AutoFixture;
using FluentAssertions;
using FluentValidation;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.DeleteMailingItem;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetOutgoingEmailHistory;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetResendComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.QueueOutgoingEmail;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.RenderMailPreview;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailSnippet;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailTemplate;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveSmtpAccount;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Mailing;

public sealed class MailingValidators_Tests : BusinessTrackerUnitTestsBase<SaveSmtpAccountCommandValidator>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<IValidator<SaveMailSnippetCommand>, SaveMailSnippetCommandValidator>();
        services.AddTransient<IValidator<SaveMailTemplateCommand>, SaveMailTemplateCommandValidator>();
        services.AddTransient<IValidator<QueueOutgoingEmailCommand>, QueueOutgoingEmailCommandValidator>();
        services.AddTransient<IValidator<GetMailComposerQuery>, GetMailComposerQueryValidator>();
        services.AddTransient<IValidator<GetOutgoingEmailHistoryQuery>, GetOutgoingEmailHistoryQueryValidator>();
        services.AddTransient<IValidator<GetResendComposerQuery>, GetResendComposerQueryValidator>();
        services.AddTransient<IValidator<RenderMailPreviewQuery>, RenderMailPreviewQueryValidator>();
        services.AddTransient<IValidator<DeleteMailingItemCommand>, DeleteMailingItemCommandValidator>();
    }

    [Fact]
    public async Task AccountAndSnippetValidators_ShouldRejectInvalidShape()
    {
        var account = await Sut.ValidateAsync(new SaveSmtpAccountCommand(null, "", "", 0, true, "", null, "bad", "", null, false, true), TestContext.Current.CancellationToken);
        var snippet = await _sp.GetRequiredService<IValidator<SaveMailSnippetCommand>>().ValidateAsync(
            new SaveMailSnippetCommand(null, "BAD KEY", "", null, "{{> nested }}", true), TestContext.Current.CancellationToken);
        account.IsValid.Should().BeFalse();
        account.Errors.Should().Contain(x => x.PropertyName == nameof(SaveSmtpAccountCommand.Password));
        snippet.Errors.Should().Contain(x => x.PropertyName == nameof(SaveMailSnippetCommand.Key))
            .And.Contain(x => x.PropertyName == nameof(SaveMailSnippetCommand.HtmlContent));
    }

    [Fact]
    public async Task TemplateAndQueueValidators_ShouldRejectUnsupportedSubjectAndOversizedPayload()
    {
        var content = new byte[(int)MailAttachmentConstraints.MaxTotalSizeBytes + 1];
        var template = await _sp.GetRequiredService<IValidator<SaveMailTemplateCommand>>().ValidateAsync(
            new SaveMailTemplateCommand(null, null, "T", "{{> footer }}", "<p>x</p>", true,
                [new MailAttachmentInput(null, "a.pdf", "application/pdf", content)]), TestContext.Current.CancellationToken);
        var queue = await _sp.GetRequiredService<IValidator<QueueOutgoingEmailCommand>>().ValidateAsync(
            new QueueOutgoingEmailCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, "bad", null, "", "", []), TestContext.Current.CancellationToken);
        template.Errors.Should().Contain(x => x.PropertyName == nameof(SaveMailTemplateCommand.SubjectTemplate))
            .And.Contain(x => x.PropertyName == nameof(SaveMailTemplateCommand.Attachments));
        queue.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task QueryAndDeleteValidators_ShouldRejectMissingOrInvalidInput()
    {
        var composer = await _sp.GetRequiredService<IValidator<GetMailComposerQuery>>().ValidateAsync(new GetMailComposerQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);
        var history = await _sp.GetRequiredService<IValidator<GetOutgoingEmailHistoryQuery>>().ValidateAsync(new GetOutgoingEmailHistoryQuery(-1, 0), TestContext.Current.CancellationToken);
        var resend = await _sp.GetRequiredService<IValidator<GetResendComposerQuery>>().ValidateAsync(new GetResendComposerQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);
        var delete = await _sp.GetRequiredService<IValidator<DeleteMailingItemCommand>>().ValidateAsync(new DeleteMailingItemCommand(Guid.Empty, (MailingItemKind)99), TestContext.Current.CancellationToken);
        var preview = await _sp.GetRequiredService<IValidator<RenderMailPreviewQuery>>().ValidateAsync(
            new RenderMailPreviewQuery(Guid.Empty, Guid.NewGuid(), null!), TestContext.Current.CancellationToken);
        composer.IsValid.Should().BeFalse(); history.Errors.Should().HaveCount(2); resend.IsValid.Should().BeFalse(); delete.Errors.Should().HaveCount(2);
        preview.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task SnippetValidator_ShouldRejectIndirectDependencyCycle()
    {
        var snippets = Arrange_BusinessTrackerDatabase(db =>
        {
            var first = db.Arrange_MailSnippet(key: "first", name: "Pierwszy", htmlContent: "{{> second }}");
            var second = db.Arrange_MailSnippet(key: "second", name: "Drugi", htmlContent: "<p>Treść</p>");
            return (first, second);
        });
        var validator = _sp.GetRequiredService<IValidator<SaveMailSnippetCommand>>();

        var result = await validator.ValidateAsync(new SaveMailSnippetCommand(
            snippets.second.Id, "second", "Drugi", null, "{{> first }}", true), TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle(x =>
            x.PropertyName == nameof(SaveMailSnippetCommand.HtmlContent) &&
            x.ErrorMessage.Contains("second → first → second", StringComparison.Ordinal));
    }
}
