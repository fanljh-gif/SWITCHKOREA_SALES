using System.Text; using SalesManagement.Api.Quotations; using SalesManagement.Core.Models; using SalesManagement.Core.Quotations;
namespace SalesManagement.Tests;
public sealed class QuotePdfTests
{
    [Theory]
    [InlineData(QuoteLanguage.Ko)]
    [InlineData(QuoteLanguage.En)]
    public void Renders_table_spanning_pages(QuoteLanguage language)
    {
        var lines = Enumerable.Range(1, 120).Select(i => new QuoteLine {
            RevisionId = Guid.NewGuid(), ProductId = Guid.NewGuid(), Description = $"TABLE-ROW-{i:000} 스위치",
            Quantity = 1, UnitPrice = 100, Amount = 100,
        }).ToArray();
        var bytes = new MigraDocQuotePdfRenderer().Render(new QuotePdfModel("SWK-202610-120", "Rev.0", "표 분할 검증", Currency.KRW, 12000,
            null, null, language, lines) { SubtotalAmount = 12000, SupplyAmount = 12000, Notes = "TABLE-END-NOTES" });
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount >= 3);
        var directory = Path.Combine(AppContext.BaseDirectory, "quote-pdf-qa");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, $"table-pages-{language}.pdf"), bytes);
    }

    [Theory]
    [InlineData(QuoteLanguage.Ko)]
    [InlineData(QuoteLanguage.En)]
    public void Renders_long_notes_and_many_lines_across_multiple_pages(QuoteLanguage language)
    {
        const string tail = "END-OF-COMMERCIAL-NOTES";
        var text = string.Concat(Enumerable.Repeat("납기 조건 확인 및 수출 포장 안내. ", 250));
        var notes = text[..(4000 - tail.Length - 1)] + "\n" + tail;
        var lines = Enumerable.Range(1, 40).Select(i => new QuoteLine {
            RevisionId = Guid.NewGuid(), ProductId = Guid.NewGuid(), Description = $"검증용 스위치 품목 {i:000}",
            Quantity = 1, UnitPrice = 100, Amount = 100,
        }).ToArray();
        var model = new QuotePdfModel("SWK-202610-099", "Rev.1", "장문 검증 고객사", Currency.KRW, 4400,
            null, null, language, lines) { PaymentTerms = "출고 전 결제", DeliveryTerms = "발주 후 4주",
            Notes = notes, SubtotalAmount = 4000, SupplyAmount = 4000, TaxAmount = 400 };
        var bytes = new MigraDocQuotePdfRenderer().Render(model);
        using var stream = new MemoryStream(bytes);
        using var pdf = PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);
        Assert.True(pdf.PageCount >= 2);
        var directory = Path.Combine(AppContext.BaseDirectory, "quote-pdf-qa");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, $"multipage-{language}.pdf"), bytes);
    }

    [Fact] public void Renders_korean_and_english_approved_snapshot_as_pdf()
    {
        var line = new QuoteLine { RevisionId = Guid.NewGuid(), ProductId = Guid.NewGuid(), Description = "마이크로 스위치", Quantity = 100, UnitPrice = 2.5m, Amount = 250m }; var renderer = new MigraDocQuotePdfRenderer();
        foreach (var language in new[] { QuoteLanguage.Ko, QuoteLanguage.En }) { var bytes = renderer.Render(new("SWK-202610-001", "Rev.0", "삼성전자", Currency.USD, 275, "CIF", "Port of Los Angeles", language, [line]) { PaymentTerms = "T/T 30% 선금", DeliveryTerms = "발주 후 4주", Notes = "표준 수출 포장\n납기 재확인", ValidUntil = new(2026, 12, 31), TaxType = QuoteTaxType.Exempt, SubtotalAmount = 250, ShippingAmount = 25, SupplyAmount = 275 }); var evidence = Path.Combine(AppContext.BaseDirectory, "quote-pdf-qa"); Directory.CreateDirectory(evidence); File.WriteAllBytes(Path.Combine(evidence, $"commercial-terms-{language}.pdf"), bytes); Assert.True(bytes.Length > 1000); Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4)); }
    }
}
