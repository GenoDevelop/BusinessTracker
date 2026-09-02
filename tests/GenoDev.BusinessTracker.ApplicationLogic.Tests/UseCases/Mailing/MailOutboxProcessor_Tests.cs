using AutoFixture;
using FluentAssertions;
using GenoDev.BusinessTracker.Domain.Entities;
using GenoDev.BusinessTracker.ApplicationLogic.UseCases.Mailing;
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
    public async Task Create_ShouldSerializeCidImagesAsRelatedPartsAlongsideOrdinaryAttachments()
    {
        // Arrange
        var email = new OutgoingEmail
        {
            RecipientAddress = "client@example.com", Subject = "Images",
            HtmlBody = MailImageTestData.Html + MailImageTestData.Html + MailInlineImages.CreateImageHtml(MailImageTestData.Gif, "Icon", 100),
            SmtpAccount = new SmtpAccount { FromAddress = "sender@example.com", FromName = "Sender" },
            Attachments = { new OutgoingEmailAttachment { FileName = "document.pdf", ContentType = "application/pdf", Content = [1, 2, 3], Size = 3 } }
        };
        var originalHtml = email.HtmlBody;
        var directory = Directory.CreateTempSubdirectory("genodev-mail-cid-");

        try
        {
            // Act
            using var message = MailMessageFactory.Create(email);
            using var smtp = new System.Net.Mail.SmtpClient
            {
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.SpecifiedPickupDirectory,
                PickupDirectoryLocation = directory.FullName
            };
            await smtp.SendMailAsync(message, TestContext.Current.CancellationToken);
            var mime = await File.ReadAllTextAsync(Directory.GetFiles(directory.FullName).Single(), TestContext.Current.CancellationToken);

            // Assert
            var view = message.AlternateViews.Should().ContainSingle().Subject;
            view.ContentStream.Position = 0;
            using var reader = new StreamReader(view.ContentStream, leaveOpen: true);
            var html = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
            html.Should().NotContain("data:");
            view.LinkedResources.Should().HaveCount(2);
            foreach (var image in view.LinkedResources)
            {
                html.Should().Contain("cid:" + image.ContentId);
                mime.Should().Contain("Content-ID: <" + image.ContentId + ">");
                image.TransferEncoding.Should().Be(System.Net.Mime.TransferEncoding.Base64);
                image.ContentStream.Position = 0;
                using var copy = new MemoryStream();
                await image.ContentStream.CopyToAsync(copy, TestContext.Current.CancellationToken);
                copy.ToArray().Should().Equal(image.ContentType.MediaType == "image/png" ? MailImageTestData.Png : MailImageTestData.Gif);
            }
            mime.Should().Contain("multipart/related").And.Contain("image/png").And.Contain("image/gif");
            message.Attachments.Should().ContainSingle().Which.ContentDisposition!.Inline.Should().BeFalse();
            email.HtmlBody.Should().Be(originalHtml);
        }
        finally
        {
            foreach (var file in Directory.GetFiles(directory.FullName)) File.Delete(file);
            directory.Delete();
        }
    }

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
