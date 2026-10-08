using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesManagement.Core.Models;

namespace SalesManagement.Postgres.Configurations;

internal static class ResearchJson
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private static readonly ValueComparer<List<string>> StringListComparer = new(
        (left, right) => left != null && right != null && left.SequenceEqual(right),
        values => values.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
        values => values.ToList());

    public static string Write(List<string> value) => JsonSerializer.Serialize(value, Options);
    public static List<string> Read(string value) => JsonSerializer.Deserialize<List<string>>(value, Options) ?? [];
    public static PropertyBuilder<List<string>> StringList(PropertyBuilder<List<string>> property)
    {
        var configured = property.HasConversion(v => Write(v), v => Read(v)).HasColumnType("jsonb");
        configured.Metadata.SetValueComparer(StringListComparer);
        return configured;
    }
}

public sealed class ResearchTargetProfileConfiguration : IEntityTypeConfiguration<ResearchTargetProfile>
{
    public void Configure(EntityTypeBuilder<ResearchTargetProfile> b) { b.ConfigureEntity("research_target_profiles"); b.Property(x => x.Name).HasMaxLength(150).IsRequired(); ResearchJson.StringList(b.Property(x => x.Regions)); ResearchJson.StringList(b.Property(x => x.Industries)); ResearchJson.StringList(b.Property(x => x.IncludeKeywords)); ResearchJson.StringList(b.Property(x => x.ExcludeKeywords)); b.HasIndex(x => x.Name).IsUnique(); }
}
public sealed class ResearchRunConfiguration : IEntityTypeConfiguration<ResearchRun>
{
    public void Configure(EntityTypeBuilder<ResearchRun> b) { b.ConfigureEntity("research_runs"); b.Property(x => x.Kind).HasConversion<string>(); b.Property(x => x.Status).HasConversion<string>(); b.Property(x => x.CostKrw).HasPrecision(18, 2); b.Property(x => x.ErrorSummary).HasMaxLength(2000); b.Property(x => x.InputHash).HasMaxLength(128); b.HasOne<ResearchTargetProfile>().WithMany().HasForeignKey(x => x.ResearchTargetProfileId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.InputHash).IsUnique().HasFilter("\"InputHash\" IS NOT NULL"); }
}
public sealed class ProspectCandidateConfiguration : IEntityTypeConfiguration<ProspectCandidate>
{
    public void Configure(EntityTypeBuilder<ProspectCandidate> b) { b.ConfigureEntity("prospect_candidates"); b.Property(x => x.ProviderResultId).HasMaxLength(300).IsRequired(); b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired(); b.Property(x => x.NormalizedCompanyName).HasMaxLength(200).IsRequired(); b.Property(x => x.Country).HasMaxLength(100).IsRequired(); b.Property(x => x.Region).HasMaxLength(100).IsRequired(); b.Property(x => x.Industry).HasMaxLength(150).IsRequired(); b.Property(x => x.Domain).HasMaxLength(253).IsRequired(); b.Property(x => x.NormalizedDomain).HasMaxLength(253).IsRequired(); b.Property(x => x.Website).HasMaxLength(2048).IsRequired(); b.Property(x => x.Status).HasConversion<string>(); ResearchJson.StringList(b.Property(x => x.Products)); ResearchJson.StringList(b.Property(x => x.Applications)); b.HasOne<ResearchRun>().WithMany().HasForeignKey(x => x.ResearchRunId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict); b.HasOne<NetworkCompany>().WithMany().HasForeignKey(x => x.NetworkCompanyId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.ProviderResultId).IsUnique(); b.HasIndex(x => x.NormalizedDomain); b.HasIndex(x => new { x.Country, x.NormalizedCompanyName }); }
}
public sealed class ResearchEvidenceConfiguration : IEntityTypeConfiguration<ResearchEvidence>
{
    public void Configure(EntityTypeBuilder<ResearchEvidence> b) { b.ConfigureEntity("research_evidence"); b.Property(x => x.SourceUrl).HasMaxLength(2048).IsRequired(); b.Property(x => x.Title).HasMaxLength(500).IsRequired(); b.Property(x => x.Summary).HasMaxLength(4000).IsRequired(); b.Property(x => x.Confidence).HasConversion<string>(); b.Property(x => x.SourceHash).HasMaxLength(300).IsRequired(); b.HasOne<ProspectCandidate>().WithMany().HasForeignKey(x => x.ProspectCandidateId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.SourceHash).IsUnique(); b.ToTable(t => t.HasCheckConstraint("CK_research_evidence_source_observed", "length(\"SourceUrl\") > 0 AND \"ObservedDate\" > '0001-01-01'")); }
}
public sealed class ProspectAssessmentConfiguration : IEntityTypeConfiguration<ProspectAssessment>
{
    public void Configure(EntityTypeBuilder<ProspectAssessment> b) { b.ConfigureEntity("prospect_assessments"); b.Property(x => x.Priority).HasConversion<string>(); b.Property(x => x.RuleVersion).HasMaxLength(100).IsRequired(); b.Property(x => x.AiSummary).HasMaxLength(4000).IsRequired(); b.Property(x => x.AiRecommendation).HasMaxLength(2000).IsRequired(); b.Property(x => x.AiModel).HasMaxLength(100).IsRequired(); ResearchJson.StringList(b.Property(x => x.AiRisks)); b.HasOne<ProspectCandidate>().WithMany().HasForeignKey(x => x.ProspectCandidateId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.ProspectCandidateId, x.AssessedAt }); }
}
public sealed class ProspectDuplicateMatchConfiguration : IEntityTypeConfiguration<ProspectDuplicateMatch>
{
    public void Configure(EntityTypeBuilder<ProspectDuplicateMatch> b) { b.ConfigureEntity("prospect_duplicate_matches"); b.Property(x => x.TargetType).HasConversion<string>(); b.Property(x => x.TargetName).HasMaxLength(200).IsRequired(); b.Property(x => x.Strength).HasConversion<string>(); b.Property(x => x.Decision).HasConversion<string>(); b.Property(x => x.Similarity).HasPrecision(5, 4); ResearchJson.StringList(b.Property(x => x.Reasons)); b.HasOne<ProspectCandidate>().WithMany().HasForeignKey(x => x.ProspectCandidateId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.ProspectCandidateId, x.TargetType, x.TargetId }).IsUnique(); }
}
public sealed class ProspectActivityConfiguration : IEntityTypeConfiguration<ProspectActivity>
{
    public void Configure(EntityTypeBuilder<ProspectActivity> b) { b.ConfigureEntity("prospect_activities"); b.Property(x => x.Action).HasConversion<string>(); b.Property(x => x.Note).HasMaxLength(2000); b.Property(x => x.BeforeJson).HasColumnType("jsonb"); b.Property(x => x.AfterJson).HasColumnType("jsonb"); b.HasOne<ProspectCandidate>().WithMany().HasForeignKey(x => x.ProspectCandidateId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.ProspectCandidateId, x.OccurredAt }); }
}
public sealed class ProspectConversionConfiguration : IEntityTypeConfiguration<ProspectConversion>
{
    public void Configure(EntityTypeBuilder<ProspectConversion> b) { b.ConfigureEntity("prospect_conversions"); b.HasOne<ProspectCandidate>().WithMany().HasForeignKey(x => x.ProspectCandidateId).OnDelete(DeleteBehavior.Restrict); b.HasOne<NetworkCompany>().WithMany().HasForeignKey(x => x.NetworkCompanyId).OnDelete(DeleteBehavior.Restrict); b.HasOne<User>().WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => x.ProspectCandidateId).IsUnique(); b.HasIndex(x => x.NetworkCompanyId).IsUnique(); }
}
