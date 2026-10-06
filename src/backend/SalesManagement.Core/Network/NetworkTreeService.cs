using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;

namespace SalesManagement.Core.Network;

public sealed record NetworkTreeQuery(Guid? RootCompanyId = null, LifecycleType? LifecycleType = null,
    Guid? OwnerId = null, EntityStatus? Status = null, string? Search = null, bool OemOnly = false);
public sealed record NetworkTreeNode(NetworkCompany Company, int Depth, IReadOnlyList<Guid> PathCompanyIds, IReadOnlyList<NetworkTreeNode> Children);

public sealed class NetworkTreeService(INetworkCompanyRepository companies, ICompanyRelationshipRepository relationships)
{
    public async Task<IReadOnlyList<NetworkTreeNode>> BuildTreeAsync(NetworkTreeQuery query, CancellationToken ct = default)
    {
        var allCompanies = await companies.ListAsync(ct);
        var byId = allCompanies.ToDictionary(x => x.Id);
        var today = SeoulBusinessDate.Today();
        var edges = (await relationships.ListAsync(ct)).Where(x => x.Status == EntityStatus.Active && x.ValidFrom <= today && (x.ValidTo is null || x.ValidTo >= today)).ToArray();
        var roots = (query.RootCompanyId.HasValue
            ? allCompanies.Where(x => x.Id == query.RootCompanyId.Value)
            : allCompanies.Where(x => x.IsOem)).ToArray();

        var mutableRoots = new List<MutableNode>();
        var allNodes = new List<MutableNode>();
        var pending = new Stack<MutableNode>();
        foreach (var root in roots)
        {
            var node = new MutableNode(root, 0, [root.Id]);
            mutableRoots.Add(node); allNodes.Add(node); pending.Push(node);
        }
        while (pending.TryPop(out var node))
        {
            ct.ThrowIfCancellationRequested();
            foreach (var edge in edges.Where(x => x.UpstreamCompanyId == node.Company.Id && !node.Path.Contains(x.DownstreamCompanyId)))
            {
                if (!byId.TryGetValue(edge.DownstreamCompanyId, out var childCompany)) continue;
                var child = new MutableNode(childCompany, node.Depth + 1, [.. node.Path, childCompany.Id]);
                node.Children.Add(child); allNodes.Add(child); pending.Push(child);
            }
        }

        foreach (var node in allNodes.OrderByDescending(x => x.Depth)) node.Keep = Matches(node.Company, query) || node.Children.Any(x => x.Keep);
        mutableRoots.RemoveAll(x => !x.Keep);
        var materialized = new Dictionary<MutableNode, NetworkTreeNode>();
        foreach (var node in allNodes.OrderByDescending(x => x.Depth))
            if (node.Keep) materialized[node] = new NetworkTreeNode(node.Company, node.Depth, node.Path, node.Children.Where(x => x.Keep).Select(x => materialized[x]).ToArray());
        return mutableRoots.Select(x => materialized[x]).ToArray();
    }

    private static bool Matches(NetworkCompany company, NetworkTreeQuery query)
    {
        return (!query.OemOnly || company.IsOem) &&
            (!query.LifecycleType.HasValue || !company.IsOem && company.LifecycleType == query.LifecycleType) &&
            (!query.OwnerId.HasValue || company.CurrentOwnerUserId == query.OwnerId) &&
            (!query.Status.HasValue || company.Status == query.Status) &&
            (string.IsNullOrWhiteSpace(query.Search) || $"{company.CompanyName} {company.Country} {company.Industry}".Contains(query.Search, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class MutableNode(NetworkCompany company, int depth, IReadOnlyList<Guid> path)
    {
        public NetworkCompany Company { get; } = company;
        public int Depth { get; } = depth;
        public IReadOnlyList<Guid> Path { get; } = path;
        public List<MutableNode> Children { get; } = [];
        public bool Keep { get; set; }
    }
}
