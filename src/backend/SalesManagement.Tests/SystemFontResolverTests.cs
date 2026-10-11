using SalesManagement.Api.Quotations;

namespace SalesManagement.Tests;

public sealed class SystemFontResolverTests
{
    [Theory]
    [InlineData(false, "NanumGothic.ttf")]
    [InlineData(true, "NanumGothicBold.ttf")]
    public void Loads_standalone_fonts_from_the_selected_directory(bool bold, string fileName)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"switch-korea-fonts-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var expected = new byte[] { 0, 1, 0, 0, 10, 20, (byte)(bold ? 1 : 0) };
            File.WriteAllBytes(Path.Combine(directory, fileName), expected);
            var resolver = new SystemFontResolver(directory);
            var face = resolver.ResolveTypeface(SystemFontResolver.FamilyName, bold, false)!;
            Assert.Equal(expected, resolver.GetFont(face.FaceName));
        }
        finally { Directory.Delete(directory, true); }
    }
}
