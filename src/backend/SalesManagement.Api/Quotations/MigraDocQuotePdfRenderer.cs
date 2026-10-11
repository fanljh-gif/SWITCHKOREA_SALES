using MigraDoc.DocumentObjectModel; using MigraDoc.DocumentObjectModel.Tables; using MigraDoc.Rendering; using PdfSharp.Fonts; using SalesManagement.Core.Models; using SalesManagement.Core.Quotations;
namespace SalesManagement.Api.Quotations;
public sealed class MigraDocQuotePdfRenderer : IQuotePdfRenderer
{
    private static readonly object FontGate = new(); private static bool _fontReady;
    public byte[] Render(QuotePdfModel model)
    {
        EnsureFont(); var ko = model.Language == QuoteLanguage.Ko; var document = new Document(); document.Info.Title = $"{model.QuoteNumber} {model.RevisionLabel}"; var normal = document.Styles[StyleNames.Normal]!; normal.Font.Name = SystemFontResolver.FamilyName; normal.Font.Size = 9;
        var section = document.AddSection(); var title = section.AddParagraph(ko ? "견 적 서" : "QUOTATION"); title.Format.Alignment = ParagraphAlignment.Center; title.Format.Font.Size = 20; title.Format.Font.Bold = true; title.Format.SpaceAfter = Unit.FromCentimeter(.7);
        AddLine(section, ko ? "상호" : "Company", "SWITCH KOREA"); AddLine(section, ko ? "견적번호" : "Quote No.", $"{model.QuoteNumber} {model.RevisionLabel}"); AddLine(section, ko ? "고객사" : "Customer", model.CustomerName); AddLine(section, ko ? "거래조건" : "Terms", model.IncotermCode is null ? (ko ? "국내" : "Domestic") : $"{model.IncotermCode} · {model.NamedPlace}");
        if (model.ValidUntil.HasValue) AddLine(section, ko ? "유효기간" : "Valid Until", model.ValidUntil.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        AddLine(section, ko ? "세금구분" : "Tax Treatment", model.TaxType switch { QuoteTaxType.Inclusive => ko ? "부가세 포함" : "VAT inclusive", QuoteTaxType.Exempt => ko ? "면세" : "Tax exempt", _ => ko ? "부가세 별도" : "VAT exclusive" });
        var table = section.AddTable(); table.Borders.Width = .5;
        foreach (var width in new[] { 1.8, 6.0, 2.0, 2.6, 3.0 }) table.AddColumn(Unit.FromCentimeter(width));
        var header = table.AddRow(); header.HeadingFormat = true; header.Shading.Color = Colors.LightGray;
        var labels = ko ? new[] { "품목", "설명", "수량", "단가", "금액" } : new[] { "Item", "Description", "Qty", "Unit Price", "Amount" };
        for (var i = 0; i < labels.Length; i++) Cell(header, i, labels[i], true);
        foreach (var line in model.Lines) { var row = table.AddRow(); Cell(row, 0, line.ProductId.ToString("N")[..8]); Cell(row, 1, line.Description); Cell(row, 2, line.Quantity.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)); Cell(row, 3, Money(model.Currency, line.UnitPrice)); Cell(row, 4, Money(model.Currency, line.Amount)); }
        AddLine(section, ko ? "품목 합계" : "Subtotal", Money(model.Currency, model.SubtotalAmount));
        AddLine(section, ko ? "할인" : "Discount", Money(model.Currency, model.DiscountAmount));
        AddLine(section, ko ? "운송비" : "Shipping", Money(model.Currency, model.ShippingAmount));
        AddLine(section, ko ? "기타 비용" : "Other Charges", Money(model.Currency, model.OtherAmount));
        AddLine(section, ko ? "공급가액" : "Supply Amount", Money(model.Currency, model.SupplyAmount));
        AddLine(section, ko ? "세액" : "Tax", Money(model.Currency, model.TaxAmount));
        var total = section.AddParagraph($"{(ko ? "합계" : "TOTAL")}: {model.Currency} {model.TotalAmount:N2}"); total.Format.Alignment = ParagraphAlignment.Right; total.Format.Font.Size = 13; total.Format.Font.Bold = true; total.Format.SpaceBefore = Unit.FromCentimeter(.5);
        AddCommercialTerm(section, ko ? "결제조건" : "Payment Terms", model.PaymentTerms);
        AddCommercialTerm(section, ko ? "납기조건" : "Delivery Terms", model.DeliveryTerms);
        AddCommercialTerm(section, ko ? "비고" : "Notes", model.Notes);
        var renderer = new PdfDocumentRenderer { Document = document }; renderer.RenderDocument(); using var stream = new MemoryStream(); renderer.Save(stream, false); return stream.ToArray();
    }
    private static void EnsureFont() { lock (FontGate) { if (_fontReady) return; GlobalFontSettings.FontResolver = new SystemFontResolver(Environment.GetEnvironmentVariable("SWITCHKOREA_PDF_FONT_DIRECTORY")); _fontReady = true; } }
    private static void AddLine(Section section, string label, string value) { var p = section.AddParagraph(); p.AddFormattedText($"{label}: ", TextFormat.Bold); p.AddText(value); }
    private static void AddCommercialTerm(Section section, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var heading = section.AddParagraph(label);
        heading.Format.Font.Bold = true;
        heading.Format.SpaceBefore = Unit.FromCentimeter(.3);
        heading.Format.KeepWithNext = true;
        var paragraph = section.AddParagraph();
        var lines = value.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) paragraph.AddLineBreak();
            paragraph.AddText(lines[i]);
        }
    }
    private static void Cell(Row row, int index, string text, bool bold = false) { var p = row.Cells[index].AddParagraph(text); p.Format.Font.Bold = bold; }
    private static string Money(Currency currency, decimal amount) => $"{currency} {amount.ToString(currency == Currency.KRW ? "N0" : "N2", System.Globalization.CultureInfo.InvariantCulture)}";
}
