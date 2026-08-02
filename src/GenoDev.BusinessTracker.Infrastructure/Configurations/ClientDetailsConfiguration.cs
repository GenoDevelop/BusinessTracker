using GenoDev.BusinessTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GenoDev.BusinessTracker.Infrastructure.Configurations;

public class ClientDetailsConfiguration : IEntityTypeConfiguration<ClientDetails>
{
    public void Configure(EntityTypeBuilder<ClientDetails> builder)
    {
        builder.ToTable("client_details");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.OrderId).IsRequired();
        builder.HasIndex(x => x.OrderId).IsUnique();

        builder.Property(x => x.ClientName).IsRequired(false);
        builder.Property(x => x.Street).IsRequired(false);
        builder.Property(x => x.PostCode).IsRequired(false);
        builder.Property(x => x.City).IsRequired(false);
        builder.Property(x => x.Email).IsRequired(false);
        builder.Property(x => x.Phone).IsRequired(false);
        builder.Property(x => x.Description).IsRequired(false);
    }
}
