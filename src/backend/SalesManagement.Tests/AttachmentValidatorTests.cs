using System.IO.Compression;
using SalesManagement.Core.Quotations;

namespace SalesManagement.Tests;

public sealed class AttachmentValidatorTests
{
    [Theory]
    [InlineData("file.pdf", "image/png")]
    [InlineData("file.png", "image/png")]
    [InlineData("file.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("../file.pdf", "application/pdf")]
    [InlineData("bad\nname.pdf", "application/pdf")]
    public void RejectsMismatchedOrUnsafeUploads(string name, string mime)
    {
        Assert.Throws<InvalidDataException>(() => AttachmentValidator.Validate(new(name, mime, "%PDF-1.7"u8.ToArray())));
    }

    [Fact]
    public void AcceptsPdfWithMatchingTypeAndSignature()
    {
        Assert.Equal("견적.pdf", AttachmentValidator.Validate(new("견적.pdf", "application/pdf", "%PDF-1.7"u8.ToArray())));
    }

    [Fact]
    public void RejectsEmptyAndOversizeFiles()
    {
        Assert.Throws<InvalidDataException>(() => AttachmentValidator.Validate(new("a.pdf", "application/pdf", [])));
        Assert.Throws<InvalidDataException>(() => AttachmentValidator.Validate(new("a.pdf", "application/pdf", new byte[10 * 1024 * 1024 + 1])));
    }

    [Theory]
    [InlineData("xlsx", "xl/workbook.xml", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("docx", "word/document.xml", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void RequiresExpectedOfficePackageEntries(string extension, string entry, string mime)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            zip.CreateEntry("[Content_Types].xml");
            zip.CreateEntry(entry);
        }
        Assert.Equal($"a.{extension}", AttachmentValidator.Validate(new($"a.{extension}", mime, buffer.ToArray())));
        Assert.Throws<InvalidDataException>(() => AttachmentValidator.Validate(new($"a.{extension}", mime, "PK-not-a-package"u8.ToArray())));
    }
}
