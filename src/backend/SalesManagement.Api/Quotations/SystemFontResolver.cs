using PdfSharp.Fonts;
namespace SalesManagement.Api.Quotations;
public sealed class SystemFontResolver(string? fontDirectory = null) : IFontResolver
{
    public const string FamilyName = "SwitchKoreaKorean"; private const string Regular = "switch-korea-regular"; private const string Bold = "switch-korea-bold";
    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic) => new(isBold ? Bold : Regular, false, isItalic);
    public byte[]? GetFont(string faceName) => File.ReadAllBytes(PathFor(faceName == Bold));
    private string PathFor(bool bold)
    {
        var directory = string.IsNullOrWhiteSpace(fontDirectory)
            ? (OperatingSystem.IsWindows() ? @"C:\Windows\Fonts" : "/usr/share/fonts/truetype/nanum")
            : Path.GetFullPath(fontDirectory);
        // PDFsharp cannot embed TTC collections. Use standalone TrueType faces.
        var names = OperatingSystem.IsWindows() && string.IsNullOrWhiteSpace(fontDirectory)
            ? new[] { bold ? "malgunbd.ttf" : "malgun.ttf" }
            : new[] { bold ? "NanumGothicBold.ttf" : "NanumGothic.ttf" };
        var candidates = names.Select(name => Path.Combine(directory, name));
        return candidates.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("국문 PDF용 맑은 고딕 또는 나눔고딕 단독 TTF 글꼴을 찾을 수 없습니다.");
    }
}
