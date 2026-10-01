using SalesManagement.Core.Dashboard;
using SalesManagement.Core.Models;
using SalesManagement.Core.Rates;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class RateTargetDashboardTests
{
    [Fact]
    public async Task Zero_Target_Returns_Null_Achievement_Rate()
    {
        var shipments = new InMemoryShipmentRepository();
        var targets = new InMemoryTargetRepository();
        var service = new DashboardService(shipments, targets);
        var ownerId = Guid.NewGuid();
        await targets.AddAsync(new SalesTarget { YearMonth = new YearMonth(2026, 10), UserId = ownerId, TargetKrw = 0m });

        var summary = await service.QueryAsync(new DashboardQuery(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), ownerId), CancellationToken.None);

        Assert.Null(summary.AchievementRate);
    }

    [Fact]
    public async Task Daily_Rate_Failure_Keeps_Last_Successful_Value()
    {
        var repository = new InMemoryRateRepository();
        await repository.AddDailyAsync(new DailyRate
        {
            Date = new DateOnly(2026, 9, 30), Currency = Currency.USD, Rate = 1_350m,
            FetchedAt = DateTimeOffset.UtcNow, FetchStatus = RateFetchStatus.Success
        });
        var collector = new DailyRateCollector(repository, new FailingRateProvider(), TimeProvider.System);

        var rate = await collector.CollectAsync(new DateOnly(2026, 10, 1), Currency.USD, CancellationToken.None);

        Assert.Equal(1_350m, rate.Rate);
        Assert.Equal(RateFetchStatus.Failed, rate.FetchStatus);
    }

    [Fact]
    public async Task Filters_By_Date_Owner_Customer_And_Product()
    {
        var shipments = new InMemoryShipmentRepository();
        var targets = new InMemoryTargetRepository();
        var ownerId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await shipments.AddAsync(NewShipment(ownerId, customerId, productId, Currency.USD, 30m, 40_500m));
        await shipments.AddAsync(NewShipment(Guid.NewGuid(), customerId, productId, Currency.KRW, 10_000m, 10_000m));

        var summary = await new DashboardService(shipments, targets).QueryAsync(
            new DashboardQuery(new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), ownerId, customerId, productId), CancellationToken.None);

        Assert.Equal(40_500m, summary.KrwTotal);
        Assert.Single(summary.ByOwner);
        Assert.Single(summary.ByCustomer);
        Assert.Single(summary.ByProduct);
        Assert.Equal(100m, summary.ByCurrency.Single().SharePercent);
    }

    private static Shipment NewShipment(Guid ownerId, Guid customerId, Guid productId, Currency currency, decimal supply, decimal krw) => new()
    {
        ShipmentDate = new DateOnly(2026, 10, 1), CustomerId = customerId, ProductId = productId,
        Quantity = 1m, UnitPrice = supply, Currency = currency, SupplyAmount = supply,
        AppliedRate = currency == Currency.KRW ? 1m : krw / supply, KrwAmount = krw, OwnerUserId = ownerId
    };

    private sealed class FailingRateProvider : IDailyRateProvider
    {
        public Task<decimal> FetchAsync(DateOnly date, Currency currency, CancellationToken cancellationToken) =>
            Task.FromException<decimal>(new HttpRequestException("provider unavailable"));
    }
}
