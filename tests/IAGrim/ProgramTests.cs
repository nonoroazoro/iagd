namespace IAGrim.Tests;

public sealed class ProgramTests {
    [Theory]
    [InlineData(false, false, "EN")]
    [InlineData(true, false, "ZH")]
    [InlineData(false, true, "ZH")]
    public void ResolveItemLanguageCodeUsesAvailableGameData(
        bool hasChineseGameData,
        bool hasChineseGameArchive,
        string expected) {
        var result = Program.ResolveItemLanguageCode(
            string.Empty,
            hasChineseGameData,
            hasChineseGameArchive);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolveItemLanguageCodeKeepsParsedLanguage() {
        var result = Program.ResolveItemLanguageCode("ZH", false, false);

        Assert.Equal("ZH", result);
    }
}
