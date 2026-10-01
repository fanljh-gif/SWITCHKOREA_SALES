using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Prices;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class AuthAndMasterDataTests
{
    [Fact]
    public void Member_Cannot_Update_Other_Owners_Customer()
    {
        var actor = Member("member-a");
        var otherOwner = Member("member-b");
        var customer = new Customer
        {
            Code = "CUS-001",
            CompanyName = "거래처 A",
            Country = "KR",
            DefaultCurrency = Currency.KRW,
            OwnerUserId = otherOwner.Id,
        };

        Assert.Throws<ForbiddenException>(() => AuthorizationService.EnsureCanEditCustomer(actor, customer));
    }

    [Fact]
    public async Task Price_Rejects_Overlapping_Effective_Period()
    {
        var repository = new InMemoryPriceRepository();
        var actor = Manager();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        await repository.AddAsync(new Price
        {
            CustomerId = customerId,
            ProductId = productId,
            Currency = Currency.USD,
            UnitPrice = 10m,
            ValidFrom = new DateOnly(2026, 1, 1),
            ValidTo = new DateOnly(2026, 12, 31),
        });
        var service = new PriceService(repository);

        var command = new PriceSaveCommand(null, customerId, productId, Currency.USD, 12m, new DateOnly(2026, 10, 1), null);

        await Assert.ThrowsAsync<PricePeriodConflictException>(() => service.SaveAsync(command, actor, 0, CancellationToken.None));
    }

    private static User Manager() => new()
    {
        LoginId = "manager",
        PasswordHash = "hash",
        Name = "이재학",
        Email = "jhlee@switchkorea.com",
        Role = Role.Manager,
    };

    private static User Member(string loginId) => new()
    {
        LoginId = loginId,
        PasswordHash = "hash",
        Name = loginId,
        Email = $"{loginId}@switchkorea.com",
        Role = Role.Member,
    };
}
