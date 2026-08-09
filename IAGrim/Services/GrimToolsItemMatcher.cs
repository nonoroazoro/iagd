using Newtonsoft.Json.Linq;

namespace IAGrim.Services {
    internal static class GrimToolsItemMatcher {
        public static string? MatchRecordId(
            GrimToolsItemMetadata item,
            IReadOnlyList<JToken> gdItems) {
            if (string.IsNullOrEmpty(item.NameTag)) {
                return null;
            }

            var candidates = gdItems.Where(candidate =>
                    string.Equals(candidate.Value<string>("nameTag"), item.NameTag, StringComparison.Ordinal)
                    && Matches(candidate.Value<int?>("requiredLevel"), item.RequiredLevel)
                    && Matches(candidate.Value<int?>("itemLevel"), item.ItemLevel)
                    && Matches(candidate.Value<string>("itemClass"), item.ItemClass)
                    && Matches(candidate.Value<string>("rarity"), item.Rarity)
                    && MatchesBitmap(candidate.Value<string>("bitmap"), item.Bitmap))
                .Take(2)
                .ToArray();
            return candidates.Length == 1 ? candidates[0].Value<string>("recordId") : null;
        }

        private static bool Matches<T>(T? actual, T? expected) {
            return expected == null || EqualityComparer<T>.Default.Equals(actual, expected);
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
