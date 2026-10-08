using System.Text; using SalesManagement.Core.Quotations;
namespace SalesManagement.Api.Quotations;
public sealed class SimpleQuotePdfRenderer : IQuotePdfRenderer
{
    public byte[] Render(QuotePdfModel model)
    {
        static string Escape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        var title = model.Language == Core.Models.QuoteLanguage.Ko ? "SWITCH KOREA QUOTATION" : "SWITCH KOREA QUOTATION";
        var lines = new[] { title, $"No. {model.QuoteNumber} {model.RevisionLabel}", $"Customer: {model.CustomerName}", $"Total: {model.Currency} {model.TotalAmount:N2}", model.IncotermCode is null ? "Domestic" : $"{model.IncotermCode} {model.NamedPlace}" };
        var content = "BT /F1 12 Tf 50 780 Td " + string.Join(" Tj 0 -24 Td ", lines.Select(x => $"({Escape(x)})")) + " Tj ET";
        var objects = new[] { "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj", "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj", "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>endobj", "4 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj", $"5 0 obj<< /Length {Encoding.ASCII.GetByteCount(content)} >>stream\n{content}\nendstream endobj" };
        var builder = new StringBuilder("%PDF-1.4\n"); var offsets = new List<int> { 0 }; foreach (var item in objects) { offsets.Add(Encoding.ASCII.GetByteCount(builder.ToString())); builder.Append(item).Append('\n'); } var xref = Encoding.ASCII.GetByteCount(builder.ToString()); builder.Append("xref\n0 6\n0000000000 65535 f \n"); for (var i = 1; i <= 5; i++) builder.Append(offsets[i].ToString("0000000000")).Append(" 00000 n \n"); builder.Append($"trailer<< /Size 6 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF"); return Encoding.ASCII.GetBytes(builder.ToString());
    }
}
