using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public sealed class OutgoingEmailAttachmentConfiguration : IEntityTypeConfiguration<OutgoingEmailAttachment>
{
    public void Configure(EntityTypeBuilder<OutgoingEmailAttachment> builder)
    {
        builder.ToTable("outgoing_email_attachments", BusinessTrackerDbContext.SalesSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Sha256).IsRequired().HasMaxLength(64).IsFixedLength();
        builder.HasIndex(x => new { x.OutgoingEmailId, x.SortOrder });
        builder.HasOne(x => x.OutgoingEmail)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.OutgoingEmailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
