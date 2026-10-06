using ClosedXML.Excel;
using Microsoft.AspNetCore.Identity;
using SalesManagement.Core.Admin;
using SalesManagement.Core.Audit;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Customers;
using SalesManagement.Core.Dashboard;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Imports;
using SalesManagement.Core.Models;
using SalesManagement.Core.Prices;
using SalesManagement.Core.Products;
using SalesManagement.Core.Rates;
using SalesManagement.Core.Shipments;
using SalesManagement.Core.Targets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;
using SalesManagement.Core.Network;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.DbTests;

public sealed class EndToEndTests
{
    [Fact]
    public async Task Existing_Customers_Are_Backfilled_Exactly_Once()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connection).Options;
        await using var db = new SalesDbContext(options); await db.Database.EnsureCreatedAsync();
        var owner = new User { LoginId = "owner", PasswordHash = "hash", Name = "담당자", Email = "owner@switchkorea.com", Role = Role.Member };
        db.Users.Add(owner);
        var first = new Customer { Code = "C001", CompanyName = "고객 A", Country = "KR", DefaultCurrency = Currency.KRW, OwnerUserId = owner.Id };
        var second = new Customer { Code = "C002", CompanyName = "고객 B", Country = "CN", DefaultCurrency = Currency.CNY, OwnerUserId = owner.Id };
        db.Customers.AddRange(first, second); await db.SaveChangesAsync();
        var existingNetwork = new NetworkCompany { CompanyName = first.CompanyName, Country = first.Country, Industry = "전자", LifecycleType = LifecycleType.Customer, CustomerId = first.Id, CurrentOwnerUserId = owner.Id };
        db.NetworkCompanies.Add(existingNetwork); await db.SaveChangesAsync();

        var backfill = new NetworkCustomerBackfill(db);
        await backfill.ApplyAsync(); await backfill.ApplyAsync();

        Assert.Equal(2, await db.NetworkCompanies.CountAsync());
        Assert.Equal(2, await db.CompanyAssignments.CountAsync());
        Assert.Equal(new[] { first.Id, second.Id }.Order(), await db.NetworkCompanies.Select(x => x.CustomerId!.Value).OrderBy(x => x).ToArrayAsync());
        Assert.All(await db.CompanyAssignments.ToArrayAsync(), x => Assert.Equal(owner.Id, x.UserId));
        Assert.Contains(await db.CompanyAssignments.ToArrayAsync(), x => x.CompanyId == existingNetwork.Id);
    }

    [Fact]
    public async Task Network_Workflow_Persists_All_Histories()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connection).Options;
        await using var db = new SalesDbContext(options); await db.Database.EnsureCreatedAsync();
        var manager = new User { LoginId = "manager", PasswordHash = "hash", Name = "이재학", Email = "manager@switchkorea.com", Role = Role.Manager };
        var nextOwner = new User { LoginId = "next", PasswordHash = "hash", Name = "박태원", Email = "next@switchkorea.com", Role = Role.Member };
        var product = new Product { Code = "SW-001", Name = "스위치", ItemType = ItemType.Product };
        db.AddRange(manager, nextOwner, product); await db.SaveChangesAsync();
        var companies = new NetworkCompanyRepository(db); var relationships = new CompanyRelationshipRepository(db);
        var contacts = new CompanyContactRepository(db); var assignments = new CompanyAssignmentRepository(db);
        var customers = new CustomerRepository(db); var users = new UserRepository(db); var audits = new AuditRepository(db);
        var runner = new PostgresTransactionRunner(db);
        var samsung = await companies.AddAsync(new NetworkCompany { CompanyName = "삼성", Country = "KR", Industry = "전자", LifecycleType = LifecycleType.Customer, IsOem = true, CurrentOwnerUserId = manager.Id });
        var lg = await companies.AddAsync(new NetworkCompany { CompanyName = "LG", Country = "KR", Industry = "전자", LifecycleType = LifecycleType.Customer, IsOem = true, CurrentOwnerUserId = manager.Id });
        var companyService = new NetworkCompanyService(companies, customers, assignments, users, audits, runner);
        var prospect = await companyService.SaveCompanyAsync(new NetworkCompanySaveCommand(null, "잠재사", "KR", "부품", LifecycleType.Prospect, false, manager.Id, EntityStatus.Active), manager);
        var relationshipService = new NetworkRelationshipService(relationships, companies, assignments, new ProductRepository(db), audits);
        var supplies = new[] { new RelationshipSupplyCommand(product.Id, "EV 프로젝트", null) };
        await relationshipService.SaveRelationshipAsync(new(null, samsung.Id, prospect.Id, new DateOnly(2026, 1, 1), null, null, EntityStatus.Active, supplies), manager);
        await relationshipService.SaveRelationshipAsync(new(null, lg.Id, prospect.Id, new DateOnly(2026, 1, 1), null, null, EntityStatus.Active, supplies), manager);
        var contactService = new CompanyContactService(contacts, companies, assignments, audits);
        await contactService.SaveContactAsync(prospect.Id, new(null, "김담당", "구매", "과장", "구매", "010-0000-0000", "buyer@example.com", new DateOnly(2026, 1, 1), null, EmploymentStatus.Active), manager);
        prospect = await companyService.ConvertProspectAsync(prospect.Id, "C900", Currency.KRW, prospect.Version, manager);
        var assignmentService = new CompanyAssignmentService(assignments, companies, users, customers, audits, runner);
        await assignmentService.ChangeAssignmentAsync(prospect.Id, nextOwner.Id, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1), prospect.Version, manager);

        db.ChangeTracker.Clear();
        Assert.Equal(2, await db.CompanyRelationships.CountAsync(x => x.DownstreamCompanyId == prospect.Id));
        Assert.Single(await db.CompanyContacts.Where(x => x.CompanyId == prospect.Id).ToArrayAsync());
        Assert.Equal(2, await db.CompanyAssignments.CountAsync(x => x.CompanyId == prospect.Id));
        var reloaded = await db.NetworkCompanies.SingleAsync(x => x.Id == prospect.Id);
        Assert.Equal(LifecycleType.Customer, reloaded.LifecycleType);
        Assert.Equal(nextOwner.Id, reloaded.CurrentOwnerUserId);
        Assert.NotNull(reloaded.CustomerId);
    }

    [Fact]
    public void AddSalesNetwork_Migration_Contains_Backfill_And_Overlap_Constraints()
    {
        var options = new DbContextOptionsBuilder<SalesDbContext>().UseNpgsql("Host=localhost;Database=test;Username=test").Options;
        using var context = new SalesDbContext(options);
        var migrations = context.GetService<IMigrationsAssembly>();
        var type = migrations.Migrations.Single(x => x.Key.EndsWith("_AddSalesNetwork", StringComparison.Ordinal)).Value;
        var migration = migrations.CreateMigration(type, "10.0");
        var sql = string.Join("\n", migration.UpOperations.OfType<SqlOperation>().Select(x => x.Sql));
        Assert.Contains("INSERT INTO network_companies", sql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT", sql, StringComparison.Ordinal);
        Assert.Contains("EX_company_relationships_no_overlap", sql, StringComparison.Ordinal);
        Assert.Contains("EX_company_assignments_no_overlap", sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Manager_To_Dashboard_And_Ownership_Denial_Flow_Works()
    {
        var users = new InMemoryUserRepository(); var customers = new InMemoryCustomerRepository(); var products = new InMemoryProductRepository();
        var prices = new InMemoryPriceRepository(); var shipments = new InMemoryShipmentRepository(); var rates = new InMemoryRateRepository();
        var targets = new InMemoryTargetRepository(); var audits = new InMemoryAuditRepository(); var batches = new InMemoryImportBatchRepository();
        var hasher = new PasswordHasher<User>(); var audit = new AuditService(audits);
        var manager = new User { LoginId = "jhlee", PasswordHash = string.Empty, Name = "이재학", Email = "jhlee@switchkorea.com", Role = Role.Manager };
        manager.PasswordHash = hasher.HashPassword(manager, "manager-password"); await users.AddAsync(manager);
        var login = await new AuthService(users, hasher, TimeProvider.System, AuthOptions.Default).LoginAsync("jhlee", "manager-password");
        var admin = new AdminUserService(users, hasher, audit);
        var member = await admin.CreateAsync(new CreateUserCommand("ptw", "member-password", "박태원", "ptw@switchkorea.com", Role.Member), login.User, CancellationToken.None);
        var other = await admin.CreateAsync(new CreateUserCommand("lso", "member-password", "이소연", "lso@switchkorea.com", Role.Member), login.User, CancellationToken.None);
        var customerService = new CustomerService(customers);
        var customer = await customerService.SaveAsync(new CustomerSaveCommand(null, "C001", "삼성 협력사", "KR", Currency.USD, member.Id, EntityStatus.Active), manager, 0);
        var product = await new ProductService(products).SaveAsync(new ProductSaveCommand(null, "P001", "마이크로 스위치", ItemType.Product, "SPDT", EntityStatus.Active), manager, 0);
        await new PriceService(prices, customers).SaveAsync(new PriceSaveCommand(null, customer.Id, product.Id, Currency.USD, 10m, new DateOnly(2026, 10, 1), null), manager, 0);
        await new RateService(rates, audits).SaveMonthlyAsync(new YearMonth(2026, 10), Currency.USD, 1_350m, 0, manager, CancellationToken.None);
        await new TargetService(targets, audits).SaveAsync(new YearMonth(2026, 10), member.Id, 100_000m, 0, manager, CancellationToken.None);
        var shipmentService = new ShipmentService(prices, rates, shipments, customers, audits);
        await shipmentService.SaveAsync(new ShipmentCreateCommand("S001", new DateOnly(2026, 10, 1), customer.Id, product.Id, 3m), member, CancellationToken.None);

        using var workbookStream = ShipmentWorkbook();
        var importer = new ExcelImportService(users, customers, products, prices, shipments, batches, shipmentService, TimeProvider.System);
        var validation = await importer.ValidateAsync(workbookStream, ImportKind.Shipment, "shipments.xlsx", member, CancellationToken.None);
        Assert.DoesNotContain(validation.Rows, row => row.Status == ImportRowStatus.Invalid);
        await importer.CommitAsync(validation.BatchId, member, CancellationToken.None);

        var dashboard = await new DashboardService(shipments, targets).QueryAsync(new DashboardQuery(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), member.Id), CancellationToken.None);
        Assert.Equal(67_500m, dashboard.KrwTotal);
        Assert.Equal(67.5m, dashboard.AchievementRate);
        await Assert.ThrowsAsync<ForbiddenException>(() => customerService.SaveAsync(new CustomerSaveCommand(customer.Id, customer.Code, "변경 시도", customer.Country, customer.DefaultCurrency, customer.OwnerUserId, customer.Status), other, customer.Version));
    }

    private static MemoryStream ShipmentWorkbook()
    {
        var stream = new MemoryStream(); using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Data");
            var values = new[] { ("ShipmentNumber", "S002"), ("ShipmentDate", "2026-10-02"), ("CustomerCode", "C001"), ("ProductCode", "P001"), ("Quantity", "2") };
            for (var i = 0; i < values.Length; i++) { sheet.Cell(1, i + 1).Value = values[i].Item1; sheet.Cell(2, i + 1).Value = values[i].Item2; }
            workbook.SaveAs(stream);
        }
        stream.Position = 0; return stream;
    }
}
