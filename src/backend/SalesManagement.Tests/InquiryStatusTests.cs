using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Quotations;
using SalesManagement.InMemory.Repositories;

namespace SalesManagement.Tests;

public sealed class InquiryStatusTests
{
    [Fact]
    public async Task FollowsWorkflowAndRequiresClosingMemo()
    {
        var (service, row, owner, _) = await Fixture(InquiryStatus.Received);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.ChangeStatusAsync(row.Id, InquiryStatus.Quoting, null, row.Version, owner));
        foreach (var status in new[] { InquiryStatus.Assigned, InquiryStatus.Responding, InquiryStatus.Quoting, InquiryStatus.Answered })
            row = await service.ChangeStatusAsync(row.Id, status, null, row.Version, owner);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.ChangeStatusAsync(row.Id, InquiryStatus.Closed, " ", row.Version, owner));
        row = await service.ChangeStatusAsync(row.Id, InquiryStatus.Closed, "고객 답변 완료", row.Version, owner);
        Assert.Equal(InquiryStatus.Closed, row.Status);
        Assert.Equal("고객 답변 완료", row.ResultMemo);
        Assert.Equal(5, row.Activities.Count);
    }

    [Fact]
    public async Task OnlyManagerReopensClosedInquiryToRespondingWithAuditHistory()
    {
        var (service, row, owner, manager) = await Fixture(InquiryStatus.Closed);
        await Assert.ThrowsAsync<ForbiddenException>(() => service.ChangeStatusAsync(row.Id, InquiryStatus.Responding, "추가 문의", row.Version, owner));
        await Assert.ThrowsAsync<InvalidDataException>(() => service.ChangeStatusAsync(row.Id, InquiryStatus.Received, null, row.Version, manager));
        var reopened = await service.ChangeStatusAsync(row.Id, InquiryStatus.Responding, "추가 문의", row.Version, manager);
        Assert.Equal(InquiryStatus.Responding, reopened.Status);
        Assert.Equal(manager.Id, reopened.Activities.Single().ActorId);
        Assert.Contains("Closed", reopened.Activities.Single().Body);
        Assert.Contains("추가 문의", reopened.Activities.Single().Body);
    }

    [Fact]
    public async Task ReassignmentCannotReopenOrRewindInquiry()
    {
        var (service, row, _, manager) = await Fixture(InquiryStatus.Closed);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.AssignAsync(row.Id, manager.Id, row.Version, manager));
        (service, row, _, manager) = await Fixture(InquiryStatus.Quoting);
        var reassigned = await service.AssignAsync(row.Id, manager.Id, row.Version, manager);
        Assert.Equal(InquiryStatus.Quoting, reassigned.Status);
        Assert.Equal(manager.Id, reassigned.OwnerUserId);
    }

    [Theory]
    [InlineData(InquiryStatus.Responding, InquiryStatus.Received)]
    [InlineData(InquiryStatus.Received, (InquiryStatus)99)]
    public async Task RejectsBackwardAndUnknownStatuses(InquiryStatus current, InquiryStatus next)
    {
        var (service, row, owner, _) = await Fixture(current);
        await Assert.ThrowsAsync<InvalidDataException>(() => service.ChangeStatusAsync(row.Id, next, null, row.Version, owner));
        Assert.Equal(current, (await service.GetAsync(row.Id, owner)).Status);
    }

    private static async Task<(InquiryService, CustomerInquiry, User, User)> Fixture(InquiryStatus status)
    {
        var owner = new User { LoginId = "owner", PasswordHash = "hash", Name = "담당자", Email = "owner@example.test", Role = Role.Member };
        var manager = new User { LoginId = "manager", PasswordHash = "hash", Name = "팀장", Email = "manager@example.test", Role = Role.Manager };
        var repo = new InMemoryInquiryRepository();
        var row = await repo.AddAsync(new CustomerInquiry { InquiryNumber = "INQ-202610-001", Title = "문의", CustomerId = Guid.NewGuid(), CustomerName = "거래처", ContactName = "담당", OwnerUserId = owner.Id, ReceivedAt = new(2026, 10, 8), ResponseDueAt = new(2026, 10, 10), Summary = "사양", Status = status });
        return (new InquiryService(repo, new InMemoryCustomerRepository()), row, owner, manager);
    }
}
