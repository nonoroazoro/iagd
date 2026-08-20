using IAGrim.Parsers.Arz;

namespace IAGrim.Tests.Parsers.Arz;

public sealed class LocalizationLoaderTests {
    [Fact]
    public void GetIaTranslationLoadsConfiguredLanguageAndDecodesNewlines() {
        var loader = new LocalizationLoader();

        var translation = loader.GetIaTranslation("ZH", "iatag_ui_safe_mode_running_body");

        Assert.NotNull(translation);
        Assert.Contains("\n", translation);
        Assert.DoesNotContain("\\n", translation);
    }
}
