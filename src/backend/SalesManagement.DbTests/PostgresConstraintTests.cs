using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SalesManagement.Core.Models;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;
using SalesManagement.Core.Exceptions;

namespace SalesManagement.DbTests;

public sealed class PostgresConstraintTests
{
    [Fact]
    public async Task CustomerCode_Is_Unique()
    {
        await using var fixture = await Fixture.CreateAsync();
        var owner = await fixture.AddUser();
        fixture.Context.Customers.AddRange(Customer("C001", owner.Id), Customer("C001", owner.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task CustomerShipmentNumber_Is_Unique_When_Not_Null()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (owner, customer, product) = await fixture.AddSalesReferences();
        fixture.Context.Shipments.AddRange(Shipment(customer.Id, product.Id, owner.Id, "S001"), Shipment(customer.Id, product.Id, owner.Id, "S001"));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Null_Shipment_Numbers_Can_Repeat()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (owner, customer, product) = await fixture.AddSalesReferences();
        fixture.Context.Shipments.AddRange(Shipment(customer.Id, product.Id, owner.Id, null), Shipment(customer.Id, product.Id, owner.Id, null));
        Assert.Equal(2, await fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Foreign_Key_Integrity_Is_Enforced()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Context.Customers.Add(Customer("C001", Guid.NewGuid()));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task StaleVersion_Update_Is_Rejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var owner = await fixture.AddUser();
        var customer = Customer("C001", owner.Id); fixture.Context.Customers.Add(customer); await fixture.Context.SaveChangesAsync();
        await using var first = fixture.NewContext(); await using var second = fixture.NewContext();
        var a = await first.Customers.SingleAsync(); var b = await second.Customers.SingleAsync();
        a.CompanyName = "A"; await first.SaveChangesAsync();
        b.CompanyName = "B"; await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public void Initial_Migration_Contains_Price_Overlap_Exclusion()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseNpgsql("Host=localhost;Database=test;Username=test").Options;
        using var context = new SalesDbContext(options);
        var migrations = context.GetService<IMigrationsAssembly>();
        var initial = migrations.Migrations.Single(x => x.Key.EndsWith("_InitialCreate", StringComparison.Ordinal)).Value;
        var migration = migrations.CreateMigration(initial, "10.0");
        Assert.Contains(migration.UpOperations.OfType<SqlOperation>(), operation => operation.Sql.Contains("EX_prices_no_overlap", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Cancelled_Shipment_Is_Preserved()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (owner, customer, product) = await fixture.AddSalesReferences();
        var shipment = Shipment(customer.Id, product.Id, owner.Id, "S001"); fixture.Context.Add(shipment); await fixture.Context.SaveChangesAsync();
        shipment.Status = ShipmentStatus.Cancelled; await fixture.Context.SaveChangesAsync();
        Assert.Equal(ShipmentStatus.Cancelled, (await fixture.Context.Shipments.SingleAsync()).Status);
        Assert.Equal(1, await fixture.Context.Shipments.CountAsync());
    }

    [Fact]
    public async Task Failed_Import_Audit_Transaction_Rolls_Back()
    {
        await using var fixture = await Fixture.CreateAsync();
        var actor = await fixture.AddUser();
        var runner = new PostgresTransactionRunner(fixture.Context); var audits = new AuditRepository(fixture.Context);
        await Assert.ThrowsAsync<InvalidOperationException>(() => runner.ExecuteAsync<int>(async ct =>
        {
            await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = "import:test", Action = "commit" }, ct);
            throw new InvalidOperationException("rollback");
        }));
        Assert.Empty(await fixture.Context.AuditEntries.AsNoTracking().ToArrayAsync());
    }

    [Fact]
    public async Task Model_Defines_Required_Unique_Keys_And_Decimal_Precision()
    {
        await using var fixture = await Fixture.CreateAsync();
        static bool HasUnique<TEntity>(SalesDbContext db, params string[] properties) where TEntity : class =>
            db.Model.FindEntityType(typeof(TEntity))!.GetIndexes().Any(index => index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual(properties));
        Assert.True(HasUnique<User>(fixture.Context, nameof(User.LoginId)));
        Assert.True(HasUnique<Product>(fixture.Context, nameof(Product.Code)));
        Assert.True(HasUnique<SalesTarget>(fixture.Context, nameof(SalesTarget.UserId), nameof(SalesTarget.YearMonth)));
        Assert.True(HasUnique<MonthlyRate>(fixture.Context, nameof(MonthlyRate.YearMonth), nameof(MonthlyRate.Currency)));
        var shipment = fixture.Context.Model.FindEntityType(typeof(SalesManagement.Core.Models.Shipment))!;
        Assert.Equal(20, shipment.FindProperty(nameof(SalesManagement.Core.Models.Shipment.Quantity))!.GetPrecision());
        Assert.Equal(4, shipment.FindProperty(nameof(SalesManagement.Core.Models.Shipment.Quantity))!.GetScale());
        Assert.Equal(8, shipment.FindProperty(nameof(SalesManagement.Core.Models.Shipment.AppliedRate))!.GetScale());
    }

    [Fact]
    public async Task Network_Customer_Link_Is_Unique_When_Not_Null()
    {
        await using var fixture = await Fixture.CreateAsync();
        var (owner, customer, _) = await fixture.AddSalesReferences();
        fixture.Context.NetworkCompanies.AddRange(NetworkCompany("A", owner.Id, customer.Id), NetworkCompany("B", owner.Id, customer.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Only_One_Current_Assignment_Per_Company_Is_Allowed()
    {
        await using var fixture = await Fixture.CreateAsync();
        var owner = await fixture.AddUser();
        var company = NetworkCompany("A", owner.Id); fixture.Context.NetworkCompanies.Add(company); await fixture.Context.SaveChangesAsync();
        fixture.Context.CompanyAssignments.AddRange(Assignment(company.Id, owner.Id, true), Assignment(company.Id, owner.Id, true));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Duplicate_Active_Relationship_Period_Is_Rejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var owner = await fixture.AddUser();
        var up = NetworkCompany("Up", owner.Id); var down = NetworkCompany("Down", owner.Id);
        fixture.Context.NetworkCompanies.AddRange(up, down); await fixture.Context.SaveChangesAsync();
        fixture.Context.CompanyRelationships.AddRange(Relationship(up.Id, down.Id), Relationship(up.Id, down.Id));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Network_Foreign_Keys_Are_Required()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Context.CompanyContacts.Add(new CompanyContact { CompanyId = Guid.NewGuid(), Name = "담당자", Department = "구매", Position = "과장", BusinessRole = "구매", MobilePhone = "010", Email = "a@example.com", ValidFrom = new DateOnly(2026, 1, 1) });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Network_Stale_Version_Is_Rejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var owner = await fixture.AddUser();
        fixture.Context.NetworkCompanies.Add(NetworkCompany("A", owner.Id)); await fixture.Context.SaveChangesAsync();
        await using var first = fixture.NewContext(); await using var second = fixture.NewContext();
        var a = await first.NetworkCompanies.SingleAsync(); var b = await second.NetworkCompanies.SingleAsync();
        a.CompanyName = "A1"; await first.SaveChangesAsync();
        b.CompanyName = "A2"; await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Assignment_Repository_Rejects_Overlapping_History()
    {
        await using var fixture = await Fixture.CreateAsync();
        var owner = await fixture.AddUser(); var company = NetworkCompany("A", owner.Id);
        fixture.Context.NetworkCompanies.Add(company); await fixture.Context.SaveChangesAsync();
        var repository = new CompanyAssignmentRepository(fixture.Context);
        await repository.AddAsync(new CompanyAssignment { CompanyId = company.Id, UserId = owner.Id, ValidFrom = new DateOnly(2026, 1, 1), ValidTo = new DateOnly(2026, 12, 31), IsCurrent = false });

        await Assert.ThrowsAsync<NetworkValidationException>(() => repository.AddAsync(new CompanyAssignment { CompanyId = company.Id, UserId = owner.Id, ValidFrom = new DateOnly(2026, 6, 1), ValidTo = new DateOnly(2027, 1, 1), IsCurrent = false }));
    }

    [Fact]
    public async Task Relationship_Repository_Update_Replaces_Supplies()
    {
        await using var fixture = await Fixture.CreateAsync();
        var owner = await fixture.AddUser();
        var up = NetworkCompany("Up", owner.Id); var down = NetworkCompany("Down", owner.Id);
        var firstProduct = new Product { Code = "P001", Name = "스위치 1", ItemType = ItemType.Product };
        var secondProduct = new Product { Code = "P002", Name = "스위치 2", ItemType = ItemType.Product };
        fixture.Context.AddRange(up, down, firstProduct, secondProduct); await fixture.Context.SaveChangesAsync();
        var repository = new CompanyRelationshipRepository(fixture.Context);
        var relationship = Relationship(up.Id, down.Id);
        relationship.Supplies = [new RelationshipSupply { RelationshipId = relationship.Id, ProductId = firstProduct.Id, ApplicationName = "기존" }];
        relationship = await repository.AddAsync(relationship);

        relationship.Supplies = [new RelationshipSupply { RelationshipId = relationship.Id, ProductId = secondProduct.Id, ApplicationName = "변경" }];
        await repository.UpdateAsync(relationship, relationship.Version);
        fixture.Context.ChangeTracker.Clear();

        var supplies = await fixture.Context.RelationshipSupplies.AsNoTracking().Where(x => x.RelationshipId == relationship.Id).ToArrayAsync();
        Assert.Equal(secondProduct.Id, Assert.Single(supplies).ProductId);
        Assert.Equal("변경", supplies[0].ApplicationName);
    }

    private static Customer Customer(string code, Guid ownerId) => new() { Code = code, CompanyName = "거래처", Country = "KR", DefaultCurrency = Currency.KRW, OwnerUserId = ownerId };
    private static Shipment Shipment(Guid customerId, Guid productId, Guid ownerId, string? number) => new() { ShipmentNumber = number, ShipmentDate = new DateOnly(2026, 10, 1), CustomerId = customerId, ProductId = productId, Quantity = 1m, UnitPrice = 1m, Currency = Currency.KRW, SupplyAmount = 1m, AppliedRate = 1m, KrwAmount = 1m, OwnerUserId = ownerId };
    private static NetworkCompany NetworkCompany(string name, Guid ownerId, Guid? customerId = null) => new() { CompanyName = name, Country = "KR", Industry = "전자", LifecycleType = customerId.HasValue ? LifecycleType.Customer : LifecycleType.Prospect, CustomerId = customerId, CurrentOwnerUserId = ownerId };
    private static CompanyAssignment Assignment(Guid companyId, Guid userId, bool current) => new() { CompanyId = companyId, UserId = userId, ValidFrom = new DateOnly(2026, 1, 1), IsCurrent = current };
    private static CompanyRelationship Relationship(Guid up, Guid down) => new() { UpstreamCompanyId = up, DownstreamCompanyId = down, ValidFrom = new DateOnly(2026, 1, 1), Status = EntityStatus.Active };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<SalesDbContext> _options;
        public SalesDbContext Context { get; }
        private Fixture(SqliteConnection connection, DbContextOptions<SalesDbContext> options, SalesDbContext context) { _connection = connection; _options = options; Context = context; }
        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connection).Options;
            var context = new SalesDbContext(options); await context.Database.EnsureCreatedAsync();
            return new Fixture(connection, options, context);
        }
        public SalesDbContext NewContext() => new(_options);
        public async Task<User> AddUser() { var user = new User { LoginId = Guid.NewGuid().ToString("N"), PasswordHash = "hash", Name = "사용자", Email = "user@switchkorea.com", Role = Role.Member }; Context.Users.Add(user); await Context.SaveChangesAsync(); return user; }
        public async Task<(User Owner, Customer Customer, Product Product)> AddSalesReferences() { var owner = await AddUser(); var customer = Customer("C001", owner.Id); var product = new Product { Code = "P001", Name = "스위치", ItemType = ItemType.Product }; Context.AddRange(customer, product); await Context.SaveChangesAsync(); return (owner, customer, product); }
        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await _connection.DisposeAsync(); }
    }
}
