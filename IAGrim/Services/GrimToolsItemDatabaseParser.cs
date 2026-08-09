using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace IAGrim.Services {
    internal sealed class GrimToolsItemDatabaseParser {
        private const string AllItemsMarker = "window.allItems=";
        private const string ShortNameMappingMarker = "window.shortNameMapping=";
        private readonly string _script;
        private readonly string _shortNameMapping;
        private readonly int _allItemsStart;
        private readonly int _allItemsEnd;

        public GrimToolsItemDatabaseParser(string script) {
            _script = script;
            _shortNameMapping = ReadMarkedObject(script, ShortNameMappingMarker);
            _allItemsStart = FindObjectStart(script, AllItemsMarker);
            _allItemsEnd = FindObjectEnd(script, _allItemsStart);
            if (_allItemsEnd < 0) {
                throw new InvalidDataException("GrimTools item data is incomplete.");
            }
        }

        public IReadOnlyDictionary<string, GrimToolsItemMetadata> ReadItems(IEnumerable<string> itemIds) {
            var itemNameTag = ReadRequiredString(_shortNameMapping, "itemNameTag");
            var description = ReadRequiredString(_shortNameMapping, "description");
            var levelRequirement = ReadRequiredString(_shortNameMapping, "levelRequirement");
            var itemClass = ReadRequiredString(_shortNameMapping, "Class");
            var bitmap = ReadRequiredString(_shortNameMapping, "bitmap");
            var itemClassification = ReadRequiredString(_shortNameMapping, "itemClassification");
            var result = new Dictionary<string, GrimToolsItemMetadata>(StringComparer.Ordinal);

            foreach (var itemId in itemIds.Distinct(StringComparer.Ordinal)) {
                var item = ReadItemObject(itemId);
                if (item == null) {
                    continue;
                }

                var classCode = ReadString(item, itemClass);
                result[itemId] = new GrimToolsItemMetadata {
                    ItemId = itemId,
                    NameTag = ReadString(item, itemNameTag) ?? ReadString(item, description),
                    RequiredLevel = ReadInt(item, levelRequirement),
                    ItemLevel = ReadInt(item, "itemLevel"),
                    ItemClass = classCode == null ? null : ReadMappedName(classCode),
                    Rarity = ReadString(item, itemClassification),
                    Bitmap = ReadString(item, bitmap)
                };
            }

            return result;
        }

        private string? ReadItemObject(string itemId) {
            var marker = itemId + ":{";
            var markerIndex = _script.IndexOf(marker, _allItemsStart, StringComparison.Ordinal);
            if (markerIndex < 0 || markerIndex >= _allItemsEnd) {
                return null;
            }

            var start = markerIndex + itemId.Length + 1;
            var end = FindObjectEnd(_script, start);
            return end > start && end <= _allItemsEnd
                ? _script.Substring(start, end - start + 1)
                : null;
        }

        private string? ReadMappedName(string shortName) {
            var pattern = $@"(?:^|[,{{])(?<name>[A-Za-z0-9_]+):\x22{Regex.Escape(shortName)}\x22";
            var match = Regex.Match(_shortNameMapping, pattern, RegexOptions.CultureInvariant);
            return match.Success ? match.Groups["name"].Value : null;
        }

        private static string ReadMarkedObject(string script, string marker) {
            var start = FindObjectStart(script, marker);
            var end = FindObjectEnd(script, start);
            if (end < 0) {
                throw new InvalidDataException($"GrimTools item data does not define {marker}.");
            }

            return script.Substring(start, end - start + 1);
        }

        private static int FindObjectStart(string script, string marker) {
            var markerIndex = script.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0) {
                throw new InvalidDataException($"GrimTools item data does not define {marker}.");
            }

            var start = script.IndexOf('{', markerIndex + marker.Length);
            if (start < 0) {
                throw new InvalidDataException($"GrimTools item data does not define {marker}.");
            }

            return start;
        }

        private static int FindObjectEnd(string script, int start) {
            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var index = start; index < script.Length; index++) {
                var character = script[index];
                if (inString) {
                    if (escaped) {
                        escaped = false;
                    }
                    else if (character == '\\') {
                        escaped = true;
                    }
                    else if (character == '"') {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"') {
                    inString = true;
                }
                else if (character == '{') {
                    depth++;
                }
                else if (character == '}' && --depth == 0) {
                    return index;
                }
            }

            return -1;
        }

        private static string ReadRequiredString(string item, string propertyName) {
            return ReadString(item, propertyName)
                ?? throw new InvalidDataException($"GrimTools item metadata does not define {propertyName}.");
        }

        private static string? ReadString(string item, string propertyName) {
            var value = ReadTopLevelProperty(item, propertyName);
            return value?.StartsWith('"') == true ? JsonConvert.DeserializeObject<string>(value) : null;
        }

        private static int? ReadInt(string item, string propertyName) {
            var rawValue = ReadTopLevelProperty(item, propertyName);
            return int.TryParse(rawValue, out var value) ? value : null;
        }

        private static string? ReadTopLevelProperty(string item, string propertyName) {
            var depth = 0;
            var inString = false;
            var escaped = false;

            for (var index = 0; index < item.Length; index++) {
                var character = item[index];
                if (inString) {
                    if (escaped) {
                        escaped = false;
                    }
                    else if (character == '\\') {
                        escaped = true;
                    }
                    else if (character == '"') {
                        inString = false;
                    }

                    continue;
                }

                if (character == '"') {
                    inString = true;
                    continue;
                }

                if (character == '{') {
                    depth++;
                    continue;
                }

                if (character == '}') {
                    depth--;
                    continue;
                }

                var previous = index - 1;
                while (previous >= 0 && char.IsWhiteSpace(item[previous])) {
                    previous--;
                }

                var isPropertyStart = depth == 1
                    && previous >= 0
                    && (item[previous] == '{' || item[previous] == ',');
                if (!isPropertyStart || !item.AsSpan(index).StartsWith(propertyName, StringComparison.Ordinal)) {
                    continue;
                }

                var valueStart = index + propertyName.Length;
                if (valueStart >= item.Length || item[valueStart] != ':') {
                    continue;
                }

                valueStart++;
                while (valueStart < item.Length && char.IsWhiteSpace(item[valueStart])) {
                    valueStart++;
                }

                if (valueStart >= item.Length) {
                    return null;
                }

                if (item[valueStart] == '"') {
                    escaped = false;
                    for (var valueEnd = valueStart + 1; valueEnd < item.Length; valueEnd++) {
                        if (escaped) {
                            escaped = false;
                        }
                        else if (item[valueEnd] == '\\') {
                            escaped = true;
                        }
                        else if (item[valueEnd] == '"') {
                            return item.Substring(valueStart, valueEnd - valueStart + 1);
                        }
                    }

                    return null;
                }

                var scalarEnd = item.IndexOfAny([',', '}'], valueStart);
                return scalarEnd < 0 ? null : item.Substring(valueStart, scalarEnd - valueStart);
            }

            return null;
        }
    }
}
