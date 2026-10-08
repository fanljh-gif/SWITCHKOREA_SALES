namespace SalesManagement.Core.Quotations;
public static class AttachmentValidator
{
    private const int MaxBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".xlsx", ".xls", ".docx", ".png", ".jpg", ".jpeg" };
    public static string Validate(AttachmentUpload upload)
    {
        if (upload.Content.Length is 0 or > MaxBytes || !Extensions.Contains(Path.GetExtension(upload.FileName))) throw new InvalidDataException("10MB 이하의 허용된 파일만 등록할 수 있습니다.");
        return Path.GetFileName(upload.FileName);
    }
}
