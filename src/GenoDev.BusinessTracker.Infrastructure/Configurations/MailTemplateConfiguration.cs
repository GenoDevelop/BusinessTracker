using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public sealed class MailTemplateConfiguration : IEntityTypeConfiguration<MailTemplate>
{
    public void Configure(EntityTypeBuilder<MailTemplate> builder)
    {
        builder.ToTable("mail_templates", BusinessTrackerDbContext.SalesSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.SubjectTemplate).IsRequired().HasMaxLength(998);
        builder.Property(x => x.HtmlTemplate).IsRequired();
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasOne(x => x.SmtpAccount)
            .WithMany(x => x.Templates)
            .HasForeignKey(x => x.SmtpAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
