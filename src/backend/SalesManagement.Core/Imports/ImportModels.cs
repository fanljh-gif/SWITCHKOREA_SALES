using SalesManagement.Core.Models;

namespace SalesManagement.Core.Imports;

public enum ImportRowStatus { New, Duplicate, ChangeCandidate, Invalid }
public sealed record ImportError(int? RowNumber, string Message);
public sealed record ImportRowResult(int RowNumber, ImportRowStatus Status, IReadOnlyDictionary<string, string> Values, IReadOnlyList<ImportError> Errors);
public sealed record ImportValidation(Guid BatchId, ImportKind Kind, DateTimeOffset ExpiresAt, IReadOnlyList<ImportRowResult> Rows, IReadOnlyList<ImportError> Errors);
public sealed record ImportResult(Guid BatchId, int AppliedCount);
