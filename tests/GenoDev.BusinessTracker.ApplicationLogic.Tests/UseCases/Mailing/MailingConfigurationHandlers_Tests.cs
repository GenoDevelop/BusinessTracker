using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.DeleteMailingItem;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailingWorkspace;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailSnippet;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveMailTemplate;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.SaveSmtpAccount;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Mailing;

public sealed class SaveSmtpAccountHandler_Tests : BusinessTrackerUnitTestsBase<SaveSmtpAccountCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldCreateAccountAndUnsetPreviousDefault()
    {
        Arrange_BusinessTrackerDatabase(db => db.Arrange_SmtpAccount());
        var id = await Sut.Handle(new SaveSmtpAccountCommand(null, "Gmail", "smtp.gmail.com", 587, true,
            "mail@gmail.com", "app-password", "mail@gmail.com", "Firma", null, true, true), TestContext.Current.CancellationToken);

        Assert_BusinessTrackerDatabase(db =>
        {
            db.SmtpAccounts.Single(x => x.Id == id).Password.Should().Be("app-password");
            db.SmtpAccounts.Count(x => x.IsDefault).Should().Be(1);
            db.SmtpAccounts.Single(x => x.IsDefault).Id.Should().Be(id);
        });
    }
}

public sealed class SaveMailSnippetHandler_Tests : BusinessTrackerUnitTestsBase<SaveMailSnippetCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldCreateSnippet()
    {
        var id = await Sut.Handle(new SaveMailSnippetCommand(null, "footer", "Stopka", null, "<p>Firma</p>", true), TestContext.Current.CancellationToken);
        Assert_BusinessTrackerDatabase(db => db.MailSnippets.Single(x => x.Id == id).Key.Should().Be("footer"));
    }
}

public sealed class SaveMailTemplateHandler_Tests : BusinessTrackerUnitTestsBase<SaveMailTemplateCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldCreateUpdateAndAddAttachmentToExistingTemplate()
    {
        var account = Arrange_BusinessTrackerDatabase(db => db.Arrange_SmtpAccount());
        var id = await Sut.Handle(new SaveMailTemplateCommand(null, account.Id, "Szablon", "Temat", "<p>HTML</p>", true,
            [new MailAttachmentInput(null, "a.pdf", "application/pdf", [1, 2])]), TestContext.Current.CancellationToken);
        var attachmentId = Assert_BusinessTrackerDatabase(db => db.MailTemplateAttachments.Single(x => x.MailTemplateId == id).Id);

        await Sut.Handle(new SaveMailTemplateCommand(id, account.Id, "Szablon", "Temat 2", "<p>Nowy</p>", true,
            [
                new MailAttachmentInput(attachmentId, "a.pdf", "application/pdf", [9]),
                new MailAttachmentInput(null, "b.pdf", "application/pdf", [4, 5, 6])
            ]), TestContext.Current.CancellationToken);

        Assert_BusinessTrackerDatabase(db =>
        {
            db.ChangeTracker.Clear();
            var attachment = db.MailTemplateAttachments.Single(x => x.Id == attachmentId);
            attachment.Content.Should().Equal(9);
            var added = db.MailTemplateAttachments.Single(x => x.MailTemplateId == id && x.Id != attachmentId);
            added.FileName.Should().Be("b.pdf");
            added.Content.Should().Equal(4, 5, 6);
            db.MailTemplateAttachments.Count(x => x.MailTemplateId == id).Should().Be(2);
        });
    }
}

public sealed class GetMailingWorkspaceHandler_Tests : BusinessTrackerUnitTestsBase<GetMailingWorkspaceQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldReturnConfigurationAndAttachmentContent()
    {
        Arrange_BusinessTrackerDatabase(db =>
        {
            var account = db.Arrange_SmtpAccount(); db.Arrange_MailSnippet();
            var template = db.Arrange_MailTemplate(account); db.Arrange_MailTemplateAttachment(template);
        });
        var result = await Sut.Handle(new GetMailingWorkspaceQuery(), TestContext.Current.CancellationToken);
        result.Accounts.Should().ContainSingle(); result.Snippets.Should().ContainSingle();
        result.Templates.Should().ContainSingle().Which.Attachments.Should().ContainSingle().Which.Content.Should().NotBeNull();
    }
}

public sealed class DeleteMailingItemHandler_Tests : BusinessTrackerUnitTestsBase<DeleteMailingItemCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldDeleteSnippet()
    {
        var snippet = Arrange_BusinessTrackerDatabase(db => db.Arrange_MailSnippet());
        await Sut.Handle(new DeleteMailingItemCommand(snippet.Id, MailingItemKind.Snippet), TestContext.Current.CancellationToken);
        Assert_BusinessTrackerDatabase(db => db.MailSnippets.AsNoTracking().Should().BeEmpty());
    }
}
