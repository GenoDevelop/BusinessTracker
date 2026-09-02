using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailingWorkspace;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetResendComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.QueueOutgoingEmail;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.RenderMailPreview;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailSnippet;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailTemplate;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Mailing;

public sealed class MailInlineImagesLifecycle_Tests : BusinessTrackerUnitTestsBase<SaveMailTemplateCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<IMailTemplateRenderer, MailTemplateRenderer>();
        services.AddTransient<SaveMailSnippetCommandHandler>();
        services.AddTransient<GetMailComposerQueryHandler>();
        services.AddTransient<GetMailingWorkspaceQueryHandler>();
        services.AddTransient<RenderMailPreviewQueryHandler>();
        services.AddTransient<QueueOutgoingEmailCommandHandler>();
        services.AddTransient<GetResendComposerQueryHandler>();
    }

    [Fact]
    public async Task Handle_ShouldPreserveSavedImagesThroughPreviewCompositionQueueAndResendAfterSourceChanges()
    {
        // Arrange
        var data = Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order();
            db.Arrange_ClientDetails(order);
            return (order.Id, db.Arrange_SmtpAccount().Id);
        });
        var cancellationToken = TestContext.Current.CancellationToken;
        var snippetHandler = _sp.GetRequiredService<SaveMailSnippetCommandHandler>();
        var snippetId = await snippetHandler.Handle(new SaveMailSnippetCommand(null, "logo", "Logo", null, MailImageTestData.Html, true), cancellationToken);
        var templateHtml = "{{> logo }}" + MailInlineImages.CreateImageHtml(MailImageTestData.Gif, "Icon", 120);
        var templateId = await Sut.Handle(new SaveMailTemplateCommand(null, data.Item2, "Template", "Subject", templateHtml, true, []), cancellationToken);

        // Act
        var workspace = await _sp.GetRequiredService<GetMailingWorkspaceQueryHandler>().Handle(new GetMailingWorkspaceQuery(), cancellationToken);
        var preview = await _sp.GetRequiredService<RenderMailPreviewQueryHandler>().Handle(new RenderMailPreviewQuery(data.Item1, data.Item2, templateHtml), cancellationToken);
        var composer = await _sp.GetRequiredService<GetMailComposerQueryHandler>().Handle(new GetMailComposerQuery(data.Item1, templateId), cancellationToken);
        var emailId = await _sp.GetRequiredService<QueueOutgoingEmailCommandHandler>().Handle(new QueueOutgoingEmailCommand(
            data.Item1, data.Item2, templateId, null, composer.RecipientAddress, composer.RecipientName, composer.Subject, composer.HtmlBody, []), cancellationToken);

        await snippetHandler.Handle(new SaveMailSnippetCommand(snippetId, "logo", "Logo", null, "<p>Changed</p>", true), cancellationToken);
        await Sut.Handle(new SaveMailTemplateCommand(templateId, data.Item2, "Template", "Subject", "<p>Changed</p>", true, []), cancellationToken);
        var resend = await _sp.GetRequiredService<GetResendComposerQueryHandler>().Handle(new GetResendComposerQuery(emailId), cancellationToken);

        // Assert
        workspace.Snippets.Should().ContainSingle().Which.HtmlContent.Should().Be(MailImageTestData.Html);
        workspace.Templates.Should().ContainSingle().Which.HtmlTemplate.Should().Be(templateHtml);
        preview.Should().Be(composer.HtmlBody);
        resend.HtmlBody.Should().Be(composer.HtmlBody);
        resend.Differences.Should().BeEmpty();
        var images = MailInlineImages.PrepareForDelivery(resend.HtmlBody).Images;
        images.Should().HaveCount(2);
        images.Should().Contain(x => x.Content.SequenceEqual(MailImageTestData.Png));
        images.Should().Contain(x => x.Content.SequenceEqual(MailImageTestData.Gif));
        Assert_BusinessTrackerDatabase(db => db.OutgoingEmails.Single(x => x.Id == emailId).HtmlBody.Should().Be(composer.HtmlBody));
    }
}
