using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecureBanking.Domain.Accounts;
using SecureBanking.Domain.Customers;
using SecureBanking.Domain.ValueObjects;
using SecureBanking.Infrastructure.Persistence;

namespace SecureBanking.IntegrationTests.Persistence;

public sealed class SqlServerPersistenceTests
{
    private static readonly DateTime CreatedAtUtc =
        new(2026, 9, 23, 18, 30, 15, 123, DateTimeKind.Utc);

    [Fact]
    public Task InitialMigration_CreatesExpectedDatabaseSchema() =>
        WithTemporaryDatabaseAsync(async (context, cancellationToken) =>
        {
            string[] pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            string[] applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToArray();

            Assert.Empty(pending);
            Assert.Contains(applied, migration => migration.EndsWith("_InitialPersistence", StringComparison.Ordinal));
            Assert.True(await TableExistsAsync(context, "Customers", cancellationToken));
            Assert.True(await TableExistsAsync(context, "BankAccounts", cancellationToken));
        });

    [Fact]
    public Task SaveAndReload_MaterializesCompleteDomainModel() =>
        WithTemporaryDatabaseAsync(async (context, cancellationToken) =>
        {
            Guid customerId = Guid.NewGuid();
            Guid accountId = Guid.NewGuid();
            Customer customer = Customer.Create(
                customerId,
                "Ada",
                "Lovelace",
                EmailAddress.Create("ADA@Example.COM"),
                CreatedAtUtc);
            BankAccount account = BankAccount.Create(
                accountId,
                customerId,
                AccountNumber.Create("001234567890"),
                AccountType.Savings,
                Currency.Crc,
                CreatedAtUtc);
            account.Credit(new Money(1234.5678m, Currency.Crc));

            context.Add(customer);
            context.Add(account);
            await context.SaveChangesAsync(cancellationToken);

            byte[] customerRowVersion = GetRowVersion(context, customer);
            byte[] accountRowVersion = GetRowVersion(context, account);

            context.ChangeTracker.Clear();

            Customer loadedCustomer = await context.Customers.SingleAsync(
                item => item.Id == customerId,
                cancellationToken);
            BankAccount loadedAccount = await context.BankAccounts.SingleAsync(
                item => item.Id == accountId,
                cancellationToken);

            Assert.Equal(customerId, loadedCustomer.Id);
            Assert.Equal("Ada", loadedCustomer.FirstName);
            Assert.Equal("Lovelace", loadedCustomer.LastName);
            Assert.Equal("ada@example.com", loadedCustomer.Email.Value);
            Assert.Equal(CustomerStatus.Active, loadedCustomer.Status);
            Assert.Equal(CreatedAtUtc.Ticks, loadedCustomer.CreatedAtUtc.Ticks);
            Assert.Equal(DateTimeKind.Utc, loadedCustomer.CreatedAtUtc.Kind);

            Assert.Equal(accountId, loadedAccount.Id);
            Assert.Equal(customerId, loadedAccount.CustomerId);
            Assert.Equal("001234567890", loadedAccount.AccountNumber.GetFullValue());
            Assert.Equal("********7890", loadedAccount.AccountNumber.ToString());
            Assert.Equal(AccountType.Savings, loadedAccount.Type);
            Assert.Equal(AccountStatus.Active, loadedAccount.Status);
            Assert.Equal(1234.5678m, loadedAccount.Balance.Amount);
            Assert.Equal(Currency.Crc, loadedAccount.Currency);
            Assert.Equal(Currency.Crc, loadedAccount.Balance.Currency);
            Assert.Equal(loadedAccount.Currency, loadedAccount.Balance.Currency);
            Assert.Equal(CreatedAtUtc.Ticks, loadedAccount.CreatedAtUtc.Ticks);
            Assert.Equal(DateTimeKind.Utc, loadedAccount.CreatedAtUtc.Kind);
            Assert.NotNull(loadedAccount.Balance);
            Assert.NotEmpty(customerRowVersion);
            Assert.NotEmpty(accountRowVersion);
            Assert.NotEmpty(GetRowVersion(context, loadedCustomer));
            Assert.NotEmpty(GetRowVersion(context, loadedAccount));
        });

    [Fact]
    public Task ConcurrentUpdates_UseRowVersionAndPreserveWinner() =>
        WithTemporaryDatabaseAsync(async (setupContext, cancellationToken) =>
        {
            Guid customerId = Guid.NewGuid();
            Guid accountId = Guid.NewGuid();
            Customer customer = Customer.Create(
                customerId, "Grace", "Hopper", EmailAddress.Create("grace@example.com"), CreatedAtUtc);
            BankAccount account = BankAccount.Create(
                accountId,
                customerId,
                AccountNumber.Create("009876543210"),
                AccountType.Checking,
                Currency.Usd,
                CreatedAtUtc);

            setupContext.Add(customer);
            setupContext.Add(account);
            await setupContext.SaveChangesAsync(cancellationToken);

            DbContextOptions<SecureBankingDbContext> options = setupContext.Options();
            await using SecureBankingDbContext firstContext = new(options);
            await using SecureBankingDbContext secondContext = new(options);

            BankAccount first = await firstContext.BankAccounts.SingleAsync(
                item => item.Id == accountId,
                cancellationToken);
            BankAccount second = await secondContext.BankAccounts.SingleAsync(
                item => item.Id == accountId,
                cancellationToken);
            byte[] originalRowVersion = GetRowVersion(firstContext, first).ToArray();

            first.Credit(new Money(10.0000m, Currency.Usd));
            second.Credit(new Money(20.0000m, Currency.Usd));

            await firstContext.SaveChangesAsync(cancellationToken);
            byte[] updatedRowVersion = GetRowVersion(firstContext, first).ToArray();

            Assert.NotEqual(originalRowVersion, updatedRowVersion);
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => secondContext.SaveChangesAsync(cancellationToken));

            await using SecureBankingDbContext verificationContext = new(options);
            BankAccount winner = await verificationContext.BankAccounts
                .AsNoTracking()
                .SingleAsync(item => item.Id == accountId, cancellationToken);
            Assert.Equal(10.0000m, winner.Balance.Amount);
        });

    [Fact]
    public Task Database_RejectsNonNumericAccountNumber() =>
        AssertPhysicalConstraintAsync(
            "ABCDEFGHIJKL",
            0m,
            "USD",
            "CK_BankAccounts_AccountNumber_Length");

    [Fact]
    public Task Database_RejectsNegativeBalance() =>
        AssertPhysicalConstraintAsync(
            "001234567890",
            -0.0001m,
            "USD",
            "CK_BankAccounts_BalanceAmount_Range");

    [Fact]
    public Task Database_RejectsUnsupportedCurrency() =>
        AssertPhysicalConstraintAsync(
            "001234567890",
            0m,
            "EUR",
            "CK_BankAccounts_BalanceCurrency_Supported");

    private static Task AssertPhysicalConstraintAsync(
        string accountNumber,
        decimal balanceAmount,
        string balanceCurrency,
        string expectedConstraintName) =>
        WithTemporaryDatabaseAsync(async (context, cancellationToken) =>
        {
            Guid customerId = Guid.NewGuid();
            Customer customer = Customer.Create(
                customerId, "Katherine", "Johnson", EmailAddress.Create("katherine@example.com"), CreatedAtUtc);
            context.Add(customer);
            await context.SaveChangesAsync(cancellationToken);

            object[] parameters =
            [
                new SqlParameter("@id", SqlDbType.UniqueIdentifier) { Value = Guid.NewGuid() },
                new SqlParameter("@customerId", SqlDbType.UniqueIdentifier) { Value = customerId },
                new SqlParameter("@accountNumber", SqlDbType.Char, 12) { Value = accountNumber },
                new SqlParameter("@type", SqlDbType.NVarChar, 20) { Value = "Checking" },
                new SqlParameter("@status", SqlDbType.NVarChar, 20) { Value = "Active" },
                new SqlParameter("@balanceAmount", SqlDbType.Decimal)
                {
                    Precision = 19,
                    Scale = 4,
                    Value = balanceAmount
                },
                new SqlParameter("@balanceCurrency", SqlDbType.Char, 3) { Value = balanceCurrency },
                new SqlParameter("@createdAtUtc", SqlDbType.DateTime2) { Value = CreatedAtUtc }
            ];

            SqlException exception = await Assert.ThrowsAsync<SqlException>(() =>
                context.Database.ExecuteSqlRawAsync("""
                    INSERT INTO [BankAccounts]
                        ([Id], [CustomerId], [AccountNumber], [Type], [Status], [BalanceAmount],
                         [BalanceCurrency], [CreatedAtUtc])
                    VALUES
                        (@id, @customerId, @accountNumber, @type, @status, @balanceAmount,
                         @balanceCurrency, @createdAtUtc)
                    """, parameters, cancellationToken));

            Assert.Equal(547, exception.Number);
            Assert.Contains(
                expectedConstraintName,
                exception.Message,
                StringComparison.OrdinalIgnoreCase);
        });

    private static async Task WithTemporaryDatabaseAsync(
        Func<SecureBankingDbContext, CancellationToken, Task> test)
    {
        CancellationToken cancellationToken = CancellationToken.None;
        string databaseName = $"SecureBankingTests_{Guid.NewGuid():N}";
        SqlConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };
        DbContextOptions<SecureBankingDbContext> options =
            new DbContextOptionsBuilder<SecureBankingDbContext>()
                .UseSqlServer(connectionStringBuilder.ConnectionString)
                .Options;

        await using SecureBankingDbContext context = new(options);
        try
        {
            await context.Database.MigrateAsync(cancellationToken);
            await test(context, cancellationToken);
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
            await context.Database.EnsureDeletedAsync(cancellationToken);
        }
    }

    private static byte[] GetRowVersion<TEntity>(SecureBankingDbContext context, TEntity entity)
        where TEntity : class =>
        context.Entry(entity).Property<byte[]>("RowVersion").CurrentValue ??
        throw new InvalidOperationException("RowVersion was not generated.");

    private static async Task<bool> TableExistsAsync(
        SecureBankingDbContext context,
        string tableName,
        CancellationToken cancellationToken)
    {
        DbConnection connection = context.Database.GetDbConnection();
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE [name] = @tableName";
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture) == 1;
    }
}

internal static class SecureBankingDbContextOptionsExtensions
{
    public static DbContextOptions<SecureBankingDbContext> Options(this SecureBankingDbContext context) =>
        new DbContextOptionsBuilder<SecureBankingDbContext>()
            .UseSqlServer(
                context.Database.GetConnectionString() ??
                throw new InvalidOperationException("The test context has no connection string."))
            .Options;
}
