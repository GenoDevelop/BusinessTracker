using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public sealed class OutgoingEmailConfiguration : IEntityTypeConfiguration<OutgoingEmail>
{
    public void Configure(EntityTypeBuilder<OutgoingEmail> builder)
    {
        builder.ToTable("outgoing_emails", BusinessTrackerDbContext.SalesSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.RecipientAddress).IsRequired().HasMaxLength(320);
        builder.Property(x => x.RecipientName).HasMaxLength(200);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(998);
        builder.Property(x => x.HtmlBody).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ProcessingBy).HasMaxLength(255);
        builder.Property(x => x.ErrorMessage).HasMaxLength(4000);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.OrderId, x.CreatedAtUtc });
        builder.HasOne(x => x.Order)
            .WithMany(x => x.OutgoingEmails)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SmtpAccount)
            .WithMany(x => x.OutgoingEmails)
            .HasForeignKey(x => x.SmtpAccountId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.MailTemplate)
            .WithMany(x => x.OutgoingEmails)
            .HasForeignKey(x => x.MailTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.ResentFromEmail)
            .WithMany(x => x.Resends)
            .HasForeignKey(x => x.ResentFromEmailId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
