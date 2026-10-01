using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Shipments;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class ShipmentTests
{
    [Fact]
    public void Calculates_Supply_And_Krw_Amounts()
    {
        Assert.Equal(new SalesAmounts(200m, 200m), SalesCalculator.Calculate(2m, 100m, Currency.KRW, 1m));
        Assert.Equal(new SalesAmounts(30m, 40_500m), SalesCalculator.Calculate(3m, 10m, Currency.USD, 1_350m));
    }

    [Fact]
    public async Task Missing_Monthly_Rate_Rejects_Foreign_Currency_Shipment()
    {
        var fixture = await Fixture.CreateAsync(Currency.USD, includeRate: false);
        await Assert.ThrowsAsync<MissingMonthlyRateException>(() => fixture.Service.SaveAsync(fixture.Command("S-001"), fixture.Actor, CancellationToken.None));
    }

    [Fact]
    public async Task Duplicate_External_Number_Is_Rejected_Per_Customer()
    {
        var fixture = await Fixture.CreateAsync(Currency.KRW, includeRate: true);
        await fixture.Service.SaveAsync(fixture.Command("S-001"), fixture.Actor, CancellationToken.None);
        await Assert.ThrowsAsync<DuplicateShipmentNumberException>(() => fixture.Service.SaveAsync(fixture.Command("S-001"), fixture.Actor, CancellationToken.None));
    }

    [Fact]
    public async Task Only_Manager_Can_Recalculate_Confirmed_Shipments()
    {
        var fixture = await Fixture.CreateAsync(Currency.KRW, includeRate: true);
        await Assert.ThrowsAsync<ForbiddenException>(() => fixture.Service.RecalculateAsync([], fixture.Actor, "confirmed", CancellationToken.None));
    }

    private sealed record Fixture(ShipmentService Service, User Actor, Customer Customer, Product Product)
    {
        public ShipmentCreateCommand Command(string? number) => new(number, new DateOnly(2026, 10, 1), Customer.Id, Product.Id, 2m);

        public static async Task<Fixture> CreateAsync(Currency currency, bool includeRate)
        {
            var prices = new InMemoryPriceRepository();
            var rates = new InMemoryRateRepository();
            var shipments = new InMemoryShipmentRepository();
            var customers = new InMemoryCustomerRepository();
            var audits = new InMemoryAuditRepository();
            var actor = new User { LoginId = "member", PasswordHash = "hash", Name = "박태원", Email = "ptw@switchkorea.com", Role = Role.Member };
            var customer = new Customer { Code = "C1", CompanyName = "거래처", Country = "KR", DefaultCurrency = currency, OwnerUserId = actor.Id };
            var product = new Product { Code = "P1", Name = "스위치", ItemType = ItemType.Product };
            await customers.AddAsync(customer);
            await prices.AddAsync(new Price { CustomerId = customer.Id, ProductId = product.Id, Currency = currency, UnitPrice = 100m, ValidFrom = new DateOnly(2026, 1, 1) });
            if (includeRate && currency != Currency.KRW) await rates.SaveMonthlyAsync(new MonthlyRate { YearMonth = new YearMonth(2026, 10), Currency = currency, Rate = 1_350m, UpdatedBy = actor.Id }, 0);
            return new Fixture(new ShipmentService(prices, rates, shipments, customers, audits), actor, customer, product);
        }
    }
}
