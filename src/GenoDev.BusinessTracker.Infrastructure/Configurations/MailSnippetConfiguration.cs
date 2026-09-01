using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public sealed class MailSnippetConfiguration : IEntityTypeConfiguration<MailSnippet>
{
    public void Configure(EntityTypeBuilder<MailSnippet> builder)
    {
        builder.ToTable("mail_snippets", BusinessTrackerDbContext.SalesSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Key).IsRequired().HasMaxLength(80);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.HtmlContent).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.HasIndex(x => x.Name).IsUnique();
    }
}
