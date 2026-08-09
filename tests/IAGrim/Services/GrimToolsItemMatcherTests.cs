using IAGrim.Database.Model;
using IAGrim.Services;

namespace IAGrim.Tests.Services {
    public sealed class GrimToolsItemMatcherTests {
        private static readonly GrimToolsItemMetadata _item = new GrimToolsItemMetadata {
            ItemId = "it100",
            NameTag = "tagHead",
            RequiredLevel = 94,
            ItemLevel = 94,
            ItemClass = "ArmorProtective_Head",
            Rarity = "Legendary",
            Bitmap = "items/gearhead/test.png"
        };

        [Fact]
        public void MatchRecordIdUsesAllStableFields() {
            ItemRecordMetadata[] candidates = [
                CreateCandidate("wrong-rarity", "Epic"),
                CreateCandidate("expected", "Legendary")
            ];

            var result = GrimToolsItemMatcher.MatchRecordId(_item, candidates);

            Assert.Equal("expected", result);
        }

        [Fact]
        public void MatchRecordIdRejectsAmbiguousCandidates() {
            ItemRecordMetadata[] candidates = [
                CreateCandidate("first", "Legendary"),
                CreateCandidate("second", "Legendary")
            ];

            var result = GrimToolsItemMatcher.MatchRecordId(_item, candidates);

            Assert.Null(result);
        }

        private static ItemRecordMetadata CreateCandidate(string recordId, string rarity) {
            return new ItemRecordMetadata {
                RecordId = recordId,
                NameTag = "tagHead",
                RequiredLevel = 94,
                ItemLevel = 94,
                ItemClass = "ArmorProtective_Head",
                Rarity = rarity,
                Bitmap = "items/gearhead/test.tex"
            };
        }
    }
}
