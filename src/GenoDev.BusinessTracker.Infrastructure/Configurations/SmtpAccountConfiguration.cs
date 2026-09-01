using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public sealed class SmtpAccountConfiguration : IEntityTypeConfiguration<SmtpAccount>
{
    public void Configure(EntityTypeBuilder<SmtpAccount> builder)
    {
        builder.ToTable("smtp_accounts", BusinessTrackerDbContext.SalesSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Host).IsRequired().HasMaxLength(255);
        builder.Property(x => x.UserName).IsRequired().HasMaxLength(320);
        builder.Property(x => x.Password).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.FromAddress).IsRequired().HasMaxLength(320);
        builder.Property(x => x.FromName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ReplyToAddress).HasMaxLength(320);
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsDefault)
            .IsUnique()
            .HasFilter("is_default");
    }
}
