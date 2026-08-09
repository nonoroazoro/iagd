using IAGrim.Parsers.Arz;

namespace IAGrim.Tests.Parsers.Arz;

public sealed class LanguageMappingTests {
    [Fact]
    public void GetSupportedUiLanguagesIncludesBundledLanguages() {
        var languages = LanguageMapping.GetSupportedUiLanguages();

        Assert.Equal(["EN", "ZH"], languages);
    }
}
