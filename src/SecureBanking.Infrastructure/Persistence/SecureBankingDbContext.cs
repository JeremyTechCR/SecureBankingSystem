using Microsoft.EntityFrameworkCore;
using SecureBanking.Domain.Accounts;
using SecureBanking.Domain.Customers;

namespace SecureBanking.Infrastructure.Persistence;

public sealed class SecureBankingDbContext : DbContext
{
    public SecureBankingDbContext(DbContextOptions<SecureBankingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SecureBankingDbContext).Assembly);
    }
}
