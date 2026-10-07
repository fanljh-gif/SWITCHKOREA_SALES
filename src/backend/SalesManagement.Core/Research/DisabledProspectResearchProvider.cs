namespace SalesManagement.Core.Research;

public sealed class DisabledProspectResearchProvider : IProspectResearchProvider
{
    public async IAsyncEnumerable<ResearchProviderResult> SearchAsync(ResearchQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.CompletedTask;
        yield break;
    }
}
