using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBanking.Domain.Customers;
using SecureBanking.Infrastructure.Persistence.Converters;

namespace SecureBanking.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id)
            .HasName("PK_Customers");

        builder.Property(customer => customer.Id)
            .ValueGeneratedNever();

        builder.Property(customer => customer.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(customer => customer.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasConversion<EmailAddressConverter>()
            .HasMaxLength(254)
            .IsRequired();

        builder.HasIndex(customer => customer.Email)
            .IsUnique()
            .HasDatabaseName("UX_Customers_Email");

        builder.Property(customer => customer.Status)
            .HasConversion<string>()
            .HasMaxLength(9)
            .IsRequired();

        builder.Property(customer => customer.CreatedAtUtc)
            .HasConversion<UtcDateTimeConverter>()
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .HasColumnName("RowVersion")
            .IsRequired()
            .IsRowVersion();
    }
}
