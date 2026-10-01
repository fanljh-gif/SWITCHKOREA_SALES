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
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.DbTests;

public sealed class EndToEndTests
{
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
