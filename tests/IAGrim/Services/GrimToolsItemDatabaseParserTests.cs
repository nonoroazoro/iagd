using IAGrim.Services;

namespace IAGrim.Tests.Services {
    public sealed class GrimToolsItemDatabaseParserTests {
        private const string Mapping = "window.shortNameMapping={ArmorProtective_Head:\"c10\",ItemArtifact:\"c43\",itemNameTag:\"a\",description:\"d\",levelRequirement:\"k\",Class:\"l\",bitmap:\"n\",itemClassification:\"f\"};";

        [Fact]
        public void ReadItemsReadsTopLevelMetadataFromAllItems() {
            var script = "window.decoy={it100:{a:\"wrong\"}};"
                + "window.allItems={it100:{nested:{a:\"nested\"},\n k:94,n:\"items/gearhead/test.png\",f:\"Legendary\",a:\"tagHead\",l:\"c10\",itemLevel:94}};"
                + Mapping;

            var result = new GrimToolsItemDatabaseParser(script).ReadItems(["it100"])["it100"];

            Assert.Equal("tagHead", result.NameTag);
            Assert.Equal(94, result.RequiredLevel);
            Assert.Equal(94, result.ItemLevel);
            Assert.Equal("ArmorProtective_Head", result.ItemClass);
            Assert.Equal("Legendary", result.Rarity);
            Assert.Equal("items/gearhead/test.png", result.Bitmap);
        }

        [Fact]
        public void ReadItemsFallsBackToDescription() {
            var script = "window.allItems={it200:{d:\"tagRelic\",k:90,l:\"c43\",f:\"Legendary\",n:\"items/relic.png\",itemLevel:90}};"
                + Mapping;

            var result = new GrimToolsItemDatabaseParser(script).ReadItems(["it200"])["it200"];

            Assert.Equal("tagRelic", result.NameTag);
            Assert.Equal("ItemArtifact", result.ItemClass);
        }

        [Fact]
        public void ConstructorRejectsMissingAllItemsObject() {
            Assert.Throws<InvalidDataException>(() => new GrimToolsItemDatabaseParser(Mapping));
        }
    }
}
