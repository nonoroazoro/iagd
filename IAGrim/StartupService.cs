using EvilsoftCommons.Exceptions;
using IAGrim.Database;
using IAGrim.Database.Dto;
using IAGrim.Parsers.Arz;
using IAGrim.Parsers.GameDataParsing.Service;
using IAGrim.Settings;
using IAGrim.Utilities;
using IAGrim.Utilities.HelperClasses;
using log4net;
using NHibernate;
using StatTranslator;
using System.Diagnostics;
using System.Security.Principal;

namespace IAGrim {
    public class StartupService {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(StartupService));

        public void Init() {
            DateTime buildDate = ExceptionReporter.BuildDate;
            Logger.InfoFormat("Running version {0} from {1:dd/MM/yyyy}", ExceptionReporter.VersionString, buildDate);

            FileVersionInfo dllVersion = FileVersionInfo.GetVersionInfo(Path.Combine(Directory.GetCurrentDirectory(), "ItemAssistantHook_x64.dll"));

            Logger.InfoFormat($"DLL version version {dllVersion.FileVersion}");
            LogOptionalDllVersion("Playtest", "ItemAssistantHook_playtest_x64.dll");

            // Numeric compare: dllver.txt is written from the DLL's ProductVersion (zero-padded revision) while
            // FileVersion is a numeric win32 resource that can't carry the padding, so the same version can be
            // spelled two ways. A string compare here read a stale DLL as up to date whenever the revision widths
            // differed, which is exactly the "updated while GD was running" case this check exists to catch.
            var minimumDllVersion = File.ReadAllText("dllver.txt").Trim();
            if (VersionUtility.IsOlderThan(dllVersion.FileVersion, minimumDllVersion)) {
                Logger.Error($"The DLL version ({dllVersion.FileVersion}) is older than the required {minimumDllVersion}, did you perhaps run into a conflict while updating and clicked ignore?");
                Logger.Error("Item Assistant needs to be re-installed without GD running.");

                MessageBox.Show("IAGD install is corrupted.\nReinstall IAGD without GD running.", "Warning",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }


            if (!DependencyChecker.CheckVs2013Installed()) {
                MessageBox.Show("It appears VS 2013 (x86) redistributable is not installed.\nPlease install it to continue using IA",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            if (!DependencyChecker.CheckVs2010Installed()) {
                MessageBox.Show("It appears VS 2010 (x86) redistributable is not installed.\nPlease install it to continue using IA",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

        }

        /// <summary>
        /// Logs the version of a hook DLL which may not be present in every install (GD v1.2 / playtest builds).
        /// </summary>
        private static void LogOptionalDllVersion(string label, string filename) {
            var path = Path.Combine(Directory.GetCurrentDirectory(), filename);
            if (File.Exists(path)) {
                Logger.InfoFormat($"{label} DLL version {FileVersionInfo.GetVersionInfo(path).FileVersion}");
            }
            else {
                Logger.InfoFormat($"{label} DLL not present ({filename})");
            }
        }

        public static void PrintStartupInfo(SessionFactory factory, SettingsService settings) {
            try {
                Logger.Info(settings.GetLocal().StashToLootFrom == 0
                    ? "IA is configured to loot from the last stash page"
                    : $"IA is configured to loot from stash page #{settings.GetLocal().StashToLootFrom}");

                Logger.Info(settings.GetLocal().StashToDepositTo == 0
                    ? "IA is configured to deposit to the second-to-last stash page"
                    : $"IA is configured to deposit to stash page #{settings.GetLocal().StashToDepositTo}");

                using (ISession session = factory.OpenSession()) {
                    long numItemsStored = session.CreateCriteria<PlayerItem>()
                        .SetProjection(NHibernate.Criterion.Projections.RowCountInt64())
                        .UniqueResult<long>();

                    if (numItemsStored == 0)
                        Logger.Warn($"There are {numItemsStored} items stored in the database. <---- Unless you just installed IA, this is bad. No items.");
                    else
                        Logger.Info($"There are {numItemsStored} items stored in the database.");
                }


                Logger.Info("Transfer to any mod is " + (settings.GetPersistent().TransferAnyMod ? "enabled" : "disabled"));
                Logger.Info((new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator) ? "Running as administrator" : "Not running with low privileges");

                Logger.Info("There are items stored for the following mods:");

                foreach (ModSelection entry in new PlayerItemDaoImpl(factory, new DatabaseItemStatDaoImpl(factory))
                             .GetModSelection()) {
                    Logger.Info($"Mod: \"{entry.Mod}\", HC: {entry.IsHardcore}");
                }


                string gdPath = settings.GetLocal().CurrentGrimdawnLocation;
                Logger.Info(string.IsNullOrEmpty(gdPath)
                    ? "The path to Grim Dawn is unknown (not great)"
                    : $"The path to Grim Dawn is \"{gdPath}\"");

                Logger.Info($"Using IA on multiple PCs: {settings.GetPersistent().UsingDualComputer}");

                Logger.Info($"Logged into online backups: {!string.IsNullOrEmpty(settings.GetPersistent().CloudUser)}");
                Logger.Info($"Opted out of online backups: {settings.GetLocal().OptOutOfBackups}");



                using (ISession session = factory.OpenSession()) {
                    long num = session.CreateCriteria<DatabaseItem>()
                        .SetProjection(NHibernate.Criterion.Projections.RowCountInt64())
                        .UniqueResult<long>();

                    var isGdParsed = num > 0;
                    settings.GetLocal().IsGrimDawnParsed = isGdParsed;

                    if (isGdParsed) {
                        Logger.Info("The Grim Dawn database has been parsed");
                    }
                    else {
                        Logger.Warn("The Grim Dawn database has not been parsed");
                    }
                }

                Logger.Info("Startup data dump complete");
            }
            catch (Exception ex) {
                Logger.Error(ex.Message, ex);
                Logger.Error("IA may not function correctly");
            }
        }

        public static SettingsService LoadSettingsService() {
            return SettingsService.Load(GlobalPaths.SettingsFile);
        }

        /// <summary>
        /// Startup argument for resetting the settings that can leave IA impossible to find:
        /// a window position on a monitor that no longer exists, or a window hidden in the system tray.
        /// </summary>
        public const string SafeModeArgument = "--safe-mode";

        private const string _completeSettingsResetArgument = "--complete-settings-reset";
        private static bool _settingsResetRequested;
        private static readonly EnglishLanguage _englishUiLanguage = new EnglishLanguage(new Dictionary<string, string>());

        public static bool IsSafeMode(string[]? args) {
            return args?.Any(arg => SafeModeArgument.Equals(arg?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? false;
        }

        public static void ShowSafeModeAlreadyRunningMessage() {
            MessageBox.Show(
                GetConfiguredUiTag("iatag_ui_safe_mode_running_body"),
                GetConfiguredUiTag("iatag_ui_safe_mode_running_title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Restores the window related settings to their defaults
        /// </summary>
        public static void ResetWindowSettings(SettingsService settings) {
            Logger.Info("Safe mode: resetting window position, start minimized and minimize to tray.");

            settings.GetLocal().WindowPositionSettings = null;
            settings.GetLocal().StartMinimized = false;
            settings.GetPersistent().MinimizeToTray = false;
        }

        /// <summary>
        /// Requests a clean shutdown before resetting settings and restarting IA.
        /// </summary>
        public static void ResetSettingsAndRestart() {
            Logger.Info("Settings reset and restart requested");
            _settingsResetRequested = true;
            Application.Exit();
        }

        /// <summary>
        /// Starts a replacement process after the single-instance mutex is released.
        /// </summary>
        public static void CompleteSettingsResetAndRestart() {
            if (!_settingsResetRequested) {
                return;
            }

            try {
                Process.Start(new ProcessStartInfo {
                    FileName = Application.ExecutablePath,
                    Arguments = $"{_completeSettingsResetArgument} {Environment.ProcessId}",
                    UseShellExecute = true,
                });
            }
            catch (Exception ex) {
                Logger.Error("Could not reset settings and restart Item Assistant", ex);
                MessageBox.Show(GetConfiguredUiTag("iatag_ui_resetsettings_restart_error", ex.Message),
                    GetConfiguredUiTag("iatag_ui_resetsettings_error_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Waits for the old process to stop before deleting settings, preventing shutdown workers from
        /// recreating the file after it has been reset.
        /// </summary>
        public static bool CompletePendingSettingsReset(string[]? args) {
            var argumentIndex = Array.FindIndex(args ?? Array.Empty<string>(), arg =>
                _completeSettingsResetArgument.Equals(arg, StringComparison.OrdinalIgnoreCase));
            if (argumentIndex < 0) {
                return true;
            }

            if (args == null || argumentIndex + 1 >= args.Length ||
                !int.TryParse(args[argumentIndex + 1], out var parentProcessId)) {
                MessageBox.Show(GetConfiguredUiTag("iatag_ui_resetsettings_invalid_restart"),
                    GetConfiguredUiTag("iatag_ui_resetsettings_error_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try {
                try {
                    using var parentProcess = Process.GetProcessById(parentProcessId);
                    if (!parentProcess.WaitForExit(10000)) {
                        throw new TimeoutException("The previous Item Assistant process did not exit in time.");
                    }
                }
                catch (ArgumentException) {
                    // The previous process exited before the replacement process could inspect it.
                }

                Logger.Info($"Deleting {GlobalPaths.SettingsFile} on user request");
                File.Delete(GlobalPaths.SettingsFile);
                return true;
            }
            catch (Exception ex) {
                Logger.Error("Could not complete the settings reset", ex);
                MessageBox.Show(GetConfiguredUiTag("iatag_ui_resetsettings_complete_error", ex.Message),
                    GetConfiguredUiTag("iatag_ui_resetsettings_error_title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static string GetConfiguredUiTag(string tag, params object[] args) {
            var fallback = _englishUiLanguage.GetTag(tag, args);

            try {
                var languageCode = SettingsService.Load(GlobalPaths.SettingsFile).GetLocal().LanguageCode;
                var localized = new LocalizationLoader().GetIaTranslation(languageCode, tag);
                if (string.IsNullOrEmpty(localized)) {
                    return fallback;
                }

                for (var index = 0; index < args.Length; index++) {
                    localized = localized.Replace($"{{{index}}}", args[index]?.ToString());
                }

                return localized;
            }
            catch (Exception ex) {
                Logger.Warn($"Could not load UI translation '{tag}', using English", ex);
                return fallback;
            }
        }

        public static void PerformGrimUpdateCheck(SettingsService settingsService) {
            string? location = settingsService.GetLocal().GrimDawnLocation?.FirstOrDefault();
            long lastParsed = settingsService.GetLocal().GrimDawnLocationLastModified;

            if (Directory.Exists(location)) {
                if (lastParsed > 0) {
                    long lastModified = ParsingService.GetHighestTimestamp(location);

                    if (lastModified > lastParsed) {
                        if (!settingsService.GetLocal().HasWarnedGrimDawnUpdate) {
                            Logger.Info("Grim Dawn appears to have been updated since last parse, notifying end user.");
                            string message = RuntimeSettings.Language?.GetTag("iatag_ui_database_modified_body") ?? string.Empty;
                            string title = RuntimeSettings.Language?.GetTag("iatag_ui_database_modified_title") ?? string.Empty;
                            settingsService.GetLocal().HasWarnedGrimDawnUpdate = true;
                            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        else {
                            Logger.Debug("Grim Dawn appears to have been updated since last parse, end user previously notified.");
                        }
                    }
                    else {
                        Logger.Debug("Grim dawn appears unmodified since last run, database up to date.");
                    }
                }
                else {
                    Logger.Info("Last parsed entry for GD database is unset, skipping update check.");
                    settingsService.GetLocal().GrimDawnLocationLastModified = ParsingService.GetHighestTimestamp(location);
                }
            }
            else {
                Logger.Info("Grim dawn install is unset, skipping update check.");
            }
        }

        public void PerformIconCheck(GrimDawnDetector grimDawnDetector, SettingsService settings) {
            try {
                // Load the GD database (or mod, if any)
                string? gdPath = settings.GetLocal().CurrentGrimdawnLocation;

                if (string.IsNullOrEmpty(gdPath) || !Directory.Exists(gdPath)) {
                    gdPath = grimDawnDetector.GetGrimLocations().FirstOrDefault();
                }

                if (!string.IsNullOrEmpty(gdPath) && Directory.Exists(gdPath)) {
                    int numFiles = Directory.GetFiles(GlobalPaths.StorageFolder).Length;
                    int numFilesExpected = 2100;
                    bool missingLokarrIcons = false;

                    if (Directory.Exists(Path.Combine(gdPath, "gdx3"))) {
                        // Fangs of Asterkarn. A complete icon extraction is ~717 more than
                        // base+gdx1+gdx2; kept slightly lower as a conservative floor.
                        numFilesExpected += 660;
                    }

                    if (Directory.Exists(Path.Combine(gdPath, "gdx2"))) {
                        numFilesExpected += 850;
                    }

                    if (Directory.Exists(Path.Combine(gdPath, "gdx1"))) {
                        numFilesExpected += 890;

                        // Lokarr boots. Need a re-parse if missing.
                        if (!File.Exists(Path.Combine(GlobalPaths.StorageFolder, "sign_f01a_dif.tex.png"))) {
                            missingLokarrIcons = true;
                        }
                    }

                    if (numFiles >= numFilesExpected && !missingLokarrIcons) {
                        return;
                    }

                    Logger.Debug($"Only found {numFiles} in storage, expected ~{numFilesExpected}+, parsing item icons.");
                    ArzParser.QueueIconExtraction(gdPath, null);
                }
                else {
                    Logger.Warn("Could not find the Grim Dawn install location");
                }
            }
            catch (Exception ex) {
                // Keep things moving, if icons are messed up its unfortunate, items should still be accessible.
                Logger.Warn("Error parsing icons", ex);
            }
        }
    }
}
