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

    [Fact]
    public void EvolutionLineStringsFollowMacOSTranslations()
    {
        Assert.Equal("최종 진화체", DashboardText.FinalForm(AppLanguage.Ko));
        Assert.Equal("Final form", DashboardText.FinalForm(AppLanguage.En));
        Assert.Equal("最終進化", DashboardText.FinalForm(AppLanguage.Ja));
        Assert.Equal("Forma final", DashboardText.FinalForm(AppLanguage.Es));
        Assert.Equal("Forme finale", DashboardText.FinalForm(AppLanguage.Fr));
        Assert.Equal("Forma final", DashboardText.FinalForm(AppLanguage.Pt));
        Assert.Equal("Letzte Entwicklungsstufe", DashboardText.FinalForm(AppLanguage.De));
        Assert.Equal("알 수 없는 다음 진화", DashboardText.UnknownNextEvolution(AppLanguage.Ko));
        Assert.Equal("Unknown next evolution", DashboardText.UnknownNextEvolution(AppLanguage.En));
        Assert.Equal("次の進化先は不明", DashboardText.UnknownNextEvolution(AppLanguage.Ja));
        Assert.Equal("Próxima evolución desconocida", DashboardText.UnknownNextEvolution(AppLanguage.Es));
        Assert.Equal("Prochaine évolution inconnue", DashboardText.UnknownNextEvolution(AppLanguage.Fr));
        Assert.Equal("Próxima evolución desconhecida", DashboardText.UnknownNextEvolution(AppLanguage.Pt));
        Assert.Equal("Nächste Entwicklung unbekannt", DashboardText.UnknownNextEvolution(AppLanguage.De));
        Assert.Equal("이로치", DashboardText.ShinyLabel(AppLanguage.Ko));
        Assert.Equal("Shiny", DashboardText.ShinyLabel(AppLanguage.En));
        Assert.Equal("色違い", DashboardText.ShinyLabel(AppLanguage.Ja));
        Assert.Equal("Variocolor", DashboardText.ShinyLabel(AppLanguage.Es));
        Assert.Equal("Chromatique", DashboardText.ShinyLabel(AppLanguage.Fr));
        Assert.Equal("Shiny", DashboardText.ShinyLabel(AppLanguage.Pt));
        Assert.Equal("Schillernd", DashboardText.ShinyLabel(AppLanguage.De));
    }

    [Fact]
    public void UpdateStringsFollowMacOSTranslations()
    {
        Assert.Equal("🆕 v0.16.0 사용 가능 (현재 0.15.0)",
            DashboardText.UpdateAvailable(AppLanguage.Ko, "0.16.0", "0.15.0"));
        Assert.Equal("🆕 v0.16.0 available (you have 0.15.0)",
            DashboardText.UpdateAvailable(AppLanguage.En, "0.16.0", "0.15.0"));
        Assert.Equal("🆕 v0.16.0 が利用可能（現在 0.15.0）",
            DashboardText.UpdateAvailable(AppLanguage.Ja, "0.16.0", "0.15.0"));
        Assert.Equal("🆕 v0.16.0 disponible (tienes 0.15.0)",
            DashboardText.UpdateAvailable(AppLanguage.Es, "0.16.0", "0.15.0"));
        Assert.Equal("🆕 v0.16.0 disponible (tu as 0.15.0)",
            DashboardText.UpdateAvailable(AppLanguage.Fr, "0.16.0", "0.15.0"));
        Assert.Equal("🆕 v0.16.0 disponível (você tem 0.15.0)",
            DashboardText.UpdateAvailable(AppLanguage.Pt, "0.16.0", "0.15.0"));
        Assert.Equal("🆕 v0.16.0 verfügbar (installiert: 0.15.0)",
            DashboardText.UpdateAvailable(AppLanguage.De, "0.16.0", "0.15.0"));
        Assert.Equal("업데이트", DashboardText.UpdateButton(AppLanguage.Ko));
        Assert.Equal("Update", DashboardText.UpdateButton(AppLanguage.En));
        Assert.Equal("更新", DashboardText.UpdateButton(AppLanguage.Ja));
        Assert.Equal("Actualizar", DashboardText.UpdateButton(AppLanguage.Es));
        Assert.Equal("Mettre à jour", DashboardText.UpdateButton(AppLanguage.Fr));
        Assert.Equal("Instalar", DashboardText.UpdateButton(AppLanguage.Pt));
        Assert.Equal("Aktualisieren", DashboardText.UpdateButton(AppLanguage.De));
        Assert.Equal("이 버전 건너뛰기", DashboardText.SkipThisVersion(AppLanguage.Ko));
        Assert.Equal("Skip this version", DashboardText.SkipThisVersion(AppLanguage.En));
        Assert.Equal("このバージョンをスキップ", DashboardText.SkipThisVersion(AppLanguage.Ja));
        Assert.Equal("Omitir esta versión", DashboardText.SkipThisVersion(AppLanguage.Es));
        Assert.Equal("Ignorer cette version", DashboardText.SkipThisVersion(AppLanguage.Fr));
        Assert.Equal("Ignorar esta versão", DashboardText.SkipThisVersion(AppLanguage.Pt));
        Assert.Equal("Diese Version überspringen", DashboardText.SkipThisVersion(AppLanguage.De));
        Assert.Equal("v0.16.0을 건너뛰었어요", DashboardText.SkippedVersionText(AppLanguage.Ko, "0.16.0"));
        Assert.Equal("You skipped v0.16.0", DashboardText.SkippedVersionText(AppLanguage.En, "0.16.0"));
        Assert.Equal("v0.16.0 をスキップしました", DashboardText.SkippedVersionText(AppLanguage.Ja, "0.16.0"));
        Assert.Equal("Omitiste la v0.16.0", DashboardText.SkippedVersionText(AppLanguage.Es, "0.16.0"));
        Assert.Equal("Tu as ignoré la v0.16.0", DashboardText.SkippedVersionText(AppLanguage.Fr, "0.16.0"));
        Assert.Equal("Você ignorou a v0.16.0", DashboardText.SkippedVersionText(AppLanguage.Pt, "0.16.0"));
        Assert.Equal("Du hast v0.16.0 übersprungen", DashboardText.SkippedVersionText(AppLanguage.De, "0.16.0"));
        Assert.Equal("다시 알리기", DashboardText.ShowSkippedAgain(AppLanguage.Ko));
        Assert.Equal("Show again", DashboardText.ShowSkippedAgain(AppLanguage.En));
        Assert.Equal("もう一度表示", DashboardText.ShowSkippedAgain(AppLanguage.Ja));
        Assert.Equal("Mostrar de nuevo", DashboardText.ShowSkippedAgain(AppLanguage.Es));
        Assert.Equal("Afficher à nouveau", DashboardText.ShowSkippedAgain(AppLanguage.Fr));
        Assert.Equal("Mostrar de novo", DashboardText.ShowSkippedAgain(AppLanguage.Pt));
        Assert.Equal("Wieder anzeigen", DashboardText.ShowSkippedAgain(AppLanguage.De));
        Assert.Equal("업데이트", DashboardText.UpdateSectionTitle(AppLanguage.Ko));
        Assert.Equal("Updates", DashboardText.UpdateSectionTitle(AppLanguage.En));
        Assert.Equal("アップデート", DashboardText.UpdateSectionTitle(AppLanguage.Ja));
        Assert.Equal("Actualizaciones", DashboardText.UpdateSectionTitle(AppLanguage.Es));
        Assert.Equal("Mises à jour", DashboardText.UpdateSectionTitle(AppLanguage.Fr));
        Assert.Equal("Atualizações", DashboardText.UpdateSectionTitle(AppLanguage.Pt));
        Assert.Equal("Aktualisierungen", DashboardText.UpdateSectionTitle(AppLanguage.De));
        Assert.Equal("업데이트 알림", DashboardText.UpdateNotificationsLabel(AppLanguage.Ko));
        Assert.Equal("Update notifications", DashboardText.UpdateNotificationsLabel(AppLanguage.En));
        Assert.Equal("アップデート通知", DashboardText.UpdateNotificationsLabel(AppLanguage.Ja));
        Assert.Equal("Notificaciones de actualización", DashboardText.UpdateNotificationsLabel(AppLanguage.Es));
        Assert.Equal("Notifications de mise à jour", DashboardText.UpdateNotificationsLabel(AppLanguage.Fr));
        Assert.Equal("Notificações de atualização", DashboardText.UpdateNotificationsLabel(AppLanguage.Pt));
        Assert.Equal("Hinweise auf Aktualisierungen", DashboardText.UpdateNotificationsLabel(AppLanguage.De));
        Assert.Equal("업데이트 확인", DashboardText.CheckForUpdatesLabel(AppLanguage.Ko));
        Assert.Equal("Check for updates", DashboardText.CheckForUpdatesLabel(AppLanguage.En));
        Assert.Equal("アップデートを確認", DashboardText.CheckForUpdatesLabel(AppLanguage.Ja));
        Assert.Equal("Buscar actualizaciones", DashboardText.CheckForUpdatesLabel(AppLanguage.Es));
        Assert.Equal("Rechercher des mises à jour", DashboardText.CheckForUpdatesLabel(AppLanguage.Fr));
        Assert.Equal("Buscar atualizações", DashboardText.CheckForUpdatesLabel(AppLanguage.Pt));
        Assert.Equal("Nach Aktualisierungen suchen", DashboardText.CheckForUpdatesLabel(AppLanguage.De));
        Assert.Equal("지금 확인", DashboardText.CheckNowButton(AppLanguage.Ko));
        Assert.Equal("Check now", DashboardText.CheckNowButton(AppLanguage.En));
        Assert.Equal("今すぐ確認", DashboardText.CheckNowButton(AppLanguage.Ja));
        Assert.Equal("Comprobar ahora", DashboardText.CheckNowButton(AppLanguage.Es));
        Assert.Equal("Vérifier maintenant", DashboardText.CheckNowButton(AppLanguage.Fr));
        Assert.Equal("Buscar agora", DashboardText.CheckNowButton(AppLanguage.Pt));
        Assert.Equal("Jetzt prüfen", DashboardText.CheckNowButton(AppLanguage.De));
        Assert.Equal("새 버전 v0.16.0 있어요", DashboardText.UpdateFound(AppLanguage.Ko, "0.16.0"));
        Assert.Equal("Version 0.16.0 is available", DashboardText.UpdateFound(AppLanguage.En, "0.16.0"));
        Assert.Equal("バージョン 0.16.0 が利用可能です", DashboardText.UpdateFound(AppLanguage.Ja, "0.16.0"));
        Assert.Equal("La versión 0.16.0 está disponible", DashboardText.UpdateFound(AppLanguage.Es, "0.16.0"));
        Assert.Equal("La version 0.16.0 est disponible", DashboardText.UpdateFound(AppLanguage.Fr, "0.16.0"));
        Assert.Equal("A versão 0.16.0 está disponível", DashboardText.UpdateFound(AppLanguage.Pt, "0.16.0"));
        Assert.Equal("Version 0.16.0 ist verfügbar", DashboardText.UpdateFound(AppLanguage.De, "0.16.0"));
        Assert.Equal("최신 버전이에요 (v0.15.0)", DashboardText.UpToDate(AppLanguage.Ko, "0.15.0"));
        Assert.Equal("You're on the latest (v0.15.0)", DashboardText.UpToDate(AppLanguage.En, "0.15.0"));
        Assert.Equal("最新です (v0.15.0)", DashboardText.UpToDate(AppLanguage.Ja, "0.15.0"));
        Assert.Equal("Tienes la última versión (v0.15.0)", DashboardText.UpToDate(AppLanguage.Es, "0.15.0"));
        Assert.Equal("Tu as la dernière version (v0.15.0)", DashboardText.UpToDate(AppLanguage.Fr, "0.15.0"));
        Assert.Equal("Você está na última versão (v0.15.0)", DashboardText.UpToDate(AppLanguage.Pt, "0.15.0"));
        Assert.Equal("Du hast die neueste Version (v0.15.0)", DashboardText.UpToDate(AppLanguage.De, "0.15.0"));
    }
}
