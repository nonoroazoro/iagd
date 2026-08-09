using IAGrim.Database.Model;

namespace IAGrim.Services {
    internal static class GrimToolsItemMatcher {
        public static string? MatchRecordId(
            GrimToolsItemMetadata item,
            IReadOnlyList<ItemRecordMetadata> candidates) {
            if (string.IsNullOrEmpty(item.NameTag)) {
                return null;
            }

            var matches = candidates.Where(candidate =>
                    string.Equals(candidate.NameTag, item.NameTag, StringComparison.Ordinal)
                    && MatchesNumber(candidate.RequiredLevel, item.RequiredLevel)
                    && MatchesNumber(candidate.ItemLevel, item.ItemLevel)
                    && Matches(candidate.ItemClass, item.ItemClass)
                    && Matches(candidate.Rarity, item.Rarity)
                    && MatchesBitmap(candidate.Bitmap, item.Bitmap))
                .Take(2)
                .ToArray();
            return matches.Length == 1 ? matches[0].RecordId : null;
        }

        private static bool Matches<T>(T? actual, T? expected) {
            return expected == null || EqualityComparer<T>.Default.Equals(actual, expected);
        }

        private static bool MatchesNumber(double? actual, int? expected) {
            return expected == null || (actual ?? 0) == expected;
        }

        private static bool MatchesBitmap(string? actual, string? expected) {
            return expected == null || string.Equals(
                NormalizeBitmap(actual),
                NormalizeBitmap(expected),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string? NormalizeBitmap(string? bitmap) {
            if (string.IsNullOrEmpty(bitmap)) {
                return bitmap;
            }

            var normalized = bitmap.Replace('\\', '/');
            var extension = normalized.LastIndexOf('.');
            return extension < 0 ? normalized : normalized.Substring(0, extension);
        }
    }
}
