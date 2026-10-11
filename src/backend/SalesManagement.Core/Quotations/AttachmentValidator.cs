using System.IO.Compression;

namespace SalesManagement.Core.Quotations;
public static class AttachmentValidator
{
    private const int MaxBytes = 10 * 1024 * 1024;
    public static string Validate(AttachmentUpload upload)
    {
        var name = upload.FileName;
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 ||
            name.Any(c => char.IsControl(c) || "<>:\"/\\|?*".Contains(c)) || name.EndsWith('.') || name.EndsWith(' ') ||
            upload.Content.Length is 0 or > MaxBytes)
            throw new InvalidDataException("안전한 파일명과 10MB 이하의 파일이 필요합니다.");

        var content = upload.Content.AsSpan();
        var mime = upload.ContentType.Trim().ToLowerInvariant();
        var valid = Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".pdf" => mime == "application/pdf" && content.StartsWith("%PDF-"u8),
            ".png" => mime == "image/png" && content.StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            ".jpg" or ".jpeg" => mime == "image/jpeg" && content.StartsWith(new byte[] { 255, 216, 255 }),
            ".xls" => mime == "application/vnd.ms-excel" && content.StartsWith(new byte[] { 208, 207, 17, 224, 161, 177, 26, 225 }),
            ".xlsx" => mime == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" && IsOfficePackage(upload.Content, "xl/workbook.xml"),
            ".docx" => mime == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" && IsOfficePackage(upload.Content, "word/document.xml"),
            _ => false
        };
        if (!valid) throw new InvalidDataException("확장자, 파일 형식 또는 파일 내용이 일치하지 않습니다.");
        return name;
    }

    private static bool IsOfficePackage(byte[] content, string requiredEntry)
    {
        try
        {
            using var stream = new MemoryStream(content, false);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            return archive.GetEntry("[Content_Types].xml") is not null && archive.GetEntry(requiredEntry) is not null;
        }
        catch (InvalidDataException) { return false; }
    }
}
