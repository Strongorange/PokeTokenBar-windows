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
    public void SettingsWindowLabelsFollowMacOSTranslations()
    {
        Assert.Equal("설정", DashboardText.SettingsTitle(AppLanguage.Ko));
        Assert.Equal("Settings", DashboardText.SettingsTitle(AppLanguage.En));
        Assert.Equal("設定", DashboardText.SettingsTitle(AppLanguage.Ja));
        Assert.Equal("Ajustes", DashboardText.SettingsTitle(AppLanguage.Es));
        Assert.Equal("Réglages", DashboardText.SettingsTitle(AppLanguage.Fr));
        Assert.Equal("Ajustes", DashboardText.SettingsTitle(AppLanguage.Pt));
        Assert.Equal("Einstellungen", DashboardText.SettingsTitle(AppLanguage.De));
        Assert.Equal("언어", DashboardText.LanguageLabel(AppLanguage.Ko));
        Assert.Equal("Language", DashboardText.LanguageLabel(AppLanguage.En));
        Assert.Equal("言語", DashboardText.LanguageLabel(AppLanguage.Ja));
        Assert.Equal("난이도", DashboardText.DifficultySection(AppLanguage.Ko));
        Assert.Equal("Difficulty", DashboardText.DifficultySection(AppLanguage.En));
        Assert.Equal("難易度", DashboardText.DifficultySection(AppLanguage.Ja));
        Assert.Equal("상점 가격", DashboardText.DifficultyShopLabel(AppLanguage.Ko));
        Assert.Equal("Shop prices", DashboardText.DifficultyShopLabel(AppLanguage.En));
        Assert.Equal("追加スキャンフォルダ", DashboardText.ScanFoldersTitle(AppLanguage.Ja));
        Assert.Equal("추가 스캔 폴더", DashboardText.ScanFoldersTitle(AppLanguage.Ko));
        Assert.Equal("Additional scan folders", DashboardText.ScanFoldersTitle(AppLanguage.En));
        Assert.Equal("Zusätzliche Scan-Ordner", DashboardText.ScanFoldersTitle(AppLanguage.De));
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

    [Fact]
    public void UnownFormStringsFollowMacOSTranslations()
    {
        Assert.Equal("안농 글자 5/28", DashboardText.UnownFormsCollected(AppLanguage.Ko, 5));
        Assert.Equal("Unown forms 5/28", DashboardText.UnownFormsCollected(AppLanguage.En, 5));
        Assert.Equal("アンノーン 5/28文字", DashboardText.UnownFormsCollected(AppLanguage.Ja, 5));
        Assert.Equal("Formas Unown 5/28", DashboardText.UnownFormsCollected(AppLanguage.Es, 5));
        Assert.Equal("Formes Zarbi 5/28", DashboardText.UnownFormsCollected(AppLanguage.Fr, 5));
        Assert.Equal("Formas Unown 5/28", DashboardText.UnownFormsCollected(AppLanguage.Pt, 5));
        Assert.Equal("Icognito-Formen 5/28", DashboardText.UnownFormsCollected(AppLanguage.De, 5));
        Assert.Equal("미수집", DashboardText.UnownNotCollected(AppLanguage.Ko));
        Assert.Equal("Not collected", DashboardText.UnownNotCollected(AppLanguage.En));
        Assert.Equal("未収集", DashboardText.UnownNotCollected(AppLanguage.Ja));
        Assert.Equal("Sin conseguir", DashboardText.UnownNotCollected(AppLanguage.Es));
        Assert.Equal("Non collectionnée", DashboardText.UnownNotCollected(AppLanguage.Fr));
        Assert.Equal("Não coletada", DashboardText.UnownNotCollected(AppLanguage.Pt));
        Assert.Equal("Noch nicht gesammelt", DashboardText.UnownNotCollected(AppLanguage.De));
    }
}
