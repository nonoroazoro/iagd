using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using IAGrim.Database.Interfaces;
using Newtonsoft.Json.Linq;

namespace IAGrim.Services {
    public sealed class GrimToolsBuildService {
        private static readonly Uri _grimToolsRoot = new Uri("https://www.grimtools.com/");
        private static readonly HttpClient _httpClient = CreateHttpClient();
        private static readonly Regex _buildIdPattern = new Regex("^[A-Za-z0-9]{8}$", RegexOptions.Compiled);
        private static readonly Regex _buildUrlPattern = new Regex(
            "^https://(?:www\\.)?grimtools\\.com/calc/([A-Za-z0-9]{8})/?(?:[?#].*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private readonly IDatabaseItemDao _databaseItemDao;
        private readonly SemaphoreSlim _itemDatabaseLock = new SemaphoreSlim(1, 1);
        private string? _itemDatabaseScript;

        public GrimToolsBuildService(IDatabaseItemDao databaseItemDao) {
            _databaseItemDao = databaseItemDao;
        }

        public async Task<GrimToolsBuildResult> ResolveBaseRecordsAsync(string input, CancellationToken cancellationToken) {
            var buildId = ParseBuildId(input);
            if (buildId == null) {
                throw new ArgumentException("Invalid GrimTools build URL or ID.", nameof(input));
            }

            var equipmentIds = await LoadEquipmentIdsAsync(buildId, cancellationToken).ConfigureAwait(false);
            if (equipmentIds.Count == 0) {
                return new GrimToolsBuildResult([], 0, 0);
            }

            var itemDatabase = await GetItemDatabaseScriptAsync(cancellationToken).ConfigureAwait(false);
            var metadata = new GrimToolsItemDatabaseParser(itemDatabase)
                .ReadItems(equipmentIds);
            var nameTags = metadata.Values
                .Select(item => item.NameTag)
                .Where(nameTag => !string.IsNullOrEmpty(nameTag))
                .Select(nameTag => nameTag ?? string.Empty)
                .ToArray();
            var candidates = _databaseItemDao.GetItemMetadataByNameTags(nameTags);
            var records = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var resolvedIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var item in metadata.Values) {
                var recordId = GrimToolsItemMatcher.MatchRecordId(item, candidates);
                if (string.IsNullOrEmpty(recordId)) {
                    continue;
                }

                records.Add(recordId);
                resolvedIds.Add(item.ItemId);
            }

            return new GrimToolsBuildResult(
                records.OrderBy(record => record, StringComparer.Ordinal).ToArray(),
                equipmentIds.Count,
                equipmentIds.Count(resolvedIds.Contains));
        }

        private static string? ParseBuildId(string input) {
            var value = input.Trim();
            if (_buildIdPattern.IsMatch(value)) {
                return value;
            }

            var match = _buildUrlPattern.Match(value);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static async Task<List<string>> LoadEquipmentIdsAsync(string buildId, CancellationToken cancellationToken) {
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_grimToolsRoot, $"load_build.php?id={buildId}"));
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                ["token"] = "-",
                ["mod"] = string.Empty
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var equipment = JObject.Parse(json).SelectToken("data.equipment") as JObject;
            if (equipment == null) {
                return [];
            }

            return equipment.Properties()
                .Select(property => property.Value.Value<string>("item"))
                .Where(itemId => !string.IsNullOrEmpty(itemId) && itemId.StartsWith("it", StringComparison.Ordinal))
                .Select(itemId => itemId ?? string.Empty)
                .ToList();
        }

        private async Task<string> GetItemDatabaseScriptAsync(CancellationToken cancellationToken) {
            if (_itemDatabaseScript != null) {
                return _itemDatabaseScript;
            }

            await _itemDatabaseLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try {
                if (_itemDatabaseScript == null) {
                    _itemDatabaseScript = await _httpClient.GetStringAsync(
                        new Uri(_grimToolsRoot, "db/itemdb/itemdb.js"), cancellationToken).ConfigureAwait(false);
                }

                return _itemDatabaseScript;
            }
            finally {
                _itemDatabaseLock.Release();
            }
        }

        private static HttpClient CreateHttpClient() {
            var client = new HttpClient {
                Timeout = TimeSpan.FromSeconds(30)
            };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IAGrim", "1.0"));
            return client;
        }
    }
}
