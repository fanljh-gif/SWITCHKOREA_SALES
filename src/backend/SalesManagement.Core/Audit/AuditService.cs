using System.Text.Json;
using System.Text.Encodings.Web;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Audit;

public sealed class AuditService(IAuditRepository audits)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
        { "Name", "Email", "Role", "Status", "OwnerUserId", "Rate", "TargetKrw", "UnitPrice", "Currency", "ShipmentDate", "Quantity" };

    public Task<IReadOnlyList<AuditEntry>> ListAsync(string target, CancellationToken ct) => audits.ListByTargetAsync(target, ct);

    public Task<AuditEntry> RecordAsync(User actor, string target, string action,
        IReadOnlyDictionary<string, object?>? before, IReadOnlyDictionary<string, object?>? after, CancellationToken ct) =>
        audits.AddAsync(new AuditEntry
        {
            ActorUserId = actor.Id, Target = target, Action = action,
            BeforeJson = SerializeAllowed(before), AfterJson = SerializeAllowed(after)
        }, ct);

    private static string? SerializeAllowed(IReadOnlyDictionary<string, object?>? values)
    {
        if (values is null) return null;
        return JsonSerializer.Serialize(values.Where(x => AllowedFields.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value), JsonOptions);
    }
}
