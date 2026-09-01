using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public sealed class MailTemplateAttachmentConfiguration : IEntityTypeConfiguration<MailTemplateAttachment>
{
    public void Configure(EntityTypeBuilder<MailTemplateAttachment> builder)
    {
        builder.ToTable("mail_template_attachments", BusinessTrackerDbContext.SalesSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Sha256).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.Property(x => x.Content).IsRequired();
        builder.HasIndex(x => new { x.MailTemplateId, x.SortOrder });
        builder.HasOne(x => x.MailTemplate)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.MailTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
