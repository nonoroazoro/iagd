using System.Collections.Generic;
using IAGrim.Services.Dto;

namespace IAGrim.Services.ItemStats;

/// <summary>
/// Retains the raw records required to calculate roll boundaries on demand.
/// </summary>
public sealed class ItemRollSource
{
    public ItemRollSource(
        List<DBStatRow> baseRows,
        List<DBStatRow> prefixRows,
        List<DBStatRow> suffixRows,
        IReadOnlyDictionary<string, double> rolledStats)
    {
        BaseRows = baseRows;
        PrefixRows = prefixRows;
        SuffixRows = suffixRows;
        RolledStats = rolledStats;
    }

    public List<DBStatRow> BaseRows { get; }
    public List<DBStatRow> PrefixRows { get; }
    public List<DBStatRow> SuffixRows { get; }
    public IReadOnlyDictionary<string, double> RolledStats { get; }
}