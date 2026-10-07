using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalesManagement.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddProspectResearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "research_target_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Regions = table.Column<string>(type: "jsonb", nullable: false),
                    Industries = table.Column<string>(type: "jsonb", nullable: false),
                    IncludeKeywords = table.Column<string>(type: "jsonb", nullable: false),
                    ExcludeKeywords = table.Column<string>(type: "jsonb", nullable: false),
                    MaxSearchResults = table.Column<int>(type: "integer", nullable: false),
                    MaxAiAnalyses = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    NextRunAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_target_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "research_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchTargetProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DiscoveredCount = table.Column<int>(type: "integer", nullable: false),
                    EnrichedCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    ApiCallCount = table.Column<int>(type: "integer", nullable: false),
                    CostKrw = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ErrorSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    InputHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_runs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_research_runs_research_target_profiles_ResearchTargetProfil~",
                        column: x => x.ResearchTargetProfileId,
                        principalTable: "research_target_profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospect_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchRunId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderResultId = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EnglishName = table.Column<string>(type: "text", nullable: true),
                    NormalizedCompanyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Industry = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CompanySize = table.Column<string>(type: "text", nullable: true),
                    Domain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    NormalizedDomain = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: false),
                    Website = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Products = table.Column<string>(type: "jsonb", nullable: false),
                    Applications = table.Column<string>(type: "jsonb", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    NetworkCompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prospect_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prospect_candidates_network_companies_NetworkCompanyId",
                        column: x => x.NetworkCompanyId,
                        principalTable: "network_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prospect_candidates_research_runs_ResearchRunId",
                        column: x => x.ResearchRunId,
                        principalTable: "research_runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prospect_candidates_users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospect_activities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: true),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prospect_activities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prospect_activities_prospect_candidates_ProspectCandidateId",
                        column: x => x.ProspectCandidateId,
                        principalTable: "prospect_candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prospect_activities_users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospect_assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductFit = table.Column<int>(type: "integer", nullable: false),
                    IndustryFit = table.Column<int>(type: "integer", nullable: false),
                    SupplyChainFit = table.Column<int>(type: "integer", nullable: false),
                    RegionFit = table.Column<int>(type: "integer", nullable: false),
                    CompanyPotential = table.Column<int>(type: "integer", nullable: false),
                    EvidenceQuality = table.Column<int>(type: "integer", nullable: false),
                    Total = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<string>(type: "text", nullable: false),
                    RuleVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AiSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    AiRecommendation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AiRisks = table.Column<string>(type: "jsonb", nullable: false),
                    AiModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prospect_assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prospect_assessments_prospect_candidates_ProspectCandidateId",
                        column: x => x.ProspectCandidateId,
                        principalTable: "prospect_candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospect_conversions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    NetworkCompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConvertedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prospect_conversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prospect_conversions_network_companies_NetworkCompanyId",
                        column: x => x.NetworkCompanyId,
                        principalTable: "network_companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prospect_conversions_prospect_candidates_ProspectCandidateId",
                        column: x => x.ProspectCandidateId,
                        principalTable: "prospect_candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_prospect_conversions_users_ApprovedByUserId",
                        column: x => x.ApprovedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospect_duplicate_matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Similarity = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Strength = table.Column<string>(type: "text", nullable: false),
                    Reasons = table.Column<string>(type: "jsonb", nullable: false),
                    Decision = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prospect_duplicate_matches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_prospect_duplicate_matches_prospect_candidates_ProspectCand~",
                        column: x => x.ProspectCandidateId,
                        principalTable: "prospect_candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "research_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProspectCandidateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ObservedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Summary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Confidence = table.Column<string>(type: "text", nullable: false),
                    SourceHash = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_evidence", x => x.Id);
                    table.CheckConstraint("CK_research_evidence_source_observed", "length(\"SourceUrl\") > 0 AND \"ObservedDate\" > '0001-01-01'");
                    table.ForeignKey(
                        name: "FK_research_evidence_prospect_candidates_ProspectCandidateId",
                        column: x => x.ProspectCandidateId,
                        principalTable: "prospect_candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_prospect_activities_ActorUserId",
                table: "prospect_activities",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_activities_ProspectCandidateId_OccurredAt",
                table: "prospect_activities",
                columns: new[] { "ProspectCandidateId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_prospect_assessments_ProspectCandidateId_AssessedAt",
                table: "prospect_assessments",
                columns: new[] { "ProspectCandidateId", "AssessedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_prospect_candidates_Country_NormalizedCompanyName",
                table: "prospect_candidates",
                columns: new[] { "Country", "NormalizedCompanyName" });

            migrationBuilder.CreateIndex(
                name: "IX_prospect_candidates_NetworkCompanyId",
                table: "prospect_candidates",
                column: "NetworkCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_candidates_NormalizedDomain",
                table: "prospect_candidates",
                column: "NormalizedDomain");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_candidates_OwnerUserId",
                table: "prospect_candidates",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_candidates_ProviderResultId",
                table: "prospect_candidates",
                column: "ProviderResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prospect_candidates_ResearchRunId",
                table: "prospect_candidates",
                column: "ResearchRunId");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_conversions_ApprovedByUserId",
                table: "prospect_conversions",
                column: "ApprovedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_prospect_conversions_NetworkCompanyId",
                table: "prospect_conversions",
                column: "NetworkCompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prospect_conversions_ProspectCandidateId",
                table: "prospect_conversions",
                column: "ProspectCandidateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_prospect_duplicate_matches_ProspectCandidateId_TargetType_T~",
                table: "prospect_duplicate_matches",
                columns: new[] { "ProspectCandidateId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_evidence_ProspectCandidateId",
                table: "research_evidence",
                column: "ProspectCandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_research_evidence_SourceHash",
                table: "research_evidence",
                column: "SourceHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_research_runs_InputHash",
                table: "research_runs",
                column: "InputHash",
                unique: true,
                filter: "\"InputHash\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_research_runs_ResearchTargetProfileId",
                table: "research_runs",
                column: "ResearchTargetProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_research_target_profiles_Name",
                table: "research_target_profiles",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prospect_activities");

            migrationBuilder.DropTable(
                name: "prospect_assessments");

            migrationBuilder.DropTable(
                name: "prospect_conversions");

            migrationBuilder.DropTable(
                name: "prospect_duplicate_matches");

            migrationBuilder.DropTable(
                name: "research_evidence");

            migrationBuilder.DropTable(
                name: "prospect_candidates");

            migrationBuilder.DropTable(
                name: "research_runs");

            migrationBuilder.DropTable(
                name: "research_target_profiles");
        }
    }
}
