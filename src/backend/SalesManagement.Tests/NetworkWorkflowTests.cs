using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Network;
using SalesManagement.InMemory.Repositories;
using System.Text.Json;

namespace SalesManagement.Tests;

public sealed class NetworkWorkflowTests
{
    [Fact]
    public async Task Manager_Can_Create_Oem_And_Audit_Json_Escapes_Company_Name()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var customers = new InMemoryCustomerRepository();
        var assignments = new InMemoryCompanyAssignmentRepository();
        var users = new InMemoryUserRepository();
        var audits = new InMemoryAuditRepository();
        var actor = Manager();
        await users.AddAsync(actor);
        var service = new NetworkCompanyService(companies, customers, assignments, users, audits,
            new InMemoryTransactionRunner(companies, customers, assignments, audits));

        var company = await service.SaveCompanyAsync(
            new NetworkCompanySaveCommand(null, "삼성 \"전자\"", "KR", "전자", LifecycleType.Customer, true, actor.Id, EntityStatus.Active), actor);

        Assert.True(company.IsOem);
        Assert.Equal(LifecycleType.Customer, company.LifecycleType);
        Assert.Null(company.CustomerId);
        using var json = JsonDocument.Parse(Assert.Single(await audits.ListByTargetAsync(company.Id.ToString())).AfterJson!);
        Assert.Equal(company.CompanyName, json.RootElement.GetProperty("CompanyName").GetString());
    }

    [Fact]
    public async Task Relationship_Rejects_Three_Node_Cycle_And_Overlap()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var relationships = new InMemoryCompanyRelationshipRepository();
        var products = new InMemoryProductRepository();
        var owner = Manager();
        var product = await products.AddAsync(new Product { Code = "SW-001", Name = "Switch", ItemType = ItemType.Product, Specification = "" });
        var a = await companies.AddAsync(Company("A", owner.Id, true));
        var b = await companies.AddAsync(Company("B", owner.Id));
        var c = await companies.AddAsync(Company("C", owner.Id));
        var service = new NetworkRelationshipService(relationships, companies, new InMemoryCompanyAssignmentRepository(), products);
        await service.SaveRelationshipAsync(Command(a.Id, b.Id, product.Id), owner, 0);
        await service.SaveRelationshipAsync(Command(b.Id, c.Id, product.Id), owner, 0);

        await Assert.ThrowsAsync<NetworkCycleException>(() => service.SaveRelationshipAsync(Command(c.Id, a.Id, product.Id), owner, 0));
        await Assert.ThrowsAsync<NetworkRelationshipConflictException>(() => service.SaveRelationshipAsync(Command(a.Id, b.Id, product.Id), owner, 0));
    }

    [Fact]
    public async Task Tree_Shows_Shared_Company_Under_Two_Oem_Roots()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var relationships = new InMemoryCompanyRelationshipRepository();
        var ownerId = Guid.NewGuid();
        var rootA = await companies.AddAsync(Company("삼성", ownerId, true));
        var rootB = await companies.AddAsync(Company("LG", ownerId, true));
        var shared = await companies.AddAsync(Company("공용 협력사", ownerId));
        await relationships.AddAsync(Relation(rootA.Id, shared.Id));
        await relationships.AddAsync(Relation(rootB.Id, shared.Id));

        var trees = await new NetworkTreeService(companies, relationships).BuildTreeAsync(new NetworkTreeQuery(), CancellationToken.None);

        Assert.Equal(2, trees.Count);
        Assert.All(trees, root => Assert.Contains(root.Children, child => child.Company.Id == shared.Id && child.Depth == 1));
    }

    [Fact]
    public async Task Prospect_Conversion_Preserves_Network_Identity()
    {
        var networks = new InMemoryNetworkCompanyRepository();
        var customers = new InMemoryCustomerRepository();
        var audits = new InMemoryAuditRepository();
        var users = new InMemoryUserRepository();
        var actor = Manager();
        await users.AddAsync(actor);
        var prospect = await networks.AddAsync(Company("신규사", actor.Id, lifecycle: LifecycleType.Prospect));
        var assignmentRepo = new InMemoryCompanyAssignmentRepository();
        var service = new NetworkCompanyService(networks, customers, assignmentRepo, users, audits, new InMemoryTransactionRunner(networks, customers, assignmentRepo, audits));

        var converted = await service.ConvertProspectAsync(prospect.Id, "CUS-900", Currency.KRW, prospect.Version, actor);

        Assert.Equal(prospect.Id, converted.Id);
        Assert.Equal(LifecycleType.Customer, converted.LifecycleType);
        Assert.NotNull(converted.CustomerId);
        Assert.NotNull(await customers.GetAsync(converted.CustomerId!.Value));
        Assert.Single(await audits.ListByTargetAsync(prospect.Id.ToString()));
    }

    [Fact]
    public async Task Assignment_Change_Closes_Previous_And_Keeps_One_Current()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var assignments = new InMemoryCompanyAssignmentRepository();
        var users = new InMemoryUserRepository();
        var actor = Manager();
        var next = Member("next");
        await users.AddAsync(actor); await users.AddAsync(next);
        var company = await companies.AddAsync(Company("협력사", actor.Id));
        await assignments.AddAsync(new CompanyAssignment { CompanyId = company.Id, UserId = actor.Id, ValidFrom = new DateOnly(2026, 1, 1), IsCurrent = true });
        var customers = new InMemoryCustomerRepository();
        var audits = new InMemoryAuditRepository();
        var service = new CompanyAssignmentService(assignments, companies, users, customers, audits, new InMemoryTransactionRunner(assignments, companies, customers, audits));

        await service.ChangeAssignmentAsync(company.Id, next.Id, new DateOnly(2026, 10, 1), company.Version, actor);
        var history = await assignments.ListByCompanyAsync(company.Id);

        Assert.Single(history, x => x.IsCurrent);
        Assert.Equal(new DateOnly(2026, 9, 30), history.Single(x => x.UserId == actor.Id).ValidTo);
        Assert.Equal(next.Id, (await companies.GetAsync(company.Id))!.CurrentOwnerUserId);
    }

    [Fact]
    public async Task Services_Reject_Invalid_Date_Ranges()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var actor = Manager();
        var a = await companies.AddAsync(Company("A", actor.Id));
        var b = await companies.AddAsync(Company("B", actor.Id));
        var service = new NetworkRelationshipService(new InMemoryCompanyRelationshipRepository(), companies, new InMemoryCompanyAssignmentRepository());
        var invalid = Command(a.Id, b.Id) with { ValidTo = new DateOnly(2025, 12, 31) };

        await Assert.ThrowsAsync<NetworkValidationException>(() => service.SaveRelationshipAsync(invalid, actor, 0));
    }

    [Fact]
    public async Task Relationship_Update_Checks_Existing_Endpoints_And_Active_Actor()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var relationships = new InMemoryCompanyRelationshipRepository();
        var products = new InMemoryProductRepository();
        var assignments = new InMemoryCompanyAssignmentRepository();
        var actor = Member("member");
        var other = Member("other");
        var mine = await companies.AddAsync(Company("내 회사", actor.Id));
        var otherA = await companies.AddAsync(Company("타사 A", other.Id));
        var otherB = await companies.AddAsync(Company("타사 B", other.Id));
        var product = await products.AddAsync(new Product { Code = "SW-002", Name = "Switch", ItemType = ItemType.Product });
        var saved = await relationships.AddAsync(Relation(otherA.Id, otherB.Id));
        var service = new NetworkRelationshipService(relationships, companies, assignments, products);
        var hijack = Command(mine.Id, otherB.Id, product.Id) with { Id = saved.Id };

        await Assert.ThrowsAsync<ForbiddenException>(() => service.SaveRelationshipAsync(hijack, actor, saved.Version));
        actor.Status = EntityStatus.Inactive;
        await Assert.ThrowsAsync<ForbiddenException>(() => service.SaveRelationshipAsync(Command(mine.Id, otherB.Id, product.Id), actor, 0));
    }

    [Fact]
    public async Task Tree_Preserves_Ancestor_For_Matching_Child_And_Excludes_Expired_Edge()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var relationships = new InMemoryCompanyRelationshipRepository();
        var owner = Guid.NewGuid();
        var root = await companies.AddAsync(Company("원청", owner, true));
        var match = await companies.AddAsync(Company("찾는 회사", owner, lifecycle: LifecycleType.Prospect));
        var expired = await companies.AddAsync(Company("만료 회사", owner, lifecycle: LifecycleType.Prospect));
        await relationships.AddAsync(Relation(root.Id, match.Id));
        await relationships.AddAsync(new CompanyRelationship { UpstreamCompanyId = root.Id, DownstreamCompanyId = expired.Id, ValidFrom = new DateOnly(2025, 1, 1), ValidTo = new DateOnly(2025, 12, 31) });

        var tree = await new NetworkTreeService(companies, relationships).BuildTreeAsync(new NetworkTreeQuery(LifecycleType: LifecycleType.Prospect, Search: "찾는"));

        var onlyRoot = Assert.Single(tree);
        Assert.Equal(root.Id, onlyRoot.Company.Id);
        Assert.Equal(match.Id, Assert.Single(onlyRoot.Children).Company.Id);
    }

    [Fact]
    public async Task Oem_Filter_Composes_With_Owner_Status_And_Search()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var owner = Guid.NewGuid();
        await companies.AddAsync(Company("삼성전자", owner, true));
        await companies.AddAsync(Company("LG전자", Guid.NewGuid(), true));
        var inactive = Company("삼성SDI", owner, true); inactive.Status = EntityStatus.Inactive;
        await companies.AddAsync(inactive);

        var tree = await new NetworkTreeService(companies, new InMemoryCompanyRelationshipRepository())
            .BuildTreeAsync(new NetworkTreeQuery(OwnerId: owner, Status: EntityStatus.Active, Search: "삼성", OemOnly: true));

        Assert.Equal("삼성전자", Assert.Single(tree).Company.CompanyName);
    }

    [Fact]
    public async Task Ordinary_Company_Update_Cannot_Change_Classification()
    {
        var companies = new InMemoryNetworkCompanyRepository();
        var customers = new InMemoryCustomerRepository();
        var assignments = new InMemoryCompanyAssignmentRepository();
        var users = new InMemoryUserRepository();
        var audits = new InMemoryAuditRepository();
        var actor = Manager();
        await users.AddAsync(actor);
        var company = await companies.AddAsync(Company("잠재사", actor.Id, lifecycle: LifecycleType.Prospect));
        await assignments.AddAsync(new CompanyAssignment { CompanyId = company.Id, UserId = actor.Id, ValidFrom = new DateOnly(2026, 1, 1), IsCurrent = true });
        var service = new NetworkCompanyService(companies, customers, assignments, users, audits, new InMemoryTransactionRunner(companies, customers, assignments, audits));
        var command = new NetworkCompanySaveCommand(company.Id, company.CompanyName, company.Country, company.Industry, company.LifecycleType, true, actor.Id, company.Status);

        await Assert.ThrowsAsync<NetworkValidationException>(() => service.SaveCompanyAsync(command, actor, company.Version));
    }

    private static NetworkRelationshipSaveCommand Command(Guid up, Guid down, Guid? productId = null) => new(null, up, down, new DateOnly(2026, 1, 1), null, null, EntityStatus.Active,
        productId.HasValue ? [new RelationshipSupplyCommand(productId.Value, "프로젝트", null)] : []);
    private static CompanyRelationship Relation(Guid up, Guid down) => new() { UpstreamCompanyId = up, DownstreamCompanyId = down, ValidFrom = new DateOnly(2026, 1, 1) };
    private static NetworkCompany Company(string name, Guid ownerId, bool oem = false, LifecycleType lifecycle = LifecycleType.Customer) => new()
    { CompanyName = name, Country = "KR", Industry = "전자", LifecycleType = lifecycle, IsOem = oem, CurrentOwnerUserId = ownerId };
    private static User Manager() => new() { LoginId = "manager", PasswordHash = "hash", Name = "이재학", Email = "jhlee@switchkorea.com", Role = Role.Manager };
    private static User Member(string id) => new() { LoginId = id, PasswordHash = "hash", Name = id, Email = $"{id}@switchkorea.com", Role = Role.Member };
}
