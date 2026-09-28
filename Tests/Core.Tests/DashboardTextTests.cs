using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

public class DashboardTextTests
{
    [Fact]
    public void ResolvesKoreanAndEnglish()
    {
        Assert.Equal("새로 고침", DashboardText.RefreshButton(AppLanguage.Ko));
        Assert.Equal("Refresh", DashboardText.RefreshButton(AppLanguage.En));
        Assert.Equal("Aktualisieren", DashboardText.RefreshButton(AppLanguage.De));
    }

    [Fact]
    public void TrendLabelsFollowMacOSTranslations()
    {
        Assert.Equal("이번 달 일별", DashboardText.DailyTrend(AppLanguage.Ko));
        Assert.Equal("Daily this month", DashboardText.DailyTrend(AppLanguage.En));
        Assert.Equal("今月の日別", DashboardText.DailyTrend(AppLanguage.Ja));
        Assert.Equal("Diario de este mes", DashboardText.DailyTrend(AppLanguage.Es));
        Assert.Equal("Par jour ce mois-ci", DashboardText.DailyTrend(AppLanguage.Fr));
        Assert.Equal("Diário deste mês", DashboardText.DailyTrend(AppLanguage.Pt));
        Assert.Equal("Täglich", DashboardText.DailyTrend(AppLanguage.De));
        Assert.Equal("최다", DashboardText.PeakDay(AppLanguage.Ko));
        Assert.Equal("Peak", DashboardText.PeakDay(AppLanguage.En));
        Assert.Equal("最多", DashboardText.PeakDay(AppLanguage.Ja));
        Assert.Equal("Máx.", DashboardText.PeakDay(AppLanguage.Es));
        Assert.Equal("Max.", DashboardText.PeakDay(AppLanguage.Fr));
        Assert.Equal("Máx.", DashboardText.PeakDay(AppLanguage.Pt));
        Assert.Equal("Max.", DashboardText.PeakDay(AppLanguage.De));
    }

    [Fact]
    public void RarityAndItemNamesFollowMacOSTranslations()
    {
        Assert.Equal("희귀", DashboardText.RarityLabel(AppLanguage.Ko, Rarity.Rare));
        Assert.Equal("rare", DashboardText.RarityLabel(AppLanguage.En, Rarity.Rare));
        Assert.Equal("이상한 사탕", DashboardText.ItemName(AppLanguage.Ko, ItemKind.RareCandy));
        Assert.Equal("Rare Candy", DashboardText.ItemName(AppLanguage.En, ItemKind.RareCandy));
        Assert.Equal("포켓몬 알", DashboardText.EggName(AppLanguage.Ko, null));
        Assert.Equal("Rare Egg", DashboardText.EggName(AppLanguage.En, Rarity.Rare));
    }

    [Fact]
    public void StatAndMoveMethodLabelsMapKnownKeys()
    {
        Assert.Equal("HP", DashboardText.StatLabel(AppLanguage.Ko, "hp"));
        Assert.Equal("특공", DashboardText.StatLabel(AppLanguage.Ko, "special-attack"));
        Assert.Equal("Sp.Atk", DashboardText.StatLabel(AppLanguage.En, "special-attack"));
        Assert.Equal("Lv. 7", DashboardText.MoveMethodLabel(AppLanguage.Ko, "level-up", 7));
        Assert.Equal("TM", DashboardText.MoveMethodLabel(AppLanguage.En, "machine", 0));
        Assert.Equal("교배", DashboardText.MoveMethodLabel(AppLanguage.Ko, "egg", 0));
        Assert.Equal("수컷", DashboardText.GenderLabel(AppLanguage.Ko, PokemonGender.Male));
        Assert.Equal("", DashboardText.GenderLabel(AppLanguage.En, null));
    }

    [Fact]
    public void EventTextsAreLocalized()
    {
        Assert.Contains("알에서 꼬렛이(가) 나왔어요",
            DashboardText.EventHatch(AppLanguage.Ko, "꼬렛", false));
        Assert.Equal("Rattata hatched from the egg!",
            DashboardText.EventHatch(AppLanguage.En, "Rattata", false));
        Assert.Contains("이로치 꼬렛이(가) 알에서",
            DashboardText.EventHatch(AppLanguage.Ko, "꼬렛", true));
        Assert.Contains("Evolved into Raticate",
            DashboardText.EventEvolve(AppLanguage.En, "Raticate"));
    }
}
