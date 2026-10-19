using System.Globalization;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public sealed record RecapBucketView(string Label, long Tokens, bool IsCurrent, bool HasData, string ToolTip);

public sealed record RecapGraduateView(string Name, int SpeciesID, bool IsShiny, PokemonForm? Form);

public sealed record RecapView(
    RecapScope Scope,
    int Offset,
    string Title,
    long TotalTokens,
    string TotalText,
    string? DeltaText,
    string? CompareNote,
    string CurrentPeriodLabel,
    bool IsInProgress,
    bool CanGoBack,
    IReadOnlyList<RecapBucketView> Buckets,
    string? BestDayDateText,
    string BestDayTokensText,
    string BestDayText,
    string ActiveDaysText,
    string StreakText,
    string GraduatesCaption,
    IReadOnlyList<RecapGraduateView> Graduates,
    string GraduatesEmptyText,
    AppLanguage Language);

/// <summary>
/// Builds the recap screen's read-only view model (macOS RecapContent). Bucket tooltips carry
/// the #368 contract — exact tokens, a real zero, or "no data" — and everything is pure so the
/// window re-renders from a fresh ledger copy on every refresh.
/// </summary>
public static class RecapQuery
{
    public static RecapView Build(UsageLedger ledger, IReadOnlyList<DexEntry> dex,
        RecapScope scope, int offset, DateTimeOffset now, TimeZoneInfo timeZone, AppLanguage lang)
    {
        var culture = CultureFor(lang);
        var period = RecapPeriod.Containing(scope, now, timeZone).Shifted(offset);
        var recap = UsageRecap.Make(ledger, dex, period, now, timeZone);

        var title = scope switch
        {
            RecapScope.Week => string.Create(culture,
                $"{period.LocalStart.ToString("m", culture)} – {period.LocalEndExclusive.AddDays(-1).ToString("m", culture)}"),
            RecapScope.Month => period.LocalStart.ToString("Y", culture),
            _ => period.LocalStart.Year.ToString(CultureInfo.InvariantCulture)
        };

        string? deltaText = null;
        if (recap.Delta is { } delta)
        {
            var percent = TokenFormatter.Percent(delta * 100);
            deltaText = delta >= 0 ? $"+{percent}" : percent;
        }

        var weekdayInitials = DashboardText.WeekdayInitials(lang);
        var buckets = recap.Buckets.Select(bucket =>
        {
            var day = DateTime.ParseExact(bucket.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            var label = scope switch
            {
                RecapScope.Week => weekdayInitials[(int)day.DayOfWeek].ToString(),
                RecapScope.Month => day.Day.ToString(CultureInfo.InvariantCulture),
                _ => culture.DateTimeFormat.AbbreviatedMonthNames[day.Month - 1]
            };
            var periodLabel = scope == RecapScope.Year
                ? day.ToString("Y", culture)
                : day.ToString("m", culture);
            var value = !bucket.HasData
                ? DashboardText.RecapNoData(lang)
                : TokenFormatter.Grouped(bucket.Tokens);
            return new RecapBucketView(
                label,
                bucket.Tokens,
                bucket.IsCurrent,
                bucket.HasData,
                $"{periodLabel}, {value}");
        }).ToList();

        var bestDayText = recap.BestDay is { } best
            ? DashboardText.RecapBestDayLine(lang,
                DateTime.ParseExact(best.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                    .ToString("m", culture),
                TokenFormatter.Grouped(best.Tokens))
            : "--";

        var graduates = recap.Graduated.Take(4).Select(entry =>
        {
            var name = entry.Names is not null && entry.Names.TryGetValue(entry.FinalID, out var byLang)
                ? lang.ResolveName(byLang)
                : null;
            return new RecapGraduateView(
                name ?? PokemonNameLocalization.Identifier($"#{entry.FinalID}"),
                entry.FinalID,
                entry.IsShiny,
                PokemonForms.Resolved(entry.FinalID, entry.Form));
        }).ToList();

        return new RecapView(
            scope,
            offset,
            title,
            recap.Total,
            TokenFormatter.Grouped(recap.Total),
            deltaText,
            recap.IsInProgress && recap.PreviousTotal is not null
                ? DashboardText.RecapCompareSoFar(lang, scope)
                : null,
            DashboardText.RecapCurrentPeriod(lang, scope),
            recap.IsInProgress,
            recap.CanGoBack,
            buckets,
            recap.BestDay is { } best2
                ? DateTime.ParseExact(best2.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture)
                    .ToString("m", culture)
                : null,
            recap.BestDay is { } best1 ? TokenFormatter.Compact(best1.Tokens) : "--",
            bestDayText,
            $"{recap.ActiveDays}/{recap.CountedDays}",
            DashboardText.RecapDays(lang, recap.BestStreak),
            DashboardText.RecapGraduates(lang),
            graduates,
            DashboardText.RecapNoGraduates(lang),
            lang);
    }

    /// <summary>Display culture per app language, for period titles and month names.</summary>
    public static CultureInfo CultureFor(AppLanguage lang) => lang switch
    {
        AppLanguage.Ko => CultureInfo.GetCultureInfo("ko-KR"),
        AppLanguage.Ja => CultureInfo.GetCultureInfo("ja-JP"),
        AppLanguage.Es => CultureInfo.GetCultureInfo("es-ES"),
        AppLanguage.Fr => CultureInfo.GetCultureInfo("fr-FR"),
        AppLanguage.Pt => CultureInfo.GetCultureInfo("pt-BR"),
        AppLanguage.De => CultureInfo.GetCultureInfo("de-DE"),
        _ => CultureInfo.GetCultureInfo("en-US")
    };
}
