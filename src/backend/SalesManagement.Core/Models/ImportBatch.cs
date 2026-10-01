namespace SalesManagement.Core.Models;

public sealed class ImportBatch : EntityBase
{
    public ImportKind Kind { get; set; }
    public required string FileName { get; set; }
    public Guid RequestedBy { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Validating;
    public int NewCount { get; set; }
    public int DuplicateCount { get; set; }
    public int ChangeCandidateCount { get; set; }
    public int ErrorCount { get; set; }
}
