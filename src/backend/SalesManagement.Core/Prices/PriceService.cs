using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Prices;

public sealed record PriceSaveCommand(Guid? Id, Guid CustomerId, Guid ProductId, Currency Currency, decimal UnitPrice, DateOnly ValidFrom, DateOnly? ValidTo);

public sealed class PriceService(IPriceRepository prices, ICustomerRepository? customers = null)
{
    public Task<IReadOnlyList<Price>> ListAsync(CancellationToken cancellationToken = default) => prices.ListAsync(cancellationToken);

    public async Task<Price> SaveAsync(PriceSaveCommand command, User actor, long expectedVersion, CancellationToken cancellationToken = default)
    {
        AuthorizationService.EnsureActive(actor);
        if (actor.Role != Role.Manager)
        {
            var customer = customers is null ? null : await customers.GetAsync(command.CustomerId, cancellationToken);
            if (customer is null) throw new ForbiddenException();
            AuthorizationService.EnsureCanEditCustomer(actor, customer);
        }
        if (command.ValidTo < command.ValidFrom) throw new ArgumentException("종료일은 시작일보다 빠를 수 없습니다.");
        var all = await prices.ListAsync(cancellationToken);
        var overlaps = all.Any(x => x.Id != command.Id && x.CustomerId == command.CustomerId && x.ProductId == command.ProductId && x.ValidFrom <= (command.ValidTo ?? DateOnly.MaxValue) && command.ValidFrom <= (x.ValidTo ?? DateOnly.MaxValue));
        if (overlaps) throw new PricePeriodConflictException();
        if (command.Id is null) return await prices.AddAsync(new Price { CustomerId = command.CustomerId, ProductId = command.ProductId, Currency = command.Currency, UnitPrice = command.UnitPrice, ValidFrom = command.ValidFrom, ValidTo = command.ValidTo }, cancellationToken);
        var price = await prices.GetAsync(command.Id.Value, cancellationToken) ?? throw new KeyNotFoundException();
        price.CustomerId = command.CustomerId; price.ProductId = command.ProductId; price.Currency = command.Currency; price.UnitPrice = command.UnitPrice; price.ValidFrom = command.ValidFrom; price.ValidTo = command.ValidTo;
        return await prices.UpdateAsync(price, expectedVersion, cancellationToken);
    }
}
