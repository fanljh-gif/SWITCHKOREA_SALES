using SalesManagement.Core.Auth;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Customers;

public sealed record CustomerSaveCommand(Guid? Id, string Code, string CompanyName, string Country, Currency DefaultCurrency, Guid OwnerUserId, EntityStatus Status);

public sealed class CustomerService(ICustomerRepository customers)
{
    public Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken = default) => customers.ListAsync(cancellationToken);

    public async Task<Customer> SaveAsync(CustomerSaveCommand command, User actor, long expectedVersion, CancellationToken cancellationToken = default)
    {
        if (command.Id is null)
        {
            AuthorizationService.EnsureActive(actor);
            if (actor.Role != Role.Manager && command.OwnerUserId != actor.Id) throw new SalesManagement.Core.Exceptions.ForbiddenException();
            return await customers.AddAsync(new Customer { Code = command.Code, CompanyName = command.CompanyName, Country = command.Country, DefaultCurrency = command.DefaultCurrency, OwnerUserId = command.OwnerUserId, Status = command.Status }, cancellationToken);
        }
        var customer = await customers.GetAsync(command.Id.Value, cancellationToken) ?? throw new KeyNotFoundException();
        AuthorizationService.EnsureCanEditCustomer(actor, customer);
        customer.Code = command.Code; customer.CompanyName = command.CompanyName; customer.Country = command.Country; customer.DefaultCurrency = command.DefaultCurrency; customer.Status = command.Status;
        if (actor.Role == Role.Manager) customer.OwnerUserId = command.OwnerUserId;
        return await customers.UpdateAsync(customer, expectedVersion, cancellationToken);
    }
}
