using SalesManagement.Core.Models;
using SalesManagement.Core.Research;
using SalesManagement.InMemory.Repositories;
using SalesManagement.Api.Research;
using System.Net;
using System.Text;

namespace SalesManagement.Tests;

public sealed class ResearchProviderTests
{
    [Theory]
    [InlineData(69, true, null)]
    [InlineData(70, true, "70")]
    [InlineData(90, true, "90")]
    [InlineData(100, false, "100")]
    public void Scheduled_Run_Respects_Monthly_Budget(int percent, bool allowed, string? warning)
    {
        var decision = new UsageBudgetService().CanStart(ResearchRunKind.Scheduled, new UsageSnapshot(percent, 100), User("manager", Role.Manager));
        Assert.Equal(allowed, decision.Allowed); Assert.Equal(warning, decision.WarningCode);
    }

    [Fact]
    public void Only_Manager_Can_Override_Full_Budget_With_Manual_Run()
    {
        var service = new UsageBudgetService(); var usage = new UsageSnapshot(100, 100);
        Assert.True(service.CanStart(ResearchRunKind.Manual, usage, User("manager", Role.Manager)).Allowed);
        Assert.False(service.CanStart(ResearchRunKind.Manual, usage, User("member", Role.Member)).Allowed);
    }

    [Fact]
    public async Task OpenAi_Request_Contains_Only_Public_Target_Criteria_And_Parses_Sourced_Results()
    {
        string? payload = null;
        var handler = new RecordingHandler(request =>
        {
            payload = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            var json = """{"output":[{"type":"message","content":[{"type":"output_text","text":"{\"companies\":[{\"sourceId\":\"s1\",\"companyName\":\"Acme\",\"country\":\"US\",\"website\":\"https://acme.example\",\"industry\":\"electronics\",\"summary\":\"public\",\"evidenceUrl\":\"https://acme.example/about\",\"observedDate\":\"2026-10-08\"}]}"}]}]}""";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        });
        var provider = new OpenAiProspectResearchProvider(new HttpClient(handler), Microsoft.Extensions.Options.Options.Create(new ResearchProviderOptions { ApiKey = "secret", Model = "test-model" }));
        var results = new List<ResearchProviderResult>();
        await foreach (var item in provider.SearchAsync(new ResearchQuery(Guid.NewGuid(), ["미국"], ["전자"], ["switch"], 3), default)) results.Add(item);
        Assert.Single(results); Assert.Equal("https://acme.example/about", results[0].EvidenceUrl);
        Assert.DoesNotContain("sales", payload!, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("price", payload!, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("contact", payload!, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("secret", payload!);
    }

    private static User User(string id, Role role) => new() { LoginId = id, PasswordHash = "hash", Name = id, Email = $"{id}@switchkorea.com", Role = role };
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(response(request));
    }
    [Fact]
    public async Task Provider_Partial_Failure_Preserves_Completed_Candidates_And_Can_Retry()
    {
        var runs = new InMemoryResearchRunRepository(); var candidates = new InMemoryProspectCandidateRepository(); var evidence = new InMemoryResearchEvidenceRepository(); var duplicates = new InMemoryDuplicateMatchRepository();
        var provider = new SequenceProvider(failAfterFirst: true);
        var duplicateService = new DuplicateDetectionService(candidates, new InMemoryNetworkCompanyRepository(), new InMemoryCustomerRepository());
        var service = new ResearchRunService(runs, candidates, evidence, duplicates, provider, duplicateService);
        var profile = new ResearchTargetProfile { Name = "Global", Regions = ["한국"], Industries = ["전자"] };
        var run = await service.StartAsync(ResearchRunKind.Manual, profile);

        var partial = await service.ProcessAsync(run.Id, CancellationToken.None);

        Assert.Equal(ResearchRunStatus.Partial, partial.Status);
        Assert.Single(await candidates.ListAsync());
        provider.FailAfterFirst = false;
        var completed = await service.RetryFailedAsync(run.Id, CancellationToken.None);
        Assert.Equal(ResearchRunStatus.Completed, completed.Status);
        Assert.Equal(2, (await candidates.ListAsync()).Count);
    }

    [Fact]
    public async Task Same_Provider_Result_Id_Is_Idempotent()
    {
        var runs = new InMemoryResearchRunRepository(); var candidates = new InMemoryProspectCandidateRepository(); var evidence = new InMemoryResearchEvidenceRepository(); var duplicates = new InMemoryDuplicateMatchRepository();
        var provider = new SequenceProvider();
        var service = new ResearchRunService(runs, candidates, evidence, duplicates, provider, new DuplicateDetectionService(candidates, new InMemoryNetworkCompanyRepository(), new InMemoryCustomerRepository()));
        var profile = new ResearchTargetProfile { Name = "Global" };

        await service.ProcessAsync((await service.StartAsync(ResearchRunKind.Scheduled, profile)).Id, CancellationToken.None);
        await service.ProcessAsync((await service.StartAsync(ResearchRunKind.Scheduled, profile)).Id, CancellationToken.None);

        Assert.Equal(2, (await candidates.ListAsync()).Count);
    }

    private sealed class SequenceProvider(bool failAfterFirst = false) : IProspectResearchProvider
    {
        public bool FailAfterFirst { get; set; } = failAfterFirst;
        public async IAsyncEnumerable<ResearchProviderResult> SearchAsync(ResearchQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new("provider-1", "Hanul", "KR", "https://hanul.example", "자동차 전장", "Console modules", "https://hanul.example/products", new DateOnly(2026, 10, 8));
            await Task.Yield();
            if (FailAfterFirst) throw new HttpRequestException("provider timeout");
            yield return new("provider-2", "Vina", "VN", "https://vina.example", "가전", "Appliance modules", "https://vina.example/catalog", new DateOnly(2026, 10, 8));
        }
    }
}
