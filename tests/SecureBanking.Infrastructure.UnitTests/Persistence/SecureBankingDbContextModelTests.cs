using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SecureBanking.Domain.Accounts;
using SecureBanking.Domain.Customers;
using SecureBanking.Domain.ValueObjects;
using SecureBanking.Infrastructure.Persistence;

namespace SecureBanking.Infrastructure.UnitTests.Persistence;

public sealed class SecureBankingDbContextModelTests : IDisposable
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=SecureBankingModelTests;Trusted_Connection=True;TrustServerCertificate=True";

    private readonly SecureBankingDbContext _context;

    public SecureBankingDbContextModelTests()
    {
        DbContextOptions<SecureBankingDbContext> options =
            new DbContextOptionsBuilder<SecureBankingDbContext>()
                .UseSqlServer(ConnectionString)
                .ConfigureWarnings(warnings => warnings.Default(WarningBehavior.Throw))
                .Options;

        _context = new SecureBankingDbContext(options);
    }

    [Fact]
    public void Model_BuildsAndContainsExpectedEntitiesAndTables()
    {
        IEntityType customer = GetEntityType<Customer>();
        IEntityType bankAccount = GetEntityType<BankAccount>();

        Assert.Equal("Customers", customer.GetTableName());
        Assert.Equal("BankAccounts", bankAccount.GetTableName());
    }

    [Fact]
    public void CustomerTable_HasExactColumnsAndStringConstraints()
    {
        IEntityType customer = GetEntityType<Customer>();
        StoreObjectIdentifier table = GetTable(customer);
        string[] expectedColumns =
        [
            "CreatedAtUtc", "Email", "FirstName", "Id", "LastName", "RowVersion", "Status"
        ];

        string[] actualColumns = customer.GetProperties()
            .Select(property => property.GetColumnName(table)!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedColumns, actualColumns);
        Assert.Equal(100, GetProperty(customer, nameof(Customer.FirstName)).GetMaxLength());
        Assert.Equal(100, GetProperty(customer, nameof(Customer.LastName)).GetMaxLength());
        Assert.Equal(254, GetProperty(customer, nameof(Customer.Email)).GetMaxLength());
        Assert.True(GetProperty(customer, nameof(Customer.Status)).GetMaxLength() >=
            Enum.GetNames<CustomerStatus>().Max(name => name.Length));
        Assert.Equal("RowVersion", GetProperty(customer, "RowVersion").GetColumnName(table));

        IIndex emailIndex = Assert.Single(customer.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(Customer.Email)]));
        Assert.True(emailIndex.IsUnique);
        Assert.Equal("UX_Customers_Email", emailIndex.GetDatabaseName());
    }

    [Theory]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(BankAccount))]
    public void Entity_HasExpectedPrimaryKeyAndNonGeneratedIdentifier(Type entityClrType)
    {
        IEntityType entity = GetEntityType(entityClrType);
        IKey primaryKey = Assert.Single(entity.GetKeys(), key => key.IsPrimaryKey());
        IProperty id = Assert.Single(primaryKey.Properties);

        Assert.Equal(nameof(Customer.Id), id.Name);
        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);
    }

    [Fact]
    public void CustomerEmail_HasConverterConstraintsAndUniqueIndex()
    {
        IEntityType customer = GetEntityType<Customer>();
        IProperty email = GetProperty(customer, nameof(Customer.Email));
        ValueConverter converter = GetConverter(email);
        EmailAddress value = EmailAddress.Create("USER@example.com");

        Assert.False(email.IsNullable);
        Assert.Equal(254, email.GetMaxLength());
        Assert.Equal("user@example.com", converter.ConvertToProvider(value));
        Assert.Equal(value, converter.ConvertFromProvider("user@example.com"));
        Assert.Contains(customer.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(Customer.Email)]));
    }

    [Fact]
    public void AccountNumber_HasConverterConstraintsAndUniqueIndex()
    {
        IEntityType account = GetEntityType<BankAccount>();
        IProperty number = GetProperty(account, nameof(BankAccount.AccountNumber));
        ValueConverter converter = GetConverter(number);
        AccountNumber value = AccountNumber.Create("001234567890");

        Assert.False(number.IsNullable);
        Assert.Equal(12, number.GetMaxLength());
        Assert.True(number.IsFixedLength());
        Assert.Equal("char(12)", number.GetColumnType());
        Assert.Equal("001234567890", converter.ConvertToProvider(value));
        Assert.Equal(value, converter.ConvertFromProvider("001234567890"));
        Assert.Contains(account.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(BankAccount.AccountNumber)]));
    }

    [Fact]
    public void Balance_HasExplicitAmountAndCurrencyMappings()
    {
        IEntityType account = GetEntityType<BankAccount>();
        INavigation balanceNavigation = Assert.IsAssignableFrom<INavigation>(account.FindNavigation(nameof(BankAccount.Balance)));
        IEntityType balance = balanceNavigation.TargetEntityType;
        IProperty amount = GetProperty(balance, nameof(Money.Amount));
        IProperty currency = GetProperty(balance, nameof(Money.Currency));
        ValueConverter converter = GetConverter(currency);

        Assert.False(amount.IsNullable);
        Assert.Equal("decimal(19,4)", amount.GetColumnType());
        Assert.Equal("BalanceAmount", amount.GetColumnName());
        Assert.False(currency.IsNullable);
        Assert.Equal(3, currency.GetMaxLength());
        Assert.Equal("char(3)", currency.GetColumnType());
        Assert.Equal("BalanceCurrency", currency.GetColumnName());
        Assert.Equal("CRC", converter.ConvertToProvider(Currency.Crc));
        Assert.Equal(Currency.Crc, converter.ConvertFromProvider("CRC"));
        Assert.Equal("USD", converter.ConvertToProvider(Currency.Usd));
        Assert.Equal(Currency.Usd, converter.ConvertFromProvider("USD"));
    }

    [Fact]
    public void BankAccountTable_HasExactColumnsIndexesAndConstraints()
    {
        IEntityType account = GetEntityType<BankAccount>();
        INavigation balanceNavigation = GetBalanceNavigation(account);
        IEntityType balance = balanceNavigation.TargetEntityType;
        StoreObjectIdentifier table = GetTable(account);
        string[] expectedColumns =
        [
            "AccountNumber", "BalanceAmount", "BalanceCurrency", "CreatedAtUtc", "CustomerId",
            "Id", "RowVersion", "Status", "Type"
        ];

        string[] actualColumns = account.GetProperties()
            .Concat(balance.GetProperties())
            .Select(property => property.GetColumnName(table))
            .Where(columnName => columnName is not null)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray()!;

        Assert.Equal(expectedColumns, actualColumns);
        Assert.Equal("char(12)", GetProperty(account, nameof(BankAccount.AccountNumber)).GetColumnType());
        Assert.Equal("decimal(19,4)", GetProperty(balance, nameof(Money.Amount)).GetColumnType());
        Assert.Equal("char(3)", GetProperty(balance, nameof(Money.Currency)).GetColumnType());
        Assert.True(GetProperty(account, nameof(BankAccount.Type)).GetMaxLength() >=
            Enum.GetNames<AccountType>().Max(name => name.Length));
        Assert.True(GetProperty(account, nameof(BankAccount.Status)).GetMaxLength() >=
            Enum.GetNames<AccountStatus>().Max(name => name.Length));
        Assert.Equal("RowVersion", GetProperty(account, "RowVersion").GetColumnName(table));

        IIndex accountNumberIndex = Assert.Single(account.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(BankAccount.AccountNumber)]));
        IIndex customerIndex = Assert.Single(account.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(BankAccount.CustomerId)]));
        Assert.True(accountNumberIndex.IsUnique);
        Assert.Equal("UX_BankAccounts_AccountNumber", accountNumberIndex.GetDatabaseName());
        Assert.Equal("IX_BankAccounts_CustomerId", customerIndex.GetDatabaseName());

        IEntityType designTimeAccount = _context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(BankAccount)) ??
            throw new InvalidOperationException("BankAccount is not in the design-time model.");
        Assert.Equal("BankAccounts", designTimeAccount.GetTableName());
        IReadOnlyDictionary<string, string> constraints = designTimeAccount.GetCheckConstraints()
            .ToDictionary(
                constraint => constraint.Name ?? throw new InvalidOperationException("Check constraint has no name."),
                constraint => constraint.Sql ?? throw new InvalidOperationException("Check constraint has no SQL."),
                StringComparer.Ordinal);
        Assert.Equal(
            "LEN([AccountNumber]) = 12 AND " +
            "[AccountNumber] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'",
            constraints["CK_BankAccounts_AccountNumber_Length"]);
        Assert.Equal(
            "[BalanceAmount] >= 0 AND [BalanceAmount] <= 999999999999999.9999",
            constraints["CK_BankAccounts_BalanceAmount_Range"]);
        Assert.Equal(
            "[BalanceCurrency] IN ('CRC', 'USD')",
            constraints["CK_BankAccounts_BalanceCurrency_Supported"]);
    }

    [Fact]
    public void Balance_IsRequiredOwnedTypeSharingBankAccountsTable()
    {
        IEntityType account = GetEntityType<BankAccount>();
        INavigation navigation = GetBalanceNavigation(account);
        IEntityType balance = navigation.TargetEntityType;
        IForeignKey ownership = Assert.IsAssignableFrom<IForeignKey>(balance.FindOwnership());
        IKey ownedKey = Assert.IsAssignableFrom<IKey>(balance.FindPrimaryKey());

        Assert.True(balance.IsOwned());
        Assert.Equal("BankAccounts", balance.GetTableName());
        Assert.True(navigation.ForeignKey.IsRequiredDependent);
        Assert.True(ownership.IsOwnership);
        Assert.Same(account, ownership.PrincipalEntityType);
        Assert.Equal(ownership.Properties, ownedKey.Properties);
        Assert.Equal(account.FindPrimaryKey()!.Properties, ownership.PrincipalKey.Properties);
        Assert.Single(_context.Model.GetEntityTypes(), entityType => entityType.IsOwned());
        Assert.Equal("BalanceAmount", GetProperty(balance, nameof(Money.Amount)).GetColumnName(GetTable(account)));
        Assert.Equal("BalanceCurrency", GetProperty(balance, nameof(Money.Currency)).GetColumnName(GetTable(account)));
    }

    [Theory]
    [InlineData(typeof(Customer), nameof(Customer.Status))]
    [InlineData(typeof(BankAccount), nameof(BankAccount.Type))]
    [InlineData(typeof(BankAccount), nameof(BankAccount.Status))]
    public void EnumProperty_UsesStableStringConversion(Type entityClrType, string propertyName)
    {
        IProperty property = GetProperty(GetEntityType(entityClrType), propertyName);

        Assert.Equal(typeof(string), GetConverter(property).ProviderClrType);
        Assert.False(property.IsNullable);
    }

    [Fact]
    public void EnumConverters_PersistExplicitReadableValues()
    {
        ValueConverter customerStatus = GetConverter(GetProperty(GetEntityType<Customer>(), nameof(Customer.Status)));
        ValueConverter accountType = GetConverter(GetProperty(GetEntityType<BankAccount>(), nameof(BankAccount.Type)));
        ValueConverter accountStatus = GetConverter(GetProperty(GetEntityType<BankAccount>(), nameof(BankAccount.Status)));

        Assert.Equal("Active", customerStatus.ConvertToProvider(CustomerStatus.Active));
        Assert.Equal("Suspended", customerStatus.ConvertToProvider(CustomerStatus.Suspended));
        Assert.Equal("Closed", customerStatus.ConvertToProvider(CustomerStatus.Closed));
        Assert.Equal("Savings", accountType.ConvertToProvider(AccountType.Savings));
        Assert.Equal("Checking", accountType.ConvertToProvider(AccountType.Checking));
        Assert.Equal("Active", accountStatus.ConvertToProvider(AccountStatus.Active));
        Assert.Equal("Frozen", accountStatus.ConvertToProvider(AccountStatus.Frozen));
        Assert.Equal("Closed", accountStatus.ConvertToProvider(AccountStatus.Closed));
    }

    [Theory]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(BankAccount))]
    public void CreatedAtUtc_IsRequiredDateTime2(Type entityClrType)
    {
        IProperty property = GetProperty(GetEntityType(entityClrType), nameof(Customer.CreatedAtUtc));

        Assert.False(property.IsNullable);
        Assert.Equal("datetime2", property.GetColumnType());
        Assert.IsAssignableFrom<ValueConverter<DateTime, DateTime>>(GetConverter(property));
    }

    [Theory]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(BankAccount))]
    public void UtcDateTimeConverter_PreservesUtcValueAndTicksInBothDirections(Type entityClrType)
    {
        ValueConverter converter = GetCreatedAtUtcConverter(entityClrType);
        DateTime utc = new DateTime(2026, 9, 23, 12, 34, 56, 789, DateTimeKind.Utc).AddTicks(1234);

        DateTime provider = Assert.IsType<DateTime>(converter.ConvertToProvider(utc));
        DateTime databaseValue = DateTime.SpecifyKind(provider, DateTimeKind.Unspecified);
        DateTime model = Assert.IsType<DateTime>(converter.ConvertFromProvider(databaseValue));

        Assert.Equal(utc.Ticks, provider.Ticks);
        Assert.Equal(utc, provider);
        Assert.Equal(DateTimeKind.Utc, provider.Kind);
        Assert.Equal(databaseValue.Ticks, model.Ticks);
        Assert.Equal(DateTimeKind.Utc, model.Kind);
    }

    [Theory]
    [InlineData(typeof(Customer), DateTimeKind.Local)]
    [InlineData(typeof(Customer), DateTimeKind.Unspecified)]
    [InlineData(typeof(BankAccount), DateTimeKind.Local)]
    [InlineData(typeof(BankAccount), DateTimeKind.Unspecified)]
    public void UtcDateTimeConverter_RejectsNonUtcValues(Type entityClrType, DateTimeKind kind)
    {
        ValueConverter converter = GetCreatedAtUtcConverter(entityClrType);
        DateTime value = new(2026, 9, 23, 12, 0, 0, kind);

        Assert.Throws<InvalidOperationException>(() => converter.ConvertToProvider(value));
    }

    [Fact]
    public void BankAccount_HasRequiredRestrictedCustomerRelationshipAndIndex()
    {
        IEntityType account = GetEntityType<BankAccount>();
        IForeignKey foreignKey = Assert.Single(account.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(Customer));

        Assert.True(foreignKey.IsRequired);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(nameof(BankAccount.CustomerId), Assert.Single(foreignKey.Properties).Name);
        Assert.Equal("FK_BankAccounts_Customers_CustomerId", foreignKey.GetConstraintName());
        Assert.Contains(account.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([nameof(BankAccount.CustomerId)]));
    }

    [Theory]
    [InlineData(typeof(Customer))]
    [InlineData(typeof(BankAccount))]
    public void RowVersion_HasSqlServerConcurrencySemantics(Type entityClrType)
    {
        IProperty rowVersion = GetProperty(GetEntityType(entityClrType), "RowVersion");

        Assert.Equal(typeof(byte[]), rowVersion.ClrType);
        Assert.False(rowVersion.IsNullable);
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
        Assert.Equal("rowversion", rowVersion.GetColumnType());
    }

    [Fact]
    public void Domain_DoesNotReferenceEntityFrameworkCore()
    {
        Assembly domainAssembly = typeof(Customer).Assembly;

        Assert.DoesNotContain(domainAssembly.GetReferencedAssemblies(), reference =>
            reference.Name?.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Application_DoesNotReferenceInfrastructure()
    {
        Assembly applicationAssembly = Assembly.Load("SecureBanking.Application");

        Assert.DoesNotContain(applicationAssembly.GetReferencedAssemblies(), reference =>
            reference.Name == "SecureBanking.Infrastructure");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private IEntityType GetEntityType<T>() => GetEntityType(typeof(T));

    private IEntityType GetEntityType(Type clrType) =>
        _context.Model.FindEntityType(clrType) ?? throw new InvalidOperationException($"{clrType.Name} is not in the model.");

    private static INavigation GetBalanceNavigation(IEntityType account) =>
        account.FindNavigation(nameof(BankAccount.Balance)) ??
        throw new InvalidOperationException("Balance navigation is not mapped.");

    private static StoreObjectIdentifier GetTable(IEntityType entityType) =>
        StoreObjectIdentifier.Table(
            entityType.GetTableName() ?? throw new InvalidOperationException($"{entityType.DisplayName()} has no table."),
            entityType.GetSchema());

    private static IProperty GetProperty(IEntityType entityType, string propertyName) =>
        entityType.FindProperty(propertyName) ?? throw new InvalidOperationException($"{propertyName} is not mapped.");

    private ValueConverter GetCreatedAtUtcConverter(Type entityClrType)
    {
        ValueConverter converter = GetConverter(GetProperty(GetEntityType(entityClrType), nameof(Customer.CreatedAtUtc)));

        Assert.Equal(
            "SecureBanking.Infrastructure.Persistence.Converters.UtcDateTimeConverter",
            converter.GetType().FullName);

        return converter;
    }

    private static ValueConverter GetConverter(IProperty property) =>
        property.GetValueConverter() ?? property.GetTypeMapping().Converter ??
        throw new InvalidOperationException($"{property.Name} does not have a converter.");
}
