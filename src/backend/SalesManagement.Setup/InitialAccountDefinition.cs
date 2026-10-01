using SalesManagement.Core.Models;

namespace SalesManagement.Setup;

public sealed record InitialAccountDefinition(string LoginId, string Name, string Email, Role Role);

public sealed record InitialCredential(string LoginId, string TemporaryPassword);

public sealed record SeedResult(IReadOnlyList<InitialCredential> Created, int ExistingCount);
