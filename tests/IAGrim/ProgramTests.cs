namespace IAGrim.Tests;

public sealed class ProgramTests {
    [Theory]
    [InlineData(false, "EN")]
    [InlineData(true, "ZH")]
    public void ResolveItemLanguageCodeUsesParsedGameData(
        bool hasChineseGameData,
        string expected) {
        var result = Program.ResolveItemLanguageCode(
            string.Empty,
            hasChineseGameData);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveItemLanguageCodeKeepsParsedLanguage() {
        var result = Program.ResolveItemLanguageCode("ZH", false);

        Assert.Equal("ZH", result);
    }

    [Theory]
    [InlineData("EN", "EN")]
    [InlineData("ZH", "ZH")]
    [InlineData("", "EN")]
    [InlineData("DE", "EN")]
    public void ResolveRequestedItemLanguageCodeUsesSupportedUiLanguage(string uiLanguageCode, string expected) {
        var result = Program.ResolveRequestedItemLanguageCode(uiLanguageCode);

        Assert.Equal(expected, result);
    }
}
