using IAGrim.Parsers.GameDataParsing.Service;

namespace IAGrim.Tests.Parsers.GameDataParsing.Service;

public sealed class ParsingServiceTests {
    [Theory]
    [InlineData("EN", false, "EN")]
    [InlineData("en", false, "en")]
    [InlineData("ZH", true, "ZH")]
    [InlineData("ZH", false, "EN")]
    [InlineData("", false, "EN")]
    public void ResolveAvailableLanguageCodeFallsBackToEnglish(
        string requestedLanguageCode,
        bool languageArchiveExists,
        string expected) {
        var result = ParsingService.ResolveAvailableLanguageCode(
            requestedLanguageCode,
            languageArchiveExists);

        Assert.Equal(expected, result);
    }
}
