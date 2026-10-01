using System.Collections.Concurrent;
using System.Globalization;
using ClosedXML.Excel;
using SalesManagement.Core.Exceptions;
using SalesManagement.Core.Models;
using SalesManagement.Core.Repositories;
using SalesManagement.Core.Shipments;

namespace SalesManagement.Core.Imports;

public sealed class ImportValidationStore
{
    internal ConcurrentDictionary<Guid, ImportValidation> Batches { get; } = new();
}

public sealed class ExcelImportService(
    IUserRepository users, ICustomerRepository customers, IProductRepository products, IPriceRepository prices,
    IShipmentRepository shipments, IImportBatchRepository batches, ShipmentService shipmentService, TimeProvider clock,
    ITransactionRunner? transactions = null, IAuditRepository? audits = null, ImportValidationStore? validationStore = null)
{
    private readonly ConcurrentDictionary<Guid, ImportValidation> _validated = validationStore?.Batches ?? new();
    private static readonly IReadOnlyDictionary<ImportKind, string[]> Required = new Dictionary<ImportKind, string[]>
    {
        [ImportKind.Customer] = ["Code", "CompanyName", "Country", "Currency", "OwnerLoginId"],
        [ImportKind.Product] = ["Code", "Name", "ItemType"],
        [ImportKind.Price] = ["CustomerCode", "ProductCode", "Currency", "UnitPrice", "ValidFrom"],
        [ImportKind.Shipment] = ["ShipmentNumber", "ShipmentDate", "CustomerCode", "ProductCode", "Quantity"]
    };

    public async Task<ImportValidation> ValidateAsync(Stream stream, ImportKind kind, string fileName, User actor, CancellationToken ct)
    {
        var batch = await batches.AddAsync(new ImportBatch { Kind = kind, FileName = fileName, RequestedBy = actor.Id }, ct);
        var errors = new List<ImportError>(); var rows = new List<ImportRowResult>();
        try
        {
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null) errors.Add(new(null, "필수 열이 없습니다"));
            else
            {
                var headers = sheet.Row(1).CellsUsed().ToDictionary(x => x.GetString().Trim(), x => x.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);
                if (Required[kind].Any(x => !headers.ContainsKey(x))) errors.Add(new(null, "필수 열이 없습니다"));
                else
                    foreach (var row in sheet.RowsUsed().Skip(1)) rows.Add(await ValidateRow(row, headers, kind, ct));
            }
        }
        catch { errors.Add(new(null, "파일을 읽을 수 없습니다")); }

        batch.ErrorCount = errors.Count + rows.Count(x => x.Status == ImportRowStatus.Invalid);
        batch.NewCount = rows.Count(x => x.Status == ImportRowStatus.New);
        batch.DuplicateCount = rows.Count(x => x.Status == ImportRowStatus.Duplicate);
        batch.ChangeCandidateCount = rows.Count(x => x.Status == ImportRowStatus.ChangeCandidate);
        batch.Status = batch.ErrorCount == 0 ? ImportStatus.Ready : ImportStatus.Rejected;
        await batches.UpdateAsync(batch, batch.Version, ct);
        var result = new ImportValidation(batch.Id, kind, clock.GetUtcNow().AddHours(1), rows, errors);
        _validated[result.BatchId] = result;
        return result;
    }

    public async Task<ImportResult> CommitAsync(Guid batchId, User actor, CancellationToken ct)
    {
        if (!_validated.TryGetValue(batchId, out var validation) || validation.ExpiresAt <= clock.GetUtcNow()) throw new InvalidImportBatchException("가져오기 배치가 없거나 만료되었습니다.");
        if (validation.Errors.Count > 0 || validation.Rows.Any(x => x.Status == ImportRowStatus.Invalid)) throw new InvalidImportBatchException();
        return transactions is null ? await CommitCore(validation, actor, ct) : await transactions.ExecuteAsync(token => CommitCore(validation, actor, token), ct);
    }

    private async Task<ImportResult> CommitCore(ImportValidation validation, User actor, CancellationToken ct)
    {
        var applied = 0;
        foreach (var row in validation.Rows.Where(x => x.Status == ImportRowStatus.New))
        {
            await Apply(row.Values, validation.Kind, actor, ct); applied++;
        }
        var batch = await batches.GetAsync(validation.BatchId, ct) ?? throw new InvalidImportBatchException();
        batch.Status = ImportStatus.Committed; await batches.UpdateAsync(batch, batch.Version, ct);
        if (audits is not null) await audits.AddAsync(new AuditEntry { ActorUserId = actor.Id, Target = $"import:{validation.BatchId}", Action = "commit", AfterJson = $"{{\"appliedCount\":{applied}}}" }, ct);
        _validated.TryRemove(validation.BatchId, out _);
        return new ImportResult(validation.BatchId, applied);
    }

    private async Task<ImportRowResult> ValidateRow(IXLRow row, IReadOnlyDictionary<string, int> headers, ImportKind kind, CancellationToken ct)
    {
        var values = headers.ToDictionary(x => x.Key, x => row.Cell(x.Value).GetFormattedString().Trim(), StringComparer.OrdinalIgnoreCase);
        var errors = new List<ImportError>();
        ImportRowStatus status = ImportRowStatus.New;
        try
        {
            if (kind == ImportKind.Shipment)
            {
                if (!DateOnly.TryParse(values["ShipmentDate"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) || !decimal.TryParse(values["Quantity"], NumberStyles.Number, CultureInfo.InvariantCulture, out var quantity) || quantity <= 0m) throw new FormatException();
                var customer = (await customers.ListAsync(ct)).SingleOrDefault(x => x.Code.Equals(values["CustomerCode"], StringComparison.OrdinalIgnoreCase)) ?? throw new KeyNotFoundException();
                var product = (await products.ListAsync(ct)).SingleOrDefault(x => x.Code.Equals(values["ProductCode"], StringComparison.OrdinalIgnoreCase)) ?? throw new KeyNotFoundException();
                var existing = await shipments.ListAsync(ct);
                status = string.IsNullOrWhiteSpace(values["ShipmentNumber"])
                    ? (existing.Any(x => x.ShipmentDate == date && x.CustomerId == customer.Id && x.ProductId == product.Id && x.Quantity == quantity) ? ImportRowStatus.Duplicate : ImportRowStatus.New)
                    : (existing.Any(x => x.CustomerId == customer.Id && x.ShipmentNumber == values["ShipmentNumber"]) ? ImportRowStatus.Duplicate : ImportRowStatus.New);
            }
            else if (kind == ImportKind.Customer) status = (await customers.ListAsync(ct)).Any(x => x.Code.Equals(values["Code"], StringComparison.OrdinalIgnoreCase)) ? ImportRowStatus.ChangeCandidate : ImportRowStatus.New;
            else if (kind == ImportKind.Product) status = (await products.ListAsync(ct)).Any(x => x.Code.Equals(values["Code"], StringComparison.OrdinalIgnoreCase)) ? ImportRowStatus.ChangeCandidate : ImportRowStatus.New;
            else ValidatePrice(values);
        }
        catch { errors.Add(new(row.RowNumber(), "행 데이터를 확인하세요")); status = ImportRowStatus.Invalid; }
        return new ImportRowResult(row.RowNumber(), status, values, errors);
    }

    private static void ValidatePrice(IReadOnlyDictionary<string, string> values)
    {
        if (!Enum.TryParse<Currency>(values["Currency"], true, out _) || !decimal.TryParse(values["UnitPrice"], NumberStyles.Number, CultureInfo.InvariantCulture, out var price) || price < 0m || !DateOnly.TryParse(values["ValidFrom"], out _)) throw new FormatException();
    }

    private async Task Apply(IReadOnlyDictionary<string, string> values, ImportKind kind, User actor, CancellationToken ct)
    {
        if (kind == ImportKind.Customer)
        {
            var owner = await users.FindByLoginIdAsync(values["OwnerLoginId"], ct) ?? throw new InvalidImportBatchException();
            await customers.AddAsync(new Customer { Code = values["Code"], CompanyName = values["CompanyName"], Country = values["Country"], DefaultCurrency = Enum.Parse<Currency>(values["Currency"], true), OwnerUserId = owner.Id }, ct); return;
        }
        if (kind == ImportKind.Product) { await products.AddAsync(new Product { Code = values["Code"], Name = values["Name"], ItemType = Enum.Parse<ItemType>(values["ItemType"], true) }, ct); return; }
        var customer = (await customers.ListAsync(ct)).Single(x => x.Code.Equals(values["CustomerCode"], StringComparison.OrdinalIgnoreCase));
        var product = (await products.ListAsync(ct)).Single(x => x.Code.Equals(values["ProductCode"], StringComparison.OrdinalIgnoreCase));
        if (kind == ImportKind.Price)
        {
            await prices.AddAsync(new Price { CustomerId = customer.Id, ProductId = product.Id, Currency = Enum.Parse<Currency>(values["Currency"], true), UnitPrice = decimal.Parse(values["UnitPrice"], CultureInfo.InvariantCulture), ValidFrom = DateOnly.Parse(values["ValidFrom"], CultureInfo.InvariantCulture), ValidTo = string.IsNullOrWhiteSpace(values.GetValueOrDefault("ValidTo")) ? null : DateOnly.Parse(values["ValidTo"], CultureInfo.InvariantCulture) }, ct); return;
        }
        await shipmentService.SaveAsync(new ShipmentCreateCommand(string.IsNullOrWhiteSpace(values["ShipmentNumber"]) ? null : values["ShipmentNumber"], DateOnly.Parse(values["ShipmentDate"], CultureInfo.InvariantCulture), customer.Id, product.Id, decimal.Parse(values["Quantity"], CultureInfo.InvariantCulture)), actor, ct);
    }
}
