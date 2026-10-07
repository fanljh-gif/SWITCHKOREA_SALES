using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Network;
using SalesManagement.Core.Research;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class ResearchAuthorizationTests
{
    [Fact]
    public async Task Member_Cannot_Reassign_Another_Owners_Candidate_Or_Approve()
    {
        var context = await CreateContext();

        await Assert.ThrowsAsync<ForbiddenException>(() => context.Service.AssignAsync(context.Candidate.Id, context.Other.Id, context.Candidate.Version, context.Other));
        await Assert.ThrowsAsync<ForbiddenException>(() => context.Service.ApproveAndConvertAsync(context.Candidate.Id, context.Candidate.Version, context.Owner));
    }

    [Fact]
    public async Task Manager_Approval_Requires_Evidence_And_Is_Idempotent()
    {
        var context = await CreateContext(addEvidence: false);
        await Assert.ThrowsAsync<NetworkValidationException>(() => context.Service.ApproveAndConvertAsync(context.Candidate.Id, context.Candidate.Version, context.Manager));
        await context.Evidence.AddAsync(new ResearchEvidence { ProspectCandidateId = context.Candidate.Id, SourceUrl = "https://example.com/company", Title = "Company", ObservedDate = new DateOnly(2026, 10, 8), Summary = "Public company page", SourceHash = "hash" });

        var first = await context.Service.ApproveAndConvertAsync(context.Candidate.Id, context.Candidate.Version, context.Manager);
        var second = await context.Service.ApproveAndConvertAsync(context.Candidate.Id, first.Candidate.Version, context.Manager);

        Assert.Equal(ProspectStatus.Converted, first.Candidate.Status);
        Assert.Equal(LifecycleType.Prospect, first.NetworkCompany.LifecycleType);
        Assert.Equal(first.NetworkCompany.Id, second.NetworkCompany.Id);
        Assert.Single(await context.Conversions.ListAsync());
    }

    private static async Task<TestContext> CreateContext(bool addEvidence = true)
    {
        var candidates = new InMemoryProspectCandidateRepository(); var evidence = new InMemoryResearchEvidenceRepository();
        var duplicates = new InMemoryDuplicateMatchRepository(); var activities = new InMemoryProspectActivityRepository(); var conversions = new InMemoryProspectConversionRepository();
        var networks = new InMemoryNetworkCompanyRepository(); var customers = new InMemoryCustomerRepository(); var assignments = new InMemoryCompanyAssignmentRepository(); var users = new InMemoryUserRepository(); var audits = new InMemoryAuditRepository();
        var manager = User("manager", Role.Manager); var owner = User("owner", Role.Member); var other = User("other", Role.Member);
        await users.AddAsync(manager); await users.AddAsync(owner); await users.AddAsync(other);
        var candidate = await candidates.AddAsync(new ProspectCandidate { ResearchRunId = Guid.NewGuid(), ProviderResultId = "provider-1", CompanyName = "Hanul Mobility", NormalizedCompanyName = "hanulmobility", Country = "KR", Region = "한국", Industry = "자동차 전장", Domain = "hanul.example", NormalizedDomain = "hanul.example", Website = "https://hanul.example", OwnerUserId = owner.Id, Status = ProspectStatus.ReviewPending });
        if (addEvidence) await evidence.AddAsync(new ResearchEvidence { ProspectCandidateId = candidate.Id, SourceUrl = "https://hanul.example/products", Title = "Products", ObservedDate = new DateOnly(2026, 10, 8), Summary = "Switch product", SourceHash = "hash" });
        var networkService = new NetworkCompanyService(networks, customers, assignments, users, audits, new InMemoryTransactionRunner(networks, customers, assignments, audits));
        var service = new ProspectReviewService(candidates, evidence, duplicates, activities, conversions, networkService);
        return new(service, candidate, manager, owner, other, evidence, conversions);
    }

    private static User User(string id, Role role) => new() { LoginId = id, PasswordHash = "hash", Name = id, Email = $"{id}@switchkorea.com", Role = role };
    private sealed record TestContext(ProspectReviewService Service, ProspectCandidate Candidate, User Manager, User Owner, User Other, InMemoryResearchEvidenceRepository Evidence, InMemoryProspectConversionRepository Conversions);
}
