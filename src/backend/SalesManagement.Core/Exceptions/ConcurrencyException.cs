namespace SalesManagement.Core.Exceptions;

public sealed class ConcurrencyException(Guid entityId, long expectedVersion, long actualVersion)
    : Exception($"Entity {entityId} version mismatch. Expected {expectedVersion}, actual {actualVersion}.")
{
    public Guid EntityId { get; } = entityId;
    public long ExpectedVersion { get; } = expectedVersion;
    public long ActualVersion { get; } = actualVersion;
}
