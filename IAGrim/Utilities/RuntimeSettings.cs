using IAGrim.Parsers.Arz;
using IAGrim.UI;
using IAGrim.Utilities.HelperClasses;
using StatTranslator;

using log4net;

namespace IAGrim.Utilities {
    static class RuntimeSettings {
        public static void InitializeLanguage(string languageCode, string itemLanguageCode, Dictionary<string, string> dbTags) {
            Language = CreateLanguage(languageCode, dbTags);
            ItemLanguage = languageCode.Equals(itemLanguageCode, System.StringComparison.OrdinalIgnoreCase)
                ? Language
                : CreateLanguage(itemLanguageCode, dbTags);
            StatManager = ItemLanguage == null ? null : new StatManager(ItemLanguage);
        }

        private static ILocalizedLanguage CreateLanguage(string languageCode, Dictionary<string, string> dbTags) {
            var english = new EnglishLanguage(dbTags);
            if (string.IsNullOrEmpty(languageCode) || languageCode.Equals("EN", System.StringComparison.OrdinalIgnoreCase)) {
                return english;
            }

            return new LocalizationLoader().LoadLanguage(languageCode, dbTags, english);
        }

        public static string? Uuid { get; set; }

        public static ILocalizedLanguage? Language { get; private set; }
        public static ILocalizedLanguage? ItemLanguage { get; private set; }
        public static StatManager? StatManager { get; private set; }

    }
}
