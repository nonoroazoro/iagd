using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using EvilsoftCommons;
using EvilsoftCommons.Exceptions;
using IAGrim.Database.Interfaces;
using IAGrim.Parsers.GameDataParsing.Model;
using IAGrim.Parsers.GameDataParsing.UI;
using IAGrim.Utilities;
using log4net;

namespace IAGrim.Parsers.GameDataParsing.Service {
    public class ParsingService {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ParsingService));
        private string _grimdawnLocation;
        private string? _modLocation;

        private readonly IItemTagDao _itemTagDao;
        private readonly IDatabaseItemDao _databaseItemDao;
        private readonly IDatabaseItemStatDao _databaseItemStatDao;
        private readonly IItemSkillDao _itemSkillDao;
        private string _languageCode;
        public event EventHandler? OnParseComplete;

        public string LanguageCode => _languageCode;


        public ParsingService(
            IItemTagDao itemTagDao,
            string grimdawnLocation,
            IDatabaseItemDao databaseItemDao,
            IDatabaseItemStatDao databaseItemStatDao,
            IItemSkillDao itemSkillDao,
            string languageCode
        ) {
            _itemTagDao = itemTagDao;
            _grimdawnLocation = grimdawnLocation;
            _databaseItemDao = databaseItemDao;
            _databaseItemStatDao = databaseItemStatDao;
            _itemSkillDao = itemSkillDao;
            _languageCode = languageCode;
        }

        public static long GetHighestTimestamp(string install) {
            try {
                List<string> arzFiles = new List<string> {
                    GrimFolderUtility.FindArzFile(install)
                };

                foreach (string path in GrimFolderUtility.GetGrimExpansionFolders(install)) {
                    string expansionItems = GrimFolderUtility.FindArzFile(path);

                    if (!string.IsNullOrEmpty(expansionItems)) {
                        arzFiles.Add(expansionItems);
                    }
                }

                return arzFiles
                    .Select(File.GetLastWriteTimeUtc)
                    .Select(ts => ts.ToTimestamp())
                    .Max();
            }
            catch (Exception e) {
                Logger.Warn("Error fetching timestamp, defaulting to unchanged", e);
                return 0;
            }
        }

        public void Update(string install, string mod) {
            _grimdawnLocation = install;
            _modLocation = mod;
        }

        internal static string ResolveAvailableLanguageCode(string requestedLanguageCode, bool languageArchiveExists) {
            return requestedLanguageCode.Equals("EN", StringComparison.OrdinalIgnoreCase) || languageArchiveExists
                ? requestedLanguageCode
                : "EN";
        }

        public bool Execute() {
            string requestedArcFileName = $"text_{_languageCode.ToLowerInvariant()}.arc";
            string requestedLanguageTags = _languageCode.Equals("EN", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : GrimFolderUtility.FindArcFile(_grimdawnLocation, requestedArcFileName);
            string availableLanguageCode = ResolveAvailableLanguageCode(
                _languageCode,
                !string.IsNullOrEmpty(requestedLanguageTags));

            if (!availableLanguageCode.Equals(_languageCode, StringComparison.OrdinalIgnoreCase)) {
                Logger.Warn($"Grim Dawn language {_languageCode} is unavailable; falling back to English game data.");
            }

            _languageCode = availableLanguageCode;
            string arcFileName = $"text_{_languageCode.ToLowerInvariant()}.arc";

            // Always load English first as fallback, then overlay selected language
            List<string> tagfiles = new List<string>();

            // English tags first (fallback)
            string vanillaEnTags = GrimFolderUtility.FindArcFile(_grimdawnLocation, "text_en.arc");
            if (!string.IsNullOrEmpty(vanillaEnTags)) {
                tagfiles.Add(vanillaEnTags);
            }

            foreach (string path in GrimFolderUtility.GetGrimExpansionFolders(_grimdawnLocation)) {
                string expansionEnTags = GrimFolderUtility.FindArcFile(path, "text_en.arc");
                if (!string.IsNullOrEmpty(expansionEnTags)) {
                    tagfiles.Add(expansionEnTags);
                }
            }

            string modEnTags = string.IsNullOrEmpty(_modLocation) ? "" : GrimFolderUtility.FindArcFile(_modLocation, "text_en.arc");
            if (!string.IsNullOrEmpty(modEnTags)) {
                tagfiles.Add(modEnTags);
            }

            // Selected language overlay (if not English)
            if (!_languageCode.Equals("EN", StringComparison.OrdinalIgnoreCase)) {
                tagfiles.Add(requestedLanguageTags);

                foreach (string path in GrimFolderUtility.GetGrimExpansionFolders(_grimdawnLocation)) {
                    string expansionLangTags = GrimFolderUtility.FindArcFile(path, arcFileName);
                    if (!string.IsNullOrEmpty(expansionLangTags)) {
                        tagfiles.Add(expansionLangTags);
                    }
                }

                string modLangTags = string.IsNullOrEmpty(_modLocation) ? "" : GrimFolderUtility.FindArcFile(_modLocation, arcFileName);
                if (!string.IsNullOrEmpty(modLangTags)) {
                    tagfiles.Add(modLangTags);
                }
            }




            var baseArzFile = GrimFolderUtility.FindArzFile(_grimdawnLocation);
            if (string.IsNullOrEmpty(baseArzFile) || tagfiles.Count == 0) {
                Logger.Warn($"The selected Grim Dawn folder is missing required database or text archives: {_grimdawnLocation}");
                ShowInvalidGameDataMessage();
                return false;
            }

            List<string> arzFiles = new List<string> { baseArzFile };

            foreach (string path in GrimFolderUtility.GetGrimExpansionFolders(_grimdawnLocation)) {
                string expansionItems = GrimFolderUtility.FindArzFile(path);

                if (!string.IsNullOrEmpty(expansionItems)) {
                    arzFiles.Add(expansionItems);
                }
            }

            if (!string.IsNullOrEmpty(_modLocation)) {
                var modArzFile = GrimFolderUtility.FindArzFile(_modLocation);
                if (string.IsNullOrEmpty(modArzFile)) {
                    Logger.Warn($"The selected mod folder is missing its database archive: {_modLocation}");
                    ShowInvalidGameDataMessage();
                    return false;
                }

                arzFiles.Add(modArzFile);
            }

            var form = new ParsingDatabaseProgressView();
            var parser = new ArzParsingWrapper();
            var succeeded = false;

            // Invoke the background thread & show progress UI
            Thread t = new Thread(() => {
                ExceptionReporter.EnableLogUnhandledOnThread();

                try {
                    ExecuteParse(parser, form, tagfiles, arzFiles);
                    succeeded = true;
                }
                catch (IOException ex) {
                    // Grim Dawn itself does not block us from reading its files, but Steam mid-update (or antivirus) can
                    Logger.Warn($"Unable to read the Grim Dawn game files (HResult 0x{ex.HResult:X8}): {ex.Message}", ex);
                    ShowGameFilesInUseMessage();
                }
                catch (Exception ex) {
                    Logger.Error("Unable to parse the Grim Dawn game data.", ex);
                    ShowInvalidGameDataMessage();
                }
                finally {
                    try {
                        form.Invoke(() => form.OverrideClose());
                    }
                    catch (Exception ex) {
                        Logger.Warn("Error closing the parsing progress window: " + ex.Message, ex);
                    }
                }
            });

            t.IsBackground = true;
            t.Name = "DatabaseParsing";
            form.Shown += (_, _) => t.Start();
            form.ShowDialog();

            if (succeeded) {
                OnParseComplete?.Invoke(this, EventArgs.Empty);
            }

            return succeeded;
        }

        private void ShowGameFilesInUseMessage() {
            var message = RuntimeSettings.Language?.GetTag("iatag_ui_gamefiles_in_use");
            if (string.IsNullOrEmpty(message)) {
                message = "Unable to read the Grim Dawn game files, they are in use by another program.\n"
                          + "If Steam is currently updating or verifying Grim Dawn, please wait for it to finish and try again.";
            }

            MessageBox.Show(message, "Grim Dawn files in use", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ShowInvalidGameDataMessage() {
            var message = RuntimeSettings.Language?.GetTag("iatag_ui_corrupted");
            if (string.IsNullOrEmpty(message)) {
                message = "Unable to parse the Grim Dawn game data. Please verify the installation and try again.";
            }

            var title = RuntimeSettings.Language?.GetTag("iatag_ui_db_invalidlocation_title") ?? "Invalid game data";
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ExecuteParse(
            ArzParsingWrapper parser,
            ParsingDatabaseProgressView form,
            List<string> tagfiles,
            List<string> arzFiles
        ) {
            parser.LoadTags(tagfiles, new WinformsProgressBar(form.LoadingTags).Tracker);
            parser.LoadItems(arzFiles, new WinformsProgressBar(form.LoadingItems).Tracker);
            parser.MapItemNames(new WinformsProgressBar(form.MappingItemNames).Tracker);
            parser.RenamePetStats(new WinformsProgressBar(form.MappingPetStats).Tracker);

            if (parser.Items == null || parser.Items.Count == 0 || parser.Tags.Count == 0) {
                throw new InvalidOperationException("The parsed Grim Dawn data contains no items or tags.");
            }

            // Do not clear the existing parsed database until all game files have been read successfully.
            _databaseItemDao.Clean();
            _itemTagDao.Save(parser.Tags, new WinformsProgressBar(form.SavingTags).Tracker);
            _databaseItemDao.Save(parser.Items ?? [], new WinformsProgressBar(form.SavingItems).Tracker);
            _databaseItemDao.CreateItemIndexes(new WinformsProgressBar(form.IndexingItems).Tracker);

            // TODO: This depends on the DB item name.. which is in english, not localized
            {
                var records = parser.GenerateSpecialRecords(new WinformsProgressBar(form.GeneratingSpecialStats).Tracker);
                _databaseItemStatDao.Save(records, new WinformsProgressBar(form.SavingSpecialStats).Tracker);
            };


            parser.ParseComplexItems(_itemSkillDao, new WinformsProgressBar(form.GeneratingSkills).Tracker);
            {
                var tracker = new WinformsProgressBar(form.SkillCorrectnessCheck).Tracker;
                tracker.MaxValue = 1;
                _itemSkillDao.EnsureCorrectSkillRecords();
                tracker.MaxProgress();
            };
        }
    }
}
