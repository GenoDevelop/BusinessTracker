using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.ApplicationLogic.Abstractions;
using GenoDev.BusinessTracker.ApplicationLogic.Services;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetMailComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetOutgoingEmailHistory;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.GetResendComposer;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.QueueOutgoingEmail;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing.RenderMailPreview;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Mailing;

public sealed class GetMailComposerHandler_Tests : BusinessTrackerUnitTestsBase<GetMailComposerQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services); services.AddTransient<IMailTemplateRenderer, MailTemplateRenderer>();
    }

    [Fact]
    public async Task Handle_ShouldRenderOrderClientProductsSnippetAndAttachments()
    {
        var data = Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order(orderIdentifier: "ORD-1", trackingNumber: "TRACK");
            db.Arrange_ClientDetails(order, clientName: "Jan & Syn");
            db.Arrange_OrderProduct(order, db.Arrange_Product(name: "Produkt"), orderedAmount: 2);
            var account = db.Arrange_SmtpAccount(); db.Arrange_MailSnippet();
            var template = db.Arrange_MailTemplate(account, htmlTemplate: "<p>{{ client.name }}</p>{{#each order.products}}{{ product.name }}{{/each}}{{> footer }}");
            db.Arrange_MailTemplateAttachment(template);
            return (order.Id, template.Id);
        });
        var result = await Sut.Handle(new GetMailComposerQuery(data.Item1, data.Item2), TestContext.Current.CancellationToken);
        result.Subject.Should().Contain("ORD-1"); result.HtmlBody.Should().Contain("Jan &amp; Syn").And.Contain("Produkt").And.Contain("Test Sender");
        result.Attachments.Should().ContainSingle();
    }
}

public sealed class RenderMailPreviewHandler_Tests : BusinessTrackerUnitTestsBase<RenderMailPreviewQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddTransient<IMailTemplateRenderer, MailTemplateRenderer>();
    }

    [Fact]
    public async Task Handle_ShouldRenderCurrentHtmlUsingSelectedOrderAccountAndSnippets()
    {
        var data = Arrange_BusinessTrackerDatabase(db =>
        {
            var order = db.Arrange_Order(orderIdentifier: "ORD-PREVIEW", trackingNumber: "TRACK");
            db.Arrange_ClientDetails(order, clientName: "Jan & Syn");
            db.Arrange_OrderProduct(order, db.Arrange_Product(name: "Produkt"), orderedAmount: 2);
            var account = db.Arrange_SmtpAccount();
            account.FromName = "Nadawca testowy";
            db.Arrange_MailSnippet(htmlContent: "<footer>{{ sender.name }}</footer>");
            return (order.Id, account.Id);
        });

        var result = await Sut.Handle(new RenderMailPreviewQuery(data.Item1, data.Item2,
            "<p>{{ client.name }}</p>{{#each order.products}}<b>{{ product.name }}</b>{{/each}}{{> footer }}"),
            TestContext.Current.CancellationToken);

        result.Should().Contain("Jan &amp; Syn").And.Contain("Produkt").And.Contain("Nadawca testowy");
    }
}

public sealed class QueueOutgoingEmailHandler_Tests : BusinessTrackerUnitTestsBase<QueueOutgoingEmailCommandHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldSnapshotMessageAndAttachmentAsPending()
    {
        var data = Arrange_BusinessTrackerDatabase(db => (db.Arrange_Order(), db.Arrange_SmtpAccount()));
        var id = await Sut.Handle(new QueueOutgoingEmailCommand(data.Item1.Id, data.Item2.Id, null, null, "client@example.com", "Klient", "Temat", "<p>Treść</p>",
            [new MailAttachmentInput(null, "a.pdf", "application/pdf", [1, 2, 3])]), TestContext.Current.CancellationToken);
        Assert_BusinessTrackerDatabase(db =>
        {
            var email = db.OutgoingEmails.Single(x => x.Id == id); email.Status.Should().Be(MailDeliveryStatus.Pending);
            db.OutgoingEmailAttachments.Single(x => x.OutgoingEmailId == id).Sha256.Should().HaveLength(64);
        });
    }
}

public sealed class GetOutgoingEmailHistoryHandler_Tests : BusinessTrackerUnitTestsBase<GetOutgoingEmailHistoryQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldPageNewestFirstAndExposeExpiredAttachment()
    {
        Arrange_BusinessTrackerDatabase(db =>
        {
            var account = db.Arrange_SmtpAccount();
            var first = db.Arrange_OutgoingEmail(account: account, createdAtUtc: DateTime.UtcNow.AddMinutes(-1));
            var expired = db.Arrange_OutgoingEmailAttachment(first);
            expired.Content = null;
            expired.ContentDeletedAtUtc = DateTime.UtcNow;
            db.Arrange_OutgoingEmail(account: account, createdAtUtc: DateTime.UtcNow);
        });
        var result = await Sut.Handle(new GetOutgoingEmailHistoryQuery(0, 1), TestContext.Current.CancellationToken);
        result.TotalCount.Should().Be(2); result.Items.Should().ContainSingle(); result.HasNextPage.Should().BeTrue();
    }
}

public sealed class GetResendComposerHandler_Tests : BusinessTrackerUnitTestsBase<GetResendComposerQueryHandler>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute) => RegisterBusinessTrackingPostgresDatabase(services);

    [Fact]
    public async Task Handle_ShouldUseCurrentTemplateAttachmentAndReportChangedAndExpiredFiles()
    {
        var emailId = Arrange_BusinessTrackerDatabase(db =>
        {
            var account = db.Arrange_SmtpAccount(); var template = db.Arrange_MailTemplate(account);
            var current = db.Arrange_MailTemplateAttachment(template, content: [9]);
            var email = db.Arrange_OutgoingEmail(account: account, template: template);
            var old = db.Arrange_OutgoingEmailAttachment(email, templateAttachmentId: current.Id, fileName: current.FileName, content: [1]);
            old.Sha256 = new string('A', 64);
            db.Arrange_OutgoingEmailAttachment(email, templateAttachmentId: Guid.NewGuid(), fileName: "usunięty.pdf", content: [7]);
            var expired = db.Arrange_OutgoingEmailAttachment(email, fileName: "manual.pdf");
            expired.Content = null;
            expired.ContentDeletedAtUtc = DateTime.UtcNow;
            return email.Id;
        });
        var result = await Sut.Handle(new GetResendComposerQuery(emailId), TestContext.Current.CancellationToken);
        result.AvailableAttachments.Should().ContainSingle(x => x.Content!.SequenceEqual(new byte[] { 9 }));
        result.Differences.Should().Contain(x => x.Kind == "Changed")
            .And.Contain(x => x.Kind == "Expired")
            .And.Contain(x => x.Kind == "Missing" && x.OriginalFileName == "usunięty.pdf" && x.OriginalSize == 1);
    }
}
