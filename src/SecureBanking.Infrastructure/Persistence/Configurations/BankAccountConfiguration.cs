using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureBanking.Domain.Accounts;
using SecureBanking.Domain.Customers;
using SecureBanking.Infrastructure.Persistence.Converters;

namespace SecureBanking.Infrastructure.Persistence.Configurations;

internal sealed class BankAccountConfiguration : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(EntityTypeBuilder<BankAccount> builder)
    {
        builder.ToTable("BankAccounts", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint(
                "CK_BankAccounts_AccountNumber_Length",
                "LEN([AccountNumber]) = 12 AND " +
                "[AccountNumber] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'");
            tableBuilder.HasCheckConstraint(
                "CK_BankAccounts_BalanceAmount_Range",
                "[BalanceAmount] >= 0 AND [BalanceAmount] <= 999999999999999.9999");
            tableBuilder.HasCheckConstraint(
                "CK_BankAccounts_BalanceCurrency_Supported",
                "[BalanceCurrency] IN ('CRC', 'USD')");
        });

        builder.HasKey(account => account.Id)
            .HasName("PK_BankAccounts");

        builder.Property(account => account.Id)
            .ValueGeneratedNever();

        builder.Property(account => account.CustomerId)
            .IsRequired();

        builder.Property(account => account.AccountNumber)
            .HasConversion<AccountNumberConverter>()
            .HasColumnType("char(12)")
            .HasMaxLength(12)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(account => account.AccountNumber)
            .IsUnique()
            .HasDatabaseName("UX_BankAccounts_AccountNumber");

        builder.HasIndex(account => account.CustomerId)
            .HasDatabaseName("IX_BankAccounts_CustomerId");

        builder.Property(account => account.Type)
            .HasConversion<string>()
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(account => account.Status)
            .HasConversion<string>()
            .HasMaxLength(6)
            .IsRequired();

        builder.Property(account => account.Currency)
            .HasConversion<CurrencyConverter>()
            .HasColumnName("BalanceCurrency")
            .HasColumnType("char(3)")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.OwnsOne(account => account.Balance, balanceBuilder =>
        {
            balanceBuilder.Property(money => money.Amount)
                .HasColumnName("BalanceAmount")
                .HasColumnType("decimal(19,4)")
                .IsRequired();

            balanceBuilder.Property(money => money.Currency)
                .HasConversion<CurrencyConverter>()
                .HasColumnName("BalanceCurrency")
                .HasColumnType("char(3)")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Navigation(account => account.Balance)
            .IsRequired();

        builder.Property(account => account.CreatedAtUtc)
            .HasConversion<UtcDateTimeConverter>()
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property<byte[]>("RowVersion")
            .HasColumnName("RowVersion")
            .IsRequired()
            .IsRowVersion();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(account => account.CustomerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_BankAccounts_Customers_CustomerId");
    }
}
