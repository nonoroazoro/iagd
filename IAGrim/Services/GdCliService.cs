using System.Diagnostics;
using System.Text;
using log4net;

namespace IAGrim.Services {
    public sealed class GdCliService {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(GdCliService));
        private static readonly TimeSpan _probeTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan _queryTimeout = TimeSpan.FromSeconds(30);
        private readonly Task<bool> _availability;

        public GdCliService() {
            _availability = ProbeAsync();
        }

        public bool IsAvailable => _availability.IsCompletedSuccessfully && _availability.Result;

        public Task<bool> GetAvailabilityAsync() {
            return _availability;
        }

        public async Task<string> GetItemsByNameTagsAsync(IReadOnlyCollection<string> nameTags, CancellationToken cancellationToken) {
            if (!await _availability.ConfigureAwait(false)) {
                throw new InvalidOperationException("gd-cli is not available.");
            }

            var safeTags = nameTags
                .Where(tag => tag.All(character => char.IsLetterOrDigit(character) || character == '_'))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (safeTags.Length == 0) {
                return "[]";
            }

            var condition = string.Join(" || ", safeTags.Select(tag => $"nameTag=='{tag}'"));
            var query = $"data[?{condition}].{{recordId: recordId, nameTag: nameTag, requiredLevel: requiredLevel, itemLevel: itemLevel, itemClass: itemClass, rarity: rarity, bitmap: (stats[?field=='bitmap']|[0].textValue) || (stats[?field=='artifactBitmap']|[0].textValue)}}";
            return await RunAsync(["--query", query, "items", "--all"], cancellationToken)
                .ConfigureAwait(false);
        }

        private static async Task<bool> ProbeAsync() {
            try {
                using var process = CreateProcess(["--help"]);
                if (!process.Start()) {
                    return false;
                }

                using var timeout = new CancellationTokenSource(_probeTimeout);
                var standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var standardError = process.StandardError.ReadToEndAsync(timeout.Token);
                try {
                    await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                    await Task.WhenAll(standardOutput, standardError).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeout.IsCancellationRequested) {
                    TryKill(process);
                    _logger.Warn("gd-cli probe timed out. GrimTools search is disabled.");
                    return false;
                }

                var available = process.ExitCode == 0;
                _logger.Info($"gd-cli probe completed. Available: {available}");
                return available;
            }
            catch (Exception ex) {
                _logger.Info("gd-cli is unavailable. GrimTools search is disabled.", ex);
                return false;
            }
        }

        private static async Task<string> RunAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken) {
            using var process = CreateProcess(arguments);
            using var timeout = new CancellationTokenSource(_queryTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

            try {
                if (!process.Start()) {
                    throw new GdCliQueryException("Unable to start gd-cli.");
                }

                var standardOutput = process.StandardOutput.ReadToEndAsync(linked.Token);
                var standardError = process.StandardError.ReadToEndAsync(linked.Token);
                await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
                var output = await standardOutput.ConfigureAwait(false);
                var error = await standardError.ConfigureAwait(false);

                if (process.ExitCode != 0) {
                    throw new GdCliQueryException(string.IsNullOrWhiteSpace(error)
                        ? $"gd-cli exited with code {process.ExitCode}."
                        : error.Trim());
                }

                return output;
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested) {
                TryKill(process);
                throw new GdCliQueryException("gd-cli query timed out.", ex);
            }
            catch (OperationCanceledException) {
                TryKill(process);
                throw;
            }
            catch (GdCliQueryException) {
                TryKill(process);
                throw;
            }
            catch (Exception ex) {
                TryKill(process);
                throw new GdCliQueryException("Unable to query gd-cli.", ex);
            }
        }

        private static Process CreateProcess(IReadOnlyList<string> arguments) {
            var startInfo = new ProcessStartInfo {
                FileName = "gd-cli",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            foreach (var argument in arguments) {
                startInfo.ArgumentList.Add(argument);
            }

            return new Process { StartInfo = startInfo };
        }

        private static void TryKill(Process process) {
            try {
                if (!process.HasExited) {
                    process.Kill(true);
                }
            }
            catch {
                // Process cleanup must not hide the original error or affect application startup.
            }
        }
    }
}
