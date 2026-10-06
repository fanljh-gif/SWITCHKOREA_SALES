using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;

namespace SalesManagement.Core.Auth;

public static class AuthorizationService
{
    public static void EnsureActive(User actor)
    {
        if (actor.Status != EntityStatus.Active) throw new ForbiddenException("사용 중지된 계정입니다.");
    }

    public static void EnsureManager(User actor)
    {
        EnsureActive(actor);
        if (actor.Role != Role.Manager) throw new ForbiddenException();
    }

    public static void EnsureCanEditCustomer(User actor, Customer customer)
    {
        EnsureActive(actor);
        if (actor.Role != Role.Manager && customer.OwnerUserId != actor.Id) throw new ForbiddenException();
    }

    public static void EnsureCanEditNetworkCompany(User actor, NetworkCompany company, CompanyAssignment? currentAssignment)
    {
        EnsureActive(actor);
        if (actor.Role == Role.Manager) return;
        var ownerId = currentAssignment is { IsCurrent: true } ? currentAssignment.UserId : company.CurrentOwnerUserId;
        if (ownerId != actor.Id) throw new ForbiddenException();
    }
}
