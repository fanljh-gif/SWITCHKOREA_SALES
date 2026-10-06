using Microsoft.EntityFrameworkCore;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres;

public sealed class NetworkCustomerBackfill(SalesDbContext db)
{
    public async Task<int> ApplyAsync(CancellationToken ct = default)
    {
        var customers = await db.Customers.AsNoTracking().OrderBy(x => x.Id).ToArrayAsync(ct);
        var companyByCustomerId = (await db.NetworkCompanies.AsNoTracking().Where(x => x.CustomerId != null)
            .Select(x => new { CustomerId = x.CustomerId!.Value, x.Id }).ToArrayAsync(ct)).ToDictionary(x => x.CustomerId, x => x.Id);
        var assignedCompanyIds = (await db.CompanyAssignments.AsNoTracking().Where(x => x.IsCurrent)
            .Select(x => x.CompanyId).ToArrayAsync(ct)).ToHashSet();
        var created = 0;
        foreach (var customer in customers)
        {
            if (!companyByCustomerId.TryGetValue(customer.Id, out var companyId))
            {
                companyId = customer.Id;
                db.NetworkCompanies.Add(new NetworkCompany
                {
                    Id = customer.Id, CreatedAt = customer.CreatedAt, UpdatedAt = customer.UpdatedAt,
                    CompanyName = customer.CompanyName, Country = customer.Country, Industry = "미분류",
                    LifecycleType = LifecycleType.Customer, IsOem = false, CustomerId = customer.Id,
                    CurrentOwnerUserId = customer.OwnerUserId, Status = customer.Status,
                });
                companyByCustomerId.Add(customer.Id, companyId); created++;
            }
            if (!assignedCompanyIds.Contains(companyId))
            {
                db.CompanyAssignments.Add(new CompanyAssignment
                {
                    Id = companyId, CompanyId = companyId, UserId = customer.OwnerUserId,
                    ValidFrom = SalesManagement.Core.Network.SeoulBusinessDate.FromUtc(customer.CreatedAt), IsCurrent = true,
                    CreatedAt = customer.CreatedAt, UpdatedAt = customer.UpdatedAt,
                });
                assignedCompanyIds.Add(companyId);
            }
        }
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
        return created;
    }
}
