using System;
using System.Collections.Generic;
using System.Linq;
using GrimDawnItemStats;
using IAGrim.Services.Dto;

namespace IAGrim.Services.ItemStats;

/// <summary>
/// Bridges raw database rows to the seed stat engine.
/// </summary>
internal static class SeedStatCalculator
{
    public static IReadOnlyDictionary<string, double>? Compute(
        List<DBStatRow> baseRows,
        List<DBStatRow> prefixRows,
        List<DBStatRow> suffixRows,
        uint seed)
    {
        if (seed == 0 || baseRows == null || baseRows.Count == 0)
            return null;

        var result = ItemStatEngine.Compute(
            ToInputStats(baseRows),
            seed,
            prefixStats: prefixRows != null && prefixRows.Count > 0 ? ToInputStats(prefixRows) : null,
            suffixStats: suffixRows != null && suffixRows.Count > 0 ? ToInputStats(suffixRows) : null);

        return result.UnmodeledFields.Count > 0 ? null : ExtractStats(result);
    }

    public static IReadOnlyDictionary<string, StatRange>? ComputeRanges(ItemRollSource source)
    {
        var result = ItemStatEngine.ComputeRange(
            ToInputStats(source.BaseRows),
            prefixStats: source.PrefixRows.Count > 0 ? ToInputStats(source.PrefixRows) : null,
            suffixStats: source.SuffixRows.Count > 0 ? ToInputStats(source.SuffixRows) : null);

        if (result.Minimum.UnmodeledFields.Count > 0 || result.Maximum.UnmodeledFields.Count > 0)
            return null;

        var minimum = ExtractStats(result.Minimum);
        var maximum = ExtractStats(result.Maximum);
        var ranges = new Dictionary<string, StatRange>(StringComparer.Ordinal);

        foreach (var field in minimum.Keys.Intersect(maximum.Keys))
        {
            double lower = Math.Min(minimum[field], maximum[field]);
            double upper = Math.Max(minimum[field], maximum[field]);
            if (Math.Abs(upper - lower) > 0.000001)
                ranges[field] = new StatRange(lower, upper);
        }

        return ranges;
    }

    private static Dictionary<string, double> ExtractStats(ItemStatEngine.Result result)
    {
        var stats = new Dictionary<string, double>(result.Stats, StringComparer.Ordinal);

        if (result.ProcLines == null)
            return stats;

        foreach (var procLine in result.ProcLines)
        {
            if (procLine.Min is not { } minimum)
                continue;

            stats[procLine.Field] = stats.TryGetValue(procLine.Field, out var existing)
                ? existing + minimum
                : minimum;
        }

        return stats;
    }

    private static IEnumerable<ItemStatEngine.InputStat> ToInputStats(IEnumerable<DBStatRow> rows)
    {
        return rows
            .Where(row => row.Stat != null)
            .GroupBy(row => row.Stat)
            .Select(group => group.OrderByDescending(row => row.Value).First())
            .Select(row => new ItemStatEngine.InputStat(row.Stat!, row.TextValue ?? string.Empty, row.Value));
    }
}