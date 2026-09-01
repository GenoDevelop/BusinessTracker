using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.Domain.Enums;
using GenoDev.BusinessTracker.Infrastructure;
using GenoDev.BusinessTracker.Infrastructure.Services;
using GenoDev.BusinessTracker.TestsUtilities;
using GenoDev.BusinessTracker.TestsUtilities.Database;
using GenoDev.BusinessTracker.TestsUtilities.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GenoDev.BusinessTracker.ApplicationLogic.Tests.UseCases.Mailing;

public sealed class MailOutboxProcessor_Tests : BusinessTrackerUnitTestsBase<MailOutboxProcessor>
{
    protected override void RegisterMockedDependencies(IServiceCollection services, IFixture autoSubstitute)
    {
        RegisterBusinessTrackingPostgresDatabase(services);
        services.AddSingleton<IDbContextFactory<BusinessTrackerDbContext>, MailingTestContextFactory>();
        services.AddLogging();
    }

    [Fact]
    public async Task PurgeExpiredAttachments_ShouldRemoveOnlySentContentOlderThanSevenDays()
    {
        var ids = Arrange_BusinessTrackerDatabase(db =>
        {
            var account = db.Arrange_SmtpAccount();
            var expiredMail = db.Arrange_OutgoingEmail(account: account, status: MailDeliveryStatus.Sent, sentAtUtc: DateTime.UtcNow.AddDays(-8));
            var recentMail = db.Arrange_OutgoingEmail(account: account, status: MailDeliveryStatus.Sent, sentAtUtc: DateTime.UtcNow.AddDays(-6));
            var failedMail = db.Arrange_OutgoingEmail(account: account, status: MailDeliveryStatus.Failed, sentAtUtc: DateTime.UtcNow.AddDays(-20));
            return (db.Arrange_OutgoingEmailAttachment(expiredMail).Id,
                db.Arrange_OutgoingEmailAttachment(recentMail).Id,
                db.Arrange_OutgoingEmailAttachment(failedMail).Id);
        });

        var count = await Sut.PurgeExpiredAttachmentsAsync(TestContext.Current.CancellationToken);

        count.Should().Be(1);
        Assert_BusinessTrackerDatabase(db =>
        {
            db.OutgoingEmailAttachments.Single(x => x.Id == ids.Item1).Content.Should().BeNull();
            db.OutgoingEmailAttachments.Single(x => x.Id == ids.Item2).Content.Should().NotBeNull();
            db.OutgoingEmailAttachments.Single(x => x.Id == ids.Item3).Content.Should().NotBeNull();
        });
    }

    private sealed class MailingTestContextFactory : IDbContextFactory<BusinessTrackerDbContext>
    {
        public BusinessTrackerDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<BusinessTrackerDbContext>()
                .UseNpgsql(BusinessTrackerPostgreSqlContainer.DataSource)
                .Options;
            return new BusinessTrackerDbContext(options);
        }

        public Task<BusinessTrackerDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}

public sealed class MailMessageFactory_Tests
{
    [Fact]
    public void Create_ShouldBuildBase64MimeAttachmentWithAttachmentDisposition()
    {
        var content = new byte[] { 1, 2, 3, 4, 5 };
        var email = new OutgoingEmail
        {
            Id = Guid.NewGuid(),
            RecipientAddress = "client@example.com",
            RecipientName = "Klient",
            Subject = "Temat",
            HtmlBody = "<p>Treść</p>",
            SmtpAccount = new SmtpAccount
            {
                FromAddress = "sender@example.com",
                FromName = "Nadawca"
            },
            Attachments =
            {
                new OutgoingEmailAttachment
                {
                    Id = Guid.NewGuid(),
                    FileName = "załącznik testowy.pdf",
                    ContentType = "application/pdf",
                    Content = content,
                    Size = content.Length,
                    Sha256 = new string('A', 64)
                }
            }
        };

        using var message = MailMessageFactory.Create(email);

        message.Attachments.Should().ContainSingle();
        var attachment = message.Attachments[0];
        attachment.Name.Should().Be("załącznik testowy.pdf");
        attachment.TransferEncoding.Should().Be(System.Net.Mime.TransferEncoding.Base64);
        var disposition = attachment.ContentDisposition;
        disposition.Should().NotBeNull();
        disposition!.DispositionType.Should().Be(System.Net.Mime.DispositionTypeNames.Attachment);
        disposition.Inline.Should().BeFalse();
        using var copy = new MemoryStream();
        attachment.ContentStream.CopyTo(copy);
        copy.ToArray().Should().Equal(content);
    }
}
