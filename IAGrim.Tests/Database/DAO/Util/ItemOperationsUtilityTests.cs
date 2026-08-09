using IAGrim.Database;
using IAGrim.Database.DAO.Util;

namespace IAGrim.Tests.Database.DAO.Util;

public class ItemOperationsUtilityTests {
    [Fact]
    public void MergeStackSize_GroupsDifferentAffixesByDuplicateIdentity() {
        var items = new[] {
            new PlayerItem {
                BaseRecord = "records/items/base.dbr",
                PrefixRecord = "records/items/prefix-a.dbr",
                SuffixRecord = "records/items/suffix-a.dbr",
                DuplicateIdentity = "Canonical Base Item",
                StackCount = 1
            },
            new PlayerItem {
                BaseRecord = "records/items/base.dbr",
                PrefixRecord = "records/items/prefix-b.dbr",
                SuffixRecord = "records/items/suffix-b.dbr",
                DuplicateIdentity = "Canonical Base Item",
                StackCount = 1
            }
        };

        var groups = ItemOperationsUtility.MergeStackSize(items, true);

        Assert.Single(groups);
        Assert.Equal(2, groups[0].Count);
    }
}
