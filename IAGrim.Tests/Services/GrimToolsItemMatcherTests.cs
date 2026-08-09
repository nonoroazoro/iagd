using IAGrim.Services;
using Newtonsoft.Json.Linq;

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
            var candidates = JArray.Parse("""
                [
                  {"recordId":"wrong-rarity","nameTag":"tagHead","requiredLevel":94,"itemLevel":94,"itemClass":"ArmorProtective_Head","rarity":"Epic","bitmap":"items/gearhead/test.tex"},
                  {"recordId":"expected","nameTag":"tagHead","requiredLevel":94,"itemLevel":94,"itemClass":"ArmorProtective_Head","rarity":"Legendary","bitmap":"items/gearhead/test.tex"}
                ]
                """).Children().ToArray();

            var result = GrimToolsItemMatcher.MatchRecordId(_item, candidates);

            Assert.Equal("expected", result);
        }

        [Fact]
        public void MatchRecordIdRejectsAmbiguousCandidates() {
            var candidates = JArray.Parse("""
                [
                  {"recordId":"first","nameTag":"tagHead","requiredLevel":94,"itemLevel":94,"itemClass":"ArmorProtective_Head","rarity":"Legendary","bitmap":"items/gearhead/test.tex"},
                  {"recordId":"second","nameTag":"tagHead","requiredLevel":94,"itemLevel":94,"itemClass":"ArmorProtective_Head","rarity":"Legendary","bitmap":"items/gearhead/test.tex"}
                ]
                """).Children().ToArray();

            var result = GrimToolsItemMatcher.MatchRecordId(_item, candidates);

            Assert.Null(result);
        }
    }
}
