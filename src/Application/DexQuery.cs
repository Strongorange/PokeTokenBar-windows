using System.Globalization;
using System.Text;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public enum DexSortOption
{
    NumberAsc,
    NumberDesc,
    NameAsc,
    NameDesc,
    RarityDesc
}

public enum CatchLogSortOption
{
    RecentFirst,
    OldestFirst,
    NumberAsc,
    NumberDesc,
    NameAsc,
    NameDesc,
    RarityDesc
}

/// <summary>
/// The one search rule shared by the Pokédex grid and the Catch Log, so the two screens cannot
/// drift apart. A query matches a Pokédex number (`25` or `#25`, partial digits included) or part
/// of any stored name in any language, ignoring case and diacritics ("flabebe" finds Flabébé).
/// Port of macOS DexSearchMatcher (#329).
/// </summary>
public readonly struct DexSearchMatcher
{
    private readonly string _query;
    private readonly string _folded;
    private readonly int? _number;

    private DexSearchMatcher(string query, string folded, int? number)
    {
        _query = query;
        _folded = folded;
        _number = number;
    }

    /// <summary>Null for a blank query, which filters nothing out.</summary>
    public static DexSearchMatcher? Create(string? rawQuery)
    {
        var trimmed = rawQuery?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        var digits = trimmed.StartsWith('#') ? trimmed[1..] : trimmed;
        var number = int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture,
            out var parsed) ? (int?)parsed : null;
        return new DexSearchMatcher(trimmed, Fold(trimmed), number);
    }

    public bool Matches(CompanionDexRow row)
    {
        var query = _query;
        var folded = _folded;
        var number = _number;
        var names = row.Names is null
            ? new[] { row.Name }
            : row.Names.Values.Append(row.Name).ToArray();
        return Matches(row.SpeciesID, query, folded, number, names);
    }

    /// <summary>An individual matches through any species of its chain, like its evolution line in the log row.</summary>
    public bool Matches(CompanionCatchRow row)
    {
        var query = _query;
        var folded = _folded;
        var number = _number;
        var ids = row.Chain.Select(node => node.SpeciesID).Append(row.BaseID).ToArray();
        var names = (row.SearchNames ?? [])
            .Concat(row.Chain.Select(node => node.Name)).ToArray();
        return Matches(ids, query, folded, number, names);
    }

    private static bool Matches(int id, string query, string folded, int? number, string[] names) =>
        Matches([id], query, folded, number, names);

    private static bool Matches(int[] ids, string query, string folded, int? number, string[] names)
    {
        if (number is { } hit && ids.Contains(hit)) return true;
        // Partial numbers keep working: "25" also finds #125, "#25" finds #25 and #250.
        if (ids.Any(id => $"#{id}".Contains(query, StringComparison.Ordinal))) return true;
        return names.Any(name => Fold(name).Contains(folded, StringComparison.Ordinal));
    }

    /// <summary>Case- and diacritic-insensitive comparison key (NFD, combining marks dropped).</summary>
    private static string Fold(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var buffer = new char[normalized.Length];
        var length = 0;
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                buffer[length++] = c;
        return new string(buffer, 0, length).ToLowerInvariant();
    }
}

/// <summary>Filters and sorts the dex grid and catch log rows (macOS #329).</summary>
public static class DexQuery
{
    public static IReadOnlyList<CompanionDexRow> FilterAndSortDexRows(
        IReadOnlyList<CompanionDexRow> rows, string? query = null, Rarity? rarity = null,
        bool shinyOnly = false, DexSortOption sort = DexSortOption.NumberAsc)
    {
        IEnumerable<CompanionDexRow> result = rows;
        if (rarity is { } filter) result = result.Where(row => row.Rarity == filter);
        if (shinyOnly) result = result.Where(row => row.IsShiny);
        if (DexSearchMatcher.Create(query) is { } matcher)
            result = result.Where(matcher.Matches);
        return sort switch
        {
            DexSortOption.NumberAsc => result.OrderBy(row => row.SpeciesID).ToList(),
            DexSortOption.NumberDesc => result.OrderByDescending(row => row.SpeciesID).ToList(),
            DexSortOption.NameAsc => result
                .OrderBy(row => row.Name, StringComparer.CurrentCulture)
                .ThenBy(row => row.SpeciesID).ToList(),
            DexSortOption.NameDesc => result
                .OrderByDescending(row => row.Name, StringComparer.CurrentCulture)
                .ThenBy(row => row.SpeciesID).ToList(),
            DexSortOption.RarityDesc => result
                .OrderByDescending(row => row.Rarity.SortRank())
                .ThenBy(row => row.SpeciesID).ToList(),
            _ => result.ToList()
        };
    }

    public static IReadOnlyList<CompanionCatchRow> FilterAndSortCatchRows(
        IReadOnlyList<CompanionCatchRow> rows, string? query = null, Rarity? rarity = null,
        bool shinyOnly = false, CatchLogSortOption sort = CatchLogSortOption.RecentFirst)
    {
        IEnumerable<CompanionCatchRow> result = rows;
        if (rarity is { } filter) result = result.Where(row => row.Rarity == filter);
        if (shinyOnly) result = result.Where(row => row.IsShiny);
        if (DexSearchMatcher.Create(query) is { } matcher)
            result = result.Where(matcher.Matches);
        return sort switch
        {
            CatchLogSortOption.OldestFirst => Graduated(result, ascending: true)
                .Concat(result.Where(row => row.IsRaising)).ToList(),
            CatchLogSortOption.NumberAsc => result
                .OrderBy(row => FinalID(row))
                .ThenByDescending(row => row.CaughtAt ?? DateTimeOffset.MinValue)
                .ThenBy(row => row.Key, StringComparer.Ordinal).ToList(),
            CatchLogSortOption.NumberDesc => result
                .OrderByDescending(row => FinalID(row))
                .ThenByDescending(row => row.CaughtAt ?? DateTimeOffset.MinValue)
                .ThenBy(row => row.Key, StringComparer.Ordinal).ToList(),
            CatchLogSortOption.NameAsc => result
                .OrderBy(row => SortName(row), StringComparer.CurrentCulture)
                .ThenByDescending(row => row.CaughtAt ?? DateTimeOffset.MinValue)
                .ThenBy(row => row.Key, StringComparer.Ordinal).ToList(),
            CatchLogSortOption.NameDesc => result
                .OrderByDescending(row => SortName(row), StringComparer.CurrentCulture)
                .ThenByDescending(row => row.CaughtAt ?? DateTimeOffset.MinValue)
                .ThenBy(row => row.Key, StringComparer.Ordinal).ToList(),
            CatchLogSortOption.RarityDesc => result
                .OrderByDescending(row => row.Rarity.SortRank())
                .ThenBy(row => FinalID(row)).ToList(),
            // The raising individual stays pinned first; graduates follow by caught-at descending.
            _ => result.Where(row => row.IsRaising)
                .Concat(Graduated(result, ascending: false)).ToList()
        };
    }

    private static IEnumerable<CompanionCatchRow> Graduated(IEnumerable<CompanionCatchRow> rows,
        bool ascending)
    {
        var graduated = rows.Where(row => !row.IsRaising)
            .OrderBy(row => row.CaughtAt ?? DateTimeOffset.MinValue)
            .ThenBy(row => row.Key, StringComparer.Ordinal)
            .ToList();
        if (!ascending) graduated.Reverse();
        return graduated;
    }

    private static int FinalID(CompanionCatchRow row) =>
        row.Chain.Count > 0 ? row.Chain[^1].SpeciesID : row.BaseID;

    private static string SortName(CompanionCatchRow row) =>
        row.Chain.Count > 0 ? row.Chain[^1].Name : "";

    public static string Label(AppLanguage lang, DexSortOption option) => option switch
    {
        DexSortOption.NumberAsc => DashboardText.SortNumberAsc(lang),
        DexSortOption.NumberDesc => DashboardText.SortNumberDesc(lang),
        DexSortOption.NameAsc => DashboardText.SortNameAsc(lang),
        DexSortOption.NameDesc => DashboardText.SortNameDesc(lang),
        _ => DashboardText.SortRarity(lang)
    };

    public static string Label(AppLanguage lang, CatchLogSortOption option) => option switch
    {
        CatchLogSortOption.RecentFirst => DashboardText.SortDateDesc(lang),
        CatchLogSortOption.OldestFirst => DashboardText.SortDateAsc(lang),
        CatchLogSortOption.NumberAsc => DashboardText.SortNumberAsc(lang),
        CatchLogSortOption.NumberDesc => DashboardText.SortNumberDesc(lang),
        CatchLogSortOption.NameAsc => DashboardText.SortNameAsc(lang),
        CatchLogSortOption.NameDesc => DashboardText.SortNameDesc(lang),
        _ => DashboardText.SortRarity(lang)
    };
}
