using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IAGrim.Parsers.Arz {
    /// <summary>
    /// Maps GD language codes (from Text_XX.arc filenames) to display names,
    /// and resolves bundled IA translation override files.
    /// </summary>
    public static class LanguageMapping {
        public static readonly Dictionary<string, string> CodeToDisplayName = new Dictionary<string, string> {
            { "EN", "English" },
            { "ZH", "简体中文" },
        };

        public static IReadOnlyList<string> GetSupportedUiLanguages() {
            return CodeToDisplayName.Keys.ToArray();
        }

        public static string GetDisplayName(string code) {
            return CodeToDisplayName.TryGetValue(code.ToUpperInvariant(), out var name) ? name : code;
        }

        /// <summary>
        /// Returns the path to the bundled IA translation override file, or null if it doesn't exist.
        /// </summary>
        public static string? GetIaTranslationFile(string code) {
            if (string.IsNullOrEmpty(code) || code.Equals("EN", System.StringComparison.OrdinalIgnoreCase))
                return null;

            var appDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? System.AppContext.BaseDirectory;
            var path = Path.Combine(appDir, "Resources", "translations", $"{code.ToLowerInvariant()}.txt");
            return File.Exists(path) ? path : null;
        }

    }
}
