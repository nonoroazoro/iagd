using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataAccess;
using EvilsoftCommons;
using IAGrim.Database;
using IAGrim.Database.Interfaces;
using IAGrim.Parser.Arc;
using IAGrim.Parsers.Arz;
using IAGrim.Parsers.GameDataParsing.Model;
using StatTranslator;
using log4net;

namespace IAGrim.Parsers.GameDataParsing.Service {
    class ArzParsingWrapper {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ArzParsingWrapper));
        private readonly ItemTagAccumulator _tagAccumulator = new ItemTagAccumulator();
        public List<DatabaseItem>? Items { get; private set; }

        public List<ItemTag> Tags => _tagAccumulator.Tags;

        public void LoadItems(
            List<string> arzFiles,
            ProgressTracker tracker
        ) {
            tracker.MaxValue = arzFiles.Count(File.Exists);

            // Developers can flip this switch to get a full dump of the GD database.
            // Setting it to true will cause the parsing to skip a lot of data that IA does not need.
            const bool skipIrrelevantStats = true;  // "skipLots"
            ItemAccumulator accumulator = new ItemAccumulator();
            try {
                foreach (string arzFile in arzFiles) {
                    if (File.Exists(arzFile)) {
                        Logger.Debug($"Parsing / Loading items from {arzFile}");
                        Parser.Arz.ArzParser.LoadItemRecords(arzFile, skipIrrelevantStats).ForEach(accumulator.Add);
                        tracker.Increment();
                    }
                    else {
                        Logger.Debug($"Ignoring non existing arz file {arzFile}");
                    }
                }
            }
            catch (ArgumentException ex) {
                Logger.Warn(ex.Message, ex);
                throw;
            }

            Items = accumulator.Items;
        }

        private void LoadTags(string file) {
            Logger.Debug($"Loading tags from {file}");

            List<IItemTag> tags = Parser.Arz.ArzParser.ParseArcFile(file);
            tags.ForEach(m => {
                if (m.Tag != null && m.Name != null) {
                    _tagAccumulator.Add(m.Tag, m.Name);
                }
            });
            Logger.Debug($"Loaded {tags.Count} tags from {file}");
        }

        /// <summary>
        /// Only the games own tags are stored. IA's bundled translation file is layered on top of these at
        /// startup (see LocalizationLoader.LoadLanguage) and must not be baked into the tag table -- the
        /// game is the authority on everything it defines, IA only fills in its own UI strings.
        /// </summary>
        public void LoadTags(
            List<string> tagfiles,
            ProgressTracker tracker
        ) {
            tracker.MaxValue = tagfiles.Count - tagfiles.Where(string.IsNullOrEmpty).Count();

            // Load tags in a prioritized order (EN first, then selected language arc — already ordered by caller)
            foreach (var tagfile in tagfiles) {
                if (File.Exists(tagfile)) {
                    Logger.Debug($"Loading tags from {tagfile}");
                    LoadTags(tagfile);
                }
                else {
                    Logger.Debug($"Ignoring non-existing tagfile {tagfile}");
                }

                tracker.Increment();
            }

            tracker.MaxProgress();
        }


        public void MapItemNames(ProgressTracker tracker) {
            if (Items == null)
                return;

            var tags = _tagAccumulator.MappedTags;
            var itemNameOrder = tags.TryGetValue("tagItemNameOrder", out var parsedItemNameOrder)
                && !string.IsNullOrWhiteSpace(parsedItemNameOrder)
                    ? parsedItemNameOrder
                    : EnglishLanguage.ItemNameOrderFallback;
            var itemNameCombinator = new ItemNameCombinator(itemNameOrder);

            string ResolveTag(string tag) {
                return !string.IsNullOrEmpty(tag) && tags.TryGetValue(tag, out var name)
                    ? name
                    : string.Empty;
            }

            tracker.MaxValue = Items.Count;

            Parallel.For(0, Items.Count, i => {
                var item = Items[i];
                if (!item.Slot.StartsWith("Loot")) {
                    var quality = ResolveTag(item.GetTag("itemQualityTag"));
                    var style = ResolveTag(item.GetTag("itemStyleTag"));
                    var name = ResolveTag(item.GetTag("itemNameTag", "description"));

                    Items[i].Name = itemNameCombinator.TranslateName(string.Empty, quality, style, name, string.Empty);
                    Items[i].NameLowercase = Items[i].Name?.ToLowerInvariant() ?? string.Empty;
                }

                tracker.Increment();

            });

            tracker.MaxProgress();
        }


        public void RenamePetStats(ProgressTracker tracker) {
            Logger.Debug("Detecting records with pet bonus stats..");

            if (Items == null)
                return;

            var petRecords = Items
                .SelectMany(m => (m.Stats ?? Enumerable.Empty<DatabaseItemStat>())
                    .Where(s => s.Stat == "petBonusName")
                    .Select(s => s.TextValue))
                .OfType<string>()
                .Where(record => !string.IsNullOrEmpty(record))
                .ToHashSet(StringComparer.Ordinal);

            var petItems = Items
                .Where(m => m.Record != null && petRecords.Contains(m.Record))
                .ToList();
            tracker.MaxValue = petItems.Count;
            foreach (var petItem in petItems) {
                var stats = (petItem.Stats ?? Enumerable.Empty<DatabaseItemStat>()).Select(s => new DatabaseItemStat {
                    Stat = "pet" + s.Stat,
                    TextValue = s.TextValue,
                    Value = s.Value,
                    Parent = s.Parent
                }).ToList();

                petItem.Stats?.Clear();
                petItem.Stats = stats;
                tracker.Increment();
            }

            tracker.MaxProgress();
            Logger.Debug($"Classified {petItems.Count} records as pet stats");
        }

        /// <summary>
        /// Primarily parses skills and maps item-skill
        /// </summary>
        /// <param name="itemSkillDao"></param>
        /// <param name="tracker"></param>
        public void ParseComplexItems(IItemSkillDao itemSkillDao, ProgressTracker tracker) {
            if (Items == null)
                return;

            var mappedTags = _tagAccumulator.MappedTags;
            var skillParser = new ComplexItemParser(Items, mappedTags);
            skillParser.Generate(tracker);
            itemSkillDao.Save(skillParser.Skills, false);
            itemSkillDao.Save(skillParser.SkillItemMapping, false);
        }


        public List<DatabaseItemStat> GenerateSpecialRecords(ProgressTracker tracker) {
            List<DatabaseItemStat> result = new List<DatabaseItemStat>();
            if (Items == null)
                return result;

            var skills = Items
                .Where(m => m.Record?.Contains("/skills/") ?? false)
                .ToList();

            var filtered = Items.Where(m => m.Id != 0).ToList();
            tracker.MaxValue = filtered.Count;
            foreach (var item in filtered) {
                ArzParser.GetSpecialMasteryStats(result, item, Items);
                ArzParser.GetSpecialSkillAugments(result, item, Items, skills, _tagAccumulator.MappedTags);
                tracker.Increment();
            }

            return result;
        }
    }
}
