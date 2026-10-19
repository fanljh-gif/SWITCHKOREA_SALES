using SalesManagement.Core.Models;

namespace SalesManagement.Setup;

public static class InitialAccountCatalog
{
    public static IReadOnlyList<InitialAccountDefinition> All { get; } =
    [
        new("jhlee", "이재학", "jhlee@switchkorea.com", Role.Manager),
        new("ptw0517", "박태원", "ptw0517@switchkorea.com", Role.Member),
        new("cmc", "최명철", "cmc@switchkorea.com", Role.Member),
        new("sylee", "이소연", "sylee@switchkorea.com", Role.Member),
        new("cslee", "이창선", "cslee@switchkorea.com", Role.Member),
    ];
}
