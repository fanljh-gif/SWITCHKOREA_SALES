using System.Security.Cryptography;
using System.Text;
using SalesManagement.Core.Auth;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Shipments;

public sealed record ShipmentCreateCommand(string? ShipmentNumber, DateOnly ShipmentDate, Guid CustomerId, Guid ProductId, decimal Quantity);
public sealed record ShipmentSaveResult(Shipment Shipment, IReadOnlyList<Shipment> DuplicateCandidates);
public sealed record RecalculationResult(int AffectedCount, bool Applied, string ConfirmationToken);

public sealed class ShipmentService(IPriceRepository prices, IRateRepository rates, IShipmentRepository shipments, ICustomerRepository customers, IAuditRepository audits)
{
    public Task<IReadOnlyList<Shipment>> ListAsync(CancellationToken cancellationToken = default) => shipments.ListAsync(cancellationToken);

    public async Task<ShipmentSaveResult> SaveAsync(ShipmentCreateCommand command, User actor, CancellationToken cancellationToken = default)
    {
        var customer = await customers.GetAsync(command.CustomerId, cancellationToken) ?? throw new KeyNotFoundException("Customer not found.");
        AuthorizationService.EnsureCanEditCustomer(actor, customer);
        var existing = await shipments.ListAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(command.ShipmentNumber) && existing.Any(x => x.CustomerId == command.CustomerId && x.ShipmentNumber == command.ShipmentNumber)) throw new DuplicateShipmentNumberException();
        var price = await prices.FindEffectiveAsync(command.CustomerId, command.ProductId, command.ShipmentDate, cancellationToken) ?? throw new MissingEffectivePriceException();
        var yearMonth = new YearMonth(command.ShipmentDate.Year, command.ShipmentDate.Month);
        var rate = price.Currency == Currency.KRW ? 1m : (await rates.GetMonthlyAsync(yearMonth, price.Currency, cancellationToken))?.Rate ?? throw new MissingMonthlyRateException();
        var amounts = SalesCalculator.Calculate(command.Quantity, price.UnitPrice, price.Currency, rate);
        var shipment = await shipments.AddAsync(new Shipment { ShipmentNumber = command.ShipmentNumber, ShipmentDate = command.ShipmentDate, CustomerId = command.CustomerId, ProductId = command.ProductId, Quantity = command.Quantity, UnitPrice = price.UnitPrice, Currency = price.Currency, SupplyAmount = amounts.SupplyAmount, AppliedRate = rate, KrwAmount = amounts.KrwAmount, OwnerUserId = customer.OwnerUserId }, cancellationToken);
        var candidates = string.IsNullOrWhiteSpace(command.ShipmentNumber) ? existing.Where(x => x.ShipmentDate == command.ShipmentDate && x.CustomerId == command.CustomerId && x.ProductId == command.ProductId && x.Quantity == command.Quantity).ToArray() : [];
        return new ShipmentSaveResult(shipment, candidates);
    }

    public async Task<Shipment> CancelAsync(Guid id, User actor, long expectedVersion, CancellationToken cancellationToken = default)
    {
        var shipment = await shipments.GetAsync(id, cancellationToken) ?? throw new KeyNotFoundException();
        var customer = await customers.GetAsync(shipment.CustomerId, cancellationToken) ?? throw new KeyNotFoundException();
        AuthorizationService.EnsureCanEditCustomer(actor, customer);
        shipment.Status = ShipmentStatus.Cancelled;
        var updated = await shipments.UpdateAsync(shipment, expectedVersion, cancellationToken);
        await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = $"Shipment:{id}", Action = "Cancel", AfterJson = "{\"status\":\"Cancelled\"}" }, cancellationToken);
        return updated;
    }

    public async Task<RecalculationResult> RecalculateAsync(IReadOnlyCollection<Guid> shipmentIds, User actor, string? confirmationToken, CancellationToken cancellationToken = default)
    {
        AuthorizationService.EnsureManager(actor);
        var selected = new List<Shipment>();
        foreach (var id in shipmentIds)
            if (await shipments.GetAsync(id, cancellationToken) is { Status: ShipmentStatus.Confirmed } shipment) selected.Add(shipment);
        var requiredToken = BuildToken(selected.Select(x => x.Id));
        if (confirmationToken != requiredToken) return new RecalculationResult(selected.Count, false, requiredToken);
        foreach (var shipment in selected)
        {
            var price = await prices.FindEffectiveAsync(shipment.CustomerId, shipment.ProductId, shipment.ShipmentDate, cancellationToken) ?? throw new MissingEffectivePriceException();
            var month = new YearMonth(shipment.ShipmentDate.Year, shipment.ShipmentDate.Month);
            var rate = price.Currency == Currency.KRW ? 1m : (await rates.GetMonthlyAsync(month, price.Currency, cancellationToken))?.Rate ?? throw new MissingMonthlyRateException();
            var amounts = SalesCalculator.Calculate(shipment.Quantity, price.UnitPrice, price.Currency, rate);
            var version = shipment.Version;
            shipment.UnitPrice = price.UnitPrice; shipment.Currency = price.Currency; shipment.AppliedRate = rate; shipment.SupplyAmount = amounts.SupplyAmount; shipment.KrwAmount = amounts.KrwAmount;
            await shipments.UpdateAsync(shipment, version, cancellationToken);
            await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = $"Shipment:{shipment.Id}", Action = "Recalculate" }, cancellationToken);
        }
        return new RecalculationResult(selected.Count, true, requiredToken);
    }

    private static string BuildToken(IEnumerable<Guid> ids)
    {
        var value = string.Join('|', ids.Order());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];
    }
}
