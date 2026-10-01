using ClosedXML.Excel;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Imports;
using SalesManagement.Core.Models;
using SalesManagement.Core.Shipments;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class ExcelImportTests
{
    [Fact]
    public async Task Corrupt_Workbook_Returns_Actionable_Error()
    {
        var fixture = Fixture.Create();
        var result = await fixture.Service.ValidateAsync(new MemoryStream([1, 2, 3]), ImportKind.Shipment, "bad.xlsx", fixture.Actor, CancellationToken.None);
        Assert.Contains(result.Errors, error => error.Message == "파일을 읽을 수 없습니다");
    }

    [Fact]
    public async Task Missing_Columns_Returns_Actionable_Error()
    {
        var fixture = Fixture.Create();
        using var stream = Workbook(("Wrong", "value"));
        var result = await fixture.Service.ValidateAsync(stream, ImportKind.Shipment, "missing.xlsx", fixture.Actor, CancellationToken.None);
        Assert.Contains(result.Errors, error => error.Message == "필수 열이 없습니다");
    }

    [Fact]
    public async Task Commit_With_Fatal_Row_Writes_Nothing()
    {
        var fixture = Fixture.Create();
        using var stream = Workbook(("ShipmentNumber", "S1"), ("ShipmentDate", "2026-10-01"), ("CustomerCode", "C1"), ("ProductCode", "P1"), ("Quantity", "invalid"));
        var validation = await fixture.Service.ValidateAsync(stream, ImportKind.Shipment, "fatal.xlsx", fixture.Actor, CancellationToken.None);
        await Assert.ThrowsAsync<InvalidImportBatchException>(() => fixture.Service.CommitAsync(validation.BatchId, fixture.Actor, CancellationToken.None));
        Assert.Empty(await fixture.Shipments.ListAsync());
    }

    [Fact]
    public async Task Missing_Number_Duplicate_Is_Returned_As_Candidate()
    {
        var fixture = Fixture.Create();
        await fixture.Shipments.AddAsync(new Shipment { ShipmentDate = new DateOnly(2026, 10, 1), CustomerId = fixture.Customer.Id, ProductId = fixture.Product.Id, Quantity = 2m, OwnerUserId = fixture.Actor.Id });
        using var stream = Workbook(("ShipmentNumber", ""), ("ShipmentDate", "2026-10-01"), ("CustomerCode", "C1"), ("ProductCode", "P1"), ("Quantity", "2"));
        var result = await fixture.Service.ValidateAsync(stream, ImportKind.Shipment, "duplicate.xlsx", fixture.Actor, CancellationToken.None);
        Assert.Contains(result.Rows, row => row.Status == ImportRowStatus.Duplicate);
    }

    private static MemoryStream Workbook(params (string Header, string Value)[] cells)
    {
        var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var sheet = workbook.AddWorksheet("Data");
            for (var index = 0; index < cells.Length; index++) { sheet.Cell(1, index + 1).Value = cells[index].Header; sheet.Cell(2, index + 1).Value = cells[index].Value; }
            workbook.SaveAs(stream);
        }
        stream.Position = 0;
        return stream;
    }

    private sealed record Fixture(ExcelImportService Service, InMemoryShipmentRepository Shipments, User Actor, Customer Customer, Product Product)
    {
        public static Fixture Create()
        {
            var users = new InMemoryUserRepository(); var customers = new InMemoryCustomerRepository(); var products = new InMemoryProductRepository();
            var prices = new InMemoryPriceRepository(); var rates = new InMemoryRateRepository(); var shipments = new InMemoryShipmentRepository();
            var batches = new InMemoryImportBatchRepository(); var audits = new InMemoryAuditRepository();
            var actor = new User { LoginId = "member", PasswordHash = "hash", Name = "박태원", Email = "ptw@switchkorea.com", Role = Role.Member };
            var customer = new Customer { Code = "C1", CompanyName = "거래처", Country = "KR", DefaultCurrency = Currency.KRW, OwnerUserId = actor.Id };
            var product = new Product { Code = "P1", Name = "스위치", ItemType = ItemType.Product };
            customers.AddAsync(customer).GetAwaiter().GetResult(); products.AddAsync(product).GetAwaiter().GetResult();
            var shipmentService = new ShipmentService(prices, rates, shipments, customers, audits);
            return new Fixture(new ExcelImportService(users, customers, products, prices, shipments, batches, shipmentService, TimeProvider.System), shipments, actor, customer, product);
        }
    }
}
