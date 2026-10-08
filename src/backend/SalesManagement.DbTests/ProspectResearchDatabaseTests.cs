using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SalesManagement.Core.Models;
using SalesManagement.Postgres;
using SalesManagement.Postgres.Repositories;

namespace SalesManagement.DbTests;

public sealed class ProspectResearchDatabaseTests
{
    [Fact]
    public async Task Provider_Result_And_Conversion_Are_Unique()
    {
        await using var fixture = await Fixture.CreateAsync();
        var profile = await fixture.AddProfile(); var run = await fixture.AddRun(profile.Id);
        fixture.Context.ProspectCandidates.AddRange(Candidate(run.Id, "same"), Candidate(run.Id, "same"));
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());

        fixture.Context.ChangeTracker.Clear();
        var candidate = Candidate(run.Id, "one"); fixture.Context.ProspectCandidates.Add(candidate); await fixture.Context.SaveChangesAsync();
        var owner = await fixture.AddUser(); var network = new NetworkCompany { CompanyName = "Acme", Country = "US", Industry = "전자", LifecycleType = LifecycleType.Prospect, CurrentOwnerUserId = owner.Id }; fixture.Context.NetworkCompanies.Add(network); await fixture.Context.SaveChangesAsync();
        fixture.Context.ProspectConversions.AddRange(new ProspectConversion { ProspectCandidateId = candidate.Id, NetworkCompanyId = network.Id, ApprovedByUserId = owner.Id }, new ProspectConversion { ProspectCandidateId = candidate.Id, NetworkCompanyId = network.Id, ApprovedByUserId = owner.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Research_Repository_Roundtrips_Json_Collections_And_Optimistic_Version()
    {
        await using var fixture = await Fixture.CreateAsync(); var profile = await fixture.AddProfile(); var run = await fixture.AddRun(profile.Id);
        var repository = new ProspectCandidateRepository(fixture.Context);
        var candidate = await repository.AddAsync(Candidate(run.Id, "provider-1", ["rocker switch"]));
        Assert.Equal(["rocker switch"], (await repository.GetAsync(candidate.Id))!.Products);
        candidate.CompanyName = "Changed"; await repository.UpdateAsync(candidate, candidate.Version);
        await Assert.ThrowsAsync<SalesManagement.Core.Exceptions.ConcurrencyException>(() => repository.UpdateAsync(candidate, 1));
    }

    [Fact]
    public async Task Evidence_Requires_Source_And_Activity_Has_Restrict_Delete_Foreign_Keys()
    {
        await using var fixture = await Fixture.CreateAsync();
        var designModel = fixture.Context.GetService<IDesignTimeModel>().Model;
        var evidence = designModel.FindEntityType(typeof(ResearchEvidence))!;
        Assert.Contains(evidence.GetCheckConstraints(), x => x.Name == "CK_research_evidence_source_observed");
        var activity = designModel.FindEntityType(typeof(ProspectActivity))!;
        Assert.All(activity.GetForeignKeys(), x => Assert.Equal(DeleteBehavior.Restrict, x.DeleteBehavior));
    }

    [Fact]
    public async Task Json_String_Collections_Have_Value_Comparers()
    {
        await using var fixture = await Fixture.CreateAsync();
        var designModel = fixture.Context.GetService<IDesignTimeModel>().Model;
        var properties = new[]
        {
            (typeof(ResearchTargetProfile), nameof(ResearchTargetProfile.Regions)),
            (typeof(ResearchTargetProfile), nameof(ResearchTargetProfile.Industries)),
            (typeof(ResearchTargetProfile), nameof(ResearchTargetProfile.IncludeKeywords)),
            (typeof(ResearchTargetProfile), nameof(ResearchTargetProfile.ExcludeKeywords)),
            (typeof(ProspectCandidate), nameof(ProspectCandidate.Products)),
            (typeof(ProspectCandidate), nameof(ProspectCandidate.Applications)),
            (typeof(ProspectAssessment), nameof(ProspectAssessment.AiRisks)),
            (typeof(ProspectDuplicateMatch), nameof(ProspectDuplicateMatch.Reasons))
        };

        foreach (var (entityType, propertyName) in properties)
        {
            var property = designModel.FindEntityType(entityType)!.FindProperty(propertyName)!;
            var comparer = property.GetValueComparer();
            var original = new List<string> { "switch", "relay" };
            var equivalent = new List<string> { "switch", "relay" };
            var snapshot = Assert.IsType<List<string>>(comparer.Snapshot(original));
            Assert.True(comparer.Equals(original, equivalent));
            Assert.NotSame(original, snapshot);
            Assert.Equal(original, snapshot);
        }
    }

    private static ProspectCandidate Candidate(Guid runId, string source, List<string>? products = null) => new() { ResearchRunId = runId, ProviderResultId = source, CompanyName = "Acme", NormalizedCompanyName = "acme", Country = "US", Region = "미국", Industry = "전자", Domain = "acme.example", NormalizedDomain = "acme.example", Website = "https://acme.example", Products = products ?? [] };

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection; public SalesDbContext Context { get; }
        private Fixture(SqliteConnection connection, SalesDbContext context) { _connection = connection; Context = context; }
        public static async Task<Fixture> CreateAsync() { var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var context = new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connection).Options); await context.Database.EnsureCreatedAsync(); return new(connection, context); }
        public async Task<ResearchTargetProfile> AddProfile() { var value = new ResearchTargetProfile { Name = Guid.NewGuid().ToString("N") }; Context.ResearchTargetProfiles.Add(value); await Context.SaveChangesAsync(); return value; }
        public async Task<ResearchRun> AddRun(Guid profileId) { var value = new ResearchRun { ResearchTargetProfileId = profileId, Kind = ResearchRunKind.Manual }; Context.ResearchRuns.Add(value); await Context.SaveChangesAsync(); return value; }
        public async Task<User> AddUser() { var value = new User { LoginId = Guid.NewGuid().ToString("N"), PasswordHash = "hash", Name = "owner", Email = "owner@switchkorea.com" }; Context.Users.Add(value); await Context.SaveChangesAsync(); return value; }
        public async ValueTask DisposeAsync() { await Context.DisposeAsync(); await _connection.DisposeAsync(); }
    }
}
