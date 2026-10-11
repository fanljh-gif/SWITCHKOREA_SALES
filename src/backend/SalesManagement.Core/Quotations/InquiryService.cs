using SalesManagement.Core.Auth; using SalesManagement.Core.Exceptions; using SalesManagement.Core.Models; using SalesManagement.Core.Repositories;
namespace SalesManagement.Core.Quotations;
public sealed class InquiryService(IInquiryRepository inquiries, ICustomerRepository customers)
{
    public async Task<IReadOnlyList<CustomerInquiry>> ListAsync(InquiryQuery query, User actor, CancellationToken ct = default)
    { AuthorizationService.EnsureActive(actor); var rows = await inquiries.ListAsync(ct); return rows.Where(x => (actor.Role == Role.Manager || x.OwnerUserId == actor.Id) && (!query.OwnerId.HasValue || x.OwnerUserId == query.OwnerId) && (!query.CustomerId.HasValue || x.CustomerId == query.CustomerId) && (!query.Status.HasValue || x.Status == query.Status) && (!query.OverdueOnly || x.ResponseDueAt < DateOnly.FromDateTime(DateTime.UtcNow.AddHours(9)) && x.Status is not InquiryStatus.Answered and not InquiryStatus.Closed) && (string.IsNullOrWhiteSpace(query.Search) || $"{x.InquiryNumber} {x.Title} {x.CustomerName}".Contains(query.Search, StringComparison.OrdinalIgnoreCase))).ToArray(); }
    public async Task<CustomerInquiry> GetAsync(Guid id, User actor, CancellationToken ct = default) { var row = await inquiries.GetAsync(id, ct) ?? throw new KeyNotFoundException(); EnsureOwner(row, actor); return row; }
    public async Task<CustomerInquiry> SaveAsync(SaveInquiryCommand command, User actor, CancellationToken ct = default)
    {
        AuthorizationService.EnsureActive(actor);
        if (string.IsNullOrWhiteSpace(command.Title) || command.ResponseDueAt < command.ReceivedAt) throw new InvalidDataException("문의 제목과 올바른 답변 기한을 입력해 주세요.");
        var customer = await customers.GetAsync(command.CustomerId, ct) ?? throw new KeyNotFoundException("거래처를 찾을 수 없습니다.");
        if (actor.Role != Role.Manager && command.OwnerId != actor.Id) throw new ForbiddenException();
        if (command.Id.HasValue) { var row = await GetAsync(command.Id.Value, actor, ct); row.Title = command.Title; row.ContactName = command.ContactName; row.ContactEmail = command.ContactEmail; row.ResponseDueAt = command.ResponseDueAt; row.Summary = command.Summary; return await inquiries.UpdateAsync(row, command.ExpectedVersion, ct); }
        var sequence = await inquiries.NextSequenceAsync(command.ReceivedAt.Year, command.ReceivedAt.Month, ct);
        return await inquiries.AddAsync(new CustomerInquiry { InquiryNumber = $"INQ-{command.ReceivedAt:yyyyMM}-{sequence:000}", Title = command.Title, CustomerId = customer.Id, CustomerName = customer.CompanyName, ContactName = command.ContactName, ContactEmail = command.ContactEmail, OwnerUserId = actor.Role == Role.Manager ? command.OwnerId : actor.Id, ReceivedAt = command.ReceivedAt, ResponseDueAt = command.ResponseDueAt, Summary = command.Summary }, ct);
    }
    public async Task<CustomerInquiry> AssignAsync(Guid id, Guid ownerId, long version, User actor, CancellationToken ct = default)
    {
        AuthorizationService.EnsureManager(actor);
        var row = await GetAsync(id, actor, ct);
        if (row.Status == InquiryStatus.Closed) throw new InvalidDataException("종료된 문의는 먼저 재개해야 합니다.");
        var previousOwner = row.OwnerUserId;
        row.OwnerUserId = ownerId;
        if (row.Status == InquiryStatus.Received) row.Status = InquiryStatus.Assigned;
        AddActivity(row, InquiryActivityKind.Assignment, $"담당자 변경: {previousOwner} -> {ownerId}", actor.Id);
        return await inquiries.UpdateAsync(row, version, ct);
    }

    public async Task<CustomerInquiry> ChangeStatusAsync(Guid id, InquiryStatus status, string? memo, long version, User actor, CancellationToken ct = default)
    {
        var row = await GetAsync(id, actor, ct);
        if (!Enum.IsDefined(status)) throw new InvalidDataException("알 수 없는 문의 상태입니다.");
        var previousStatus = row.Status;
        if (previousStatus == InquiryStatus.Closed)
        {
            AuthorizationService.EnsureManager(actor);
            if (status != InquiryStatus.Responding) throw new InvalidDataException("종료된 문의는 대응 중으로 재개할 수 있습니다.");
        }
        else if ((int)status != (int)previousStatus + 1)
            throw new InvalidDataException("다음 업무 단계로만 상태를 변경할 수 있습니다.");
        if (status == InquiryStatus.Closed && string.IsNullOrWhiteSpace(memo))
            throw new InvalidDataException("문의 종료 시 결과 메모를 입력해 주세요.");
        row.Status = status;
        if (status == InquiryStatus.Closed) row.ResultMemo = memo!.Trim();
        AddActivity(row, InquiryActivityKind.Status, $"상태 변경: {previousStatus} -> {status}" + (string.IsNullOrWhiteSpace(memo) ? "" : $" · {memo.Trim()}"), actor.Id);
        return await inquiries.UpdateAsync(row, version, ct);
    }
    public async Task<InquiryActivity> AddActivityAsync(Guid id, AddInquiryActivityCommand command, User actor, CancellationToken ct = default) { var row = await GetAsync(id, actor, ct); var item = AddActivity(row, command.Kind, command.Body, actor.Id); await inquiries.UpdateAsync(row, row.Version, ct); return item; }
    public async Task<InquiryAttachment> AddAttachmentAsync(Guid id, AttachmentUpload upload, User actor, CancellationToken ct = default) { var row = await GetAsync(id, actor, ct); var item = new InquiryAttachment { InquiryId = id, FileName = AttachmentValidator.Validate(upload), ContentType = upload.ContentType, Sha256 = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(upload.Content)).ToLowerInvariant(), Size = upload.Content.LongLength, Content = upload.Content, UploadedBy = actor.Id, UploadedAt = DateTimeOffset.UtcNow }; row.Attachments.Add(item); await inquiries.UpdateAsync(row, row.Version, ct); return item; }
    private static InquiryActivity AddActivity(CustomerInquiry row, InquiryActivityKind kind, string body, Guid actorId) { var item = new InquiryActivity { InquiryId = row.Id, Kind = kind, Body = body, ActorId = actorId, OccurredAt = DateTimeOffset.UtcNow }; row.Activities.Add(item); return item; }
    private static void EnsureOwner(CustomerInquiry row, User actor) { AuthorizationService.EnsureActive(actor); if (actor.Role != Role.Manager && row.OwnerUserId != actor.Id) throw new ForbiddenException(); }
}
