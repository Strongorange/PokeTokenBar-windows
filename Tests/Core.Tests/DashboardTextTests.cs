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
    public void CompanionEventsLabelFollowsMacOSTranslations()
    {
        Assert.Equal("컴패니언 이벤트 (부화·진화·졸업)",
            DashboardText.CompanionEventsLabel(AppLanguage.Ko));
        Assert.Equal("Companion events (hatch / evolve / graduate)",
            DashboardText.CompanionEventsLabel(AppLanguage.En));
        Assert.Equal("コンパニオンイベント（孵化・進化・卒業）",
            DashboardText.CompanionEventsLabel(AppLanguage.Ja));
        Assert.Equal("Eventos del compañero (eclosión / evolución / graduación)",
            DashboardText.CompanionEventsLabel(AppLanguage.Es));
        Assert.Equal("Événements du compagnon (éclosion / évolution / diplôme)",
            DashboardText.CompanionEventsLabel(AppLanguage.Fr));
        Assert.Equal("Eventos do companheiro (nascimento / evolução / formatura)",
            DashboardText.CompanionEventsLabel(AppLanguage.Pt));
        Assert.Equal("Begleiter-Ereignisse (Schlüpfen / Entwicklung / Abschied)",
            DashboardText.CompanionEventsLabel(AppLanguage.De));
    }

    [Fact]
    public void ShopStringsFollowMacOSTranslations()
    {
        Assert.Equal("쓸 수 있는 토큰", DashboardText.SpendableTokens(AppLanguage.Ko));
        Assert.Equal("Spendable tokens", DashboardText.SpendableTokens(AppLanguage.En));
        Assert.Equal("Verfügbare Tokens", DashboardText.SpendableTokens(AppLanguage.De));
        Assert.Equal("사용한 토큰으로 아이템을 살 수 있어요.", DashboardText.ShopHint(AppLanguage.Ko));
        Assert.Equal("구매", DashboardText.BuyLabel(AppLanguage.Ko));
        Assert.Equal("Buy", DashboardText.BuyLabel(AppLanguage.En));
        Assert.Equal("이상한 사탕 구매할까요?", DashboardText.BuyConfirm(AppLanguage.Ko, "이상한 사탕"));
        Assert.Equal("Buy Rare Candy?", DashboardText.BuyConfirm(AppLanguage.En, "Rare Candy"));
        Assert.Equal("취소", DashboardText.CancelLabel(AppLanguage.Ko));
        Assert.Equal("Cancel", DashboardText.CancelLabel(AppLanguage.En));
        Assert.Equal("토큰이 부족해요", DashboardText.NotEnoughTokens(AppLanguage.Ko));
        Assert.Equal("Not enough tokens", DashboardText.NotEnoughTokens(AppLanguage.En));
        Assert.Equal("보유 ×2", DashboardText.OwnedCount(AppLanguage.Ko, 2));
        Assert.Equal("가격", DashboardText.ShopPriceLabel(AppLanguage.Ko));
        Assert.Equal("Price", DashboardText.ShopPriceLabel(AppLanguage.En));
        Assert.Equal("보유 중", DashboardText.OwnedAlready(AppLanguage.Ko));
        Assert.Equal("Owned", DashboardText.OwnedAlready(AppLanguage.En));
        Assert.Equal("가방", DashboardText.BagTitle(AppLanguage.Ko));
        Assert.Equal("Bag", DashboardText.BagTitle(AppLanguage.En));
        Assert.Contains("경험치를 100M", DashboardText.ItemDescription(AppLanguage.Ko, ItemKind.RareCandy));
        Assert.Contains("EXP by 100M", DashboardText.ItemDescription(AppLanguage.En, ItemKind.RareCandy));
        Assert.Contains("성격을 랜덤으로", DashboardText.ItemDescription(AppLanguage.Ko, ItemKind.Mint));
        Assert.Contains("새 알로 다시 시작", DashboardText.EggDescription(AppLanguage.Ko, null));
        Assert.Contains("희귀 이상이 확정", DashboardText.EggDescription(AppLanguage.Ko, Rarity.Rare));
        Assert.Equal("지금 품고 있는 알이 부화하면 살 수 있어요.",
            DashboardText.EggShopLockedHint(AppLanguage.Ko));
        Assert.Contains("리자몽을(를) 놓아주고", DashboardText.EggConfirm(AppLanguage.Ko, "리자몽", "희귀 알"));
        Assert.Contains("Send off Charizard", DashboardText.EggConfirm(AppLanguage.En, "Charizard", "Rare Egg"));
        Assert.Contains("이로치 포켓몬이에요", DashboardText.FreshEggShinyWarning(AppLanguage.Ko));
        Assert.Contains("shiny!", DashboardText.FreshEggShinyWarning(AppLanguage.En));
        Assert.Equal("이로치 놓아주기", DashboardText.FreshEggDiscardShiny(AppLanguage.Ko));
        Assert.Equal("Send shiny off", DashboardText.FreshEggDiscardShiny(AppLanguage.En));
    }

    [Fact]
    public void CompanionAndDexPolishStringsFollowMacOSTranslations()
    {
        Assert.Equal("🥚 부화 준비 중", DashboardText.EggIncubating(AppLanguage.Ko));
        Assert.Equal("🥚 Incubating", DashboardText.EggIncubating(AppLanguage.En));
        Assert.Equal("곧 부화해요!", DashboardText.EggImminent(AppLanguage.Ko));
        Assert.Equal("About to hatch!", DashboardText.EggImminent(AppLanguage.En));
        Assert.Equal("부화까지 5M", DashboardText.EggToHatch(AppLanguage.Ko, "5M"));
        Assert.Equal("5M to hatch", DashboardText.EggToHatch(AppLanguage.En, "5M"));
        Assert.Equal("다음 진화까지 1.2M", DashboardText.ToNextEvolution(AppLanguage.Ko, "1.2M"));
        Assert.Equal("1.2M to next evolution", DashboardText.ToNextEvolution(AppLanguage.En, "1.2M"));
        Assert.Equal("졸업까지 900K", DashboardText.ToGraduation(AppLanguage.Ko, "900K"));
        Assert.Equal("900K to graduation", DashboardText.ToGraduation(AppLanguage.En, "900K"));
        Assert.Equal("2× 성장", DashboardText.GrowthBoost(AppLanguage.Ko, 2));
        Assert.Equal("2× growth", DashboardText.GrowthBoost(AppLanguage.En, 2));
        Assert.Equal("희귀 이상 확정", DashboardText.EggGuaranteeHint(AppLanguage.Ko, Rarity.Rare));
        Assert.Equal("Rare or better", DashboardText.EggGuaranteeHint(AppLanguage.En, Rarity.Rare));
        Assert.Equal("탭하면 이 희귀도만 보기 · 다시 탭하면 전체",
            DashboardText.DexFilterHint(AppLanguage.Ko));
        Assert.Equal("아직 포켓몬을 잡지 못했어요!", DashboardText.DexEmptyTitle(AppLanguage.Ko));
        Assert.Equal("No Pokémon caught yet!", DashboardText.DexEmptyTitle(AppLanguage.En));
        Assert.Equal("아직 가방이 비어있어요!", DashboardText.BagEmptyTitle(AppLanguage.Ko));
        Assert.Equal("Your bag is empty!", DashboardText.BagEmptyTitle(AppLanguage.En));
        Assert.Equal("사용하기", DashboardText.UseItemLabel(AppLanguage.Ko));
        Assert.Equal("리자몽에게 사용할까요?", DashboardText.UseOnCurrent(AppLanguage.Ko, "리자몽"));
        Assert.Equal("Use on Charizard?", DashboardText.UseOnCurrent(AppLanguage.En, "Charizard"));
        Assert.Equal("부화 후 사용할 수 있어요", DashboardText.UseAfterHatch(AppLanguage.Ko));
        Assert.Equal("사용할 포켓몬이 없어요", DashboardText.UseNeedsPokemon(AppLanguage.Ko));
        Assert.Equal("이 포켓몬은 졸업할 것으로 예상돼요.",
            DashboardText.CandyGraduatesHint(AppLanguage.Ko));
        Assert.Contains("이월 경험치: 50M XP", DashboardText.CandyCarryoverXP(AppLanguage.Ko, "50M"));
        Assert.Contains("남는 30M XP는 사라져요", DashboardText.CandyDiscardedXP(AppLanguage.Ko, "30M"));
        Assert.Equal("성격 랜덤 변경", DashboardText.MintEffectHint(AppLanguage.Ko));
        Assert.Equal("Random nature", DashboardText.MintEffectHint(AppLanguage.En));
        Assert.Equal("이로치 확률 ↑ · 적용 중", DashboardText.ShinyCharmEffectHint(AppLanguage.Ko));
        Assert.Equal("Shiny rate ↑ · active", DashboardText.ShinyCharmEffectHint(AppLanguage.En));
    }

    [Fact]
    public void RepresentativeAndUsageHomeStringsFollowMacOSTranslations()
    {
        Assert.Equal("대표 포켓몬", DashboardText.RepresentativePokemonLabel(AppLanguage.Ko));
        Assert.Equal("Representative Pokémon", DashboardText.RepresentativePokemonLabel(AppLanguage.En));
        Assert.Equal("현재 포켓몬 따라가기", DashboardText.RepresentativeFollowCurrent(AppLanguage.Ko));
        Assert.Equal("Follow current companion", DashboardText.RepresentativeFollowCurrent(AppLanguage.En));
        Assert.Equal("도감에서 선택…", DashboardText.RepresentativeChooseFromDex(AppLanguage.Ko));
        Assert.Equal("Choose in Pokédex…", DashboardText.RepresentativeChooseFromDex(AppLanguage.En));
        Assert.Equal("대표로 설정", DashboardText.RepresentativeSet(AppLanguage.Ko));
        Assert.Equal("Set as representative", DashboardText.RepresentativeSet(AppLanguage.En));
        Assert.Equal("대표", DashboardText.RepresentativeBadge(AppLanguage.Ko));
        Assert.Equal("Representative", DashboardText.RepresentativeBadge(AppLanguage.En));
        Assert.Equal("글자 선택", DashboardText.UnownChooseForm(AppLanguage.Ko));
        Assert.Equal("Choose form", DashboardText.UnownChooseForm(AppLanguage.En));
        Assert.Equal("오늘 사용한 토큰", DashboardText.TodayTokensHeader(AppLanguage.Ko));
        Assert.Equal("Today's tokens", DashboardText.TodayTokensHeader(AppLanguage.En));
        Assert.Equal("이번 주", DashboardText.ThisWeekLabel(AppLanguage.Ko));
        Assert.Equal("This week", DashboardText.ThisWeekLabel(AppLanguage.En));
        Assert.Equal("이번 달", DashboardText.ThisMonthLabel(AppLanguage.Ko));
        Assert.Equal("This month", DashboardText.ThisMonthLabel(AppLanguage.En));
        Assert.Equal("입력", DashboardText.TokenInputLabel(AppLanguage.Ko));
        Assert.Equal("Input", DashboardText.TokenInputLabel(AppLanguage.En));
        Assert.Equal("출력", DashboardText.TokenOutputLabel(AppLanguage.Ko));
        Assert.Equal("Output", DashboardText.TokenOutputLabel(AppLanguage.En));
        Assert.Equal("캐시 쓰기", DashboardText.TokenCacheWriteLabel(AppLanguage.Ko));
        Assert.Equal("Cache write", DashboardText.TokenCacheWriteLabel(AppLanguage.En));
        Assert.Equal("캐시 읽기", DashboardText.TokenCacheReadLabel(AppLanguage.Ko));
        Assert.Equal("Cache read", DashboardText.TokenCacheReadLabel(AppLanguage.En));
    }

    [Fact]
    public void DetailPolishStringsFollowMacOSTranslations()
    {
        Assert.Equal("개체", DashboardText.IndividualTitle(AppLanguage.Ko));
        Assert.Equal("Individual", DashboardText.IndividualTitle(AppLanguage.En));
        Assert.Equal("Individuum", DashboardText.IndividualTitle(AppLanguage.De));
        Assert.Equal("레벨", DashboardText.LevelTitle(AppLanguage.Ko));
        Assert.Equal("Level", DashboardText.LevelTitle(AppLanguage.En));
        Assert.Equal("성별", DashboardText.GenderTitle(AppLanguage.Ko));
        Assert.Equal("Gender", DashboardText.GenderTitle(AppLanguage.En));
        Assert.Equal("성격", DashboardText.NatureTitle(AppLanguage.Ko));
        Assert.Equal("Nature", DashboardText.NatureTitle(AppLanguage.En));
        Assert.Equal("실제 능력치", DashboardText.ActualStatsTitle(AppLanguage.Ko));
        Assert.Equal("Actual stats", DashboardText.ActualStatsTitle(AppLanguage.En));
        Assert.Equal("Tatsächliche Werte", DashboardText.ActualStatsTitle(AppLanguage.De));
        Assert.Equal("종 정보", DashboardText.SpeciesDataTitle(AppLanguage.Ko));
        Assert.Equal("Species data", DashboardText.SpeciesDataTitle(AppLanguage.En));
        Assert.Equal("Données de l’espèce", DashboardText.SpeciesDataTitle(AppLanguage.Fr));
        Assert.Equal("숨겨진 특성", DashboardText.HiddenAbilityTitle(AppLanguage.Ko));
        Assert.Equal("Hidden Ability", DashboardText.HiddenAbilityTitle(AppLanguage.En));
        Assert.Equal("현재 레벨에서 배운 기술이 없어요.", DashboardText.NoLevelMoves(AppLanguage.Ko));
        Assert.Equal("No level-up moves learned at this level.", DashboardText.NoLevelMoves(AppLanguage.En));
    }

    [Fact]
    public void RarityAndItemNamesFollowMacOSTranslations()
    {
        Assert.Equal("희귀", DashboardText.RarityLabel(AppLanguage.Ko, Rarity.Rare));
        Assert.Equal("Rare", DashboardText.RarityLabel(AppLanguage.En, Rarity.Rare));
        Assert.Equal("Legendary", DashboardText.RarityLabel(AppLanguage.En, Rarity.Legendary));
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

    [Fact]
    public void CatchLogStringsFollowMacOSTranslations()
    {
        Assert.Equal("포획 로그", DashboardText.CatchLogTitle(AppLanguage.Ko));
        Assert.Equal("Catch log", DashboardText.CatchLogTitle(AppLanguage.En));
        Assert.Equal("捕獲ログ", DashboardText.CatchLogTitle(AppLanguage.Ja));
        Assert.Equal("Registro de capturas", DashboardText.CatchLogTitle(AppLanguage.Es));
        Assert.Equal("Journal de captures", DashboardText.CatchLogTitle(AppLanguage.Fr));
        Assert.Equal("Registro de capturas", DashboardText.CatchLogTitle(AppLanguage.Pt));
        Assert.Equal("Fangprotokoll", DashboardText.CatchLogTitle(AppLanguage.De));
        Assert.Equal("총 3마리", DashboardText.DexTotalCount(AppLanguage.Ko, 3));
        Assert.Equal("3 total", DashboardText.DexTotalCount(AppLanguage.En, 3));
        Assert.Equal("全3匹", DashboardText.DexTotalCount(AppLanguage.Ja, 3));
        Assert.Equal("3 en total", DashboardText.DexTotalCount(AppLanguage.Es, 3));
        Assert.Equal("3 au total", DashboardText.DexTotalCount(AppLanguage.Fr, 3));
        Assert.Equal("3 no total", DashboardText.DexTotalCount(AppLanguage.Pt, 3));
        Assert.Equal("3 insgesamt", DashboardText.DexTotalCount(AppLanguage.De, 3));
        Assert.Equal("놓아줌", DashboardText.DexReleasedBadge(AppLanguage.Ko));
        Assert.Equal("Released", DashboardText.DexReleasedBadge(AppLanguage.En));
        Assert.Equal("逃がした", DashboardText.DexReleasedBadge(AppLanguage.Ja));
        Assert.Equal("Liberado", DashboardText.DexReleasedBadge(AppLanguage.Es));
        Assert.Equal("Relâché", DashboardText.DexReleasedBadge(AppLanguage.Fr));
        Assert.Equal("Solto", DashboardText.DexReleasedBadge(AppLanguage.Pt));
        Assert.Equal("Freigelassen", DashboardText.DexReleasedBadge(AppLanguage.De));
    }

    [Fact]
    public void CaughtAgoBucketsFormatInAllLanguages()
    {
        Assert.Equal("42분 전", DashboardText.CaughtAgo(AppLanguage.Ko, RelativeTimeBucket.Minutes, 42));
        Assert.Equal("42 min ago", DashboardText.CaughtAgo(AppLanguage.En, RelativeTimeBucket.Minutes, 42));
        Assert.Equal("42分前", DashboardText.CaughtAgo(AppLanguage.Ja, RelativeTimeBucket.Minutes, 42));
        Assert.Equal("5시간 전", DashboardText.CaughtAgo(AppLanguage.Ko, RelativeTimeBucket.Hours, 5));
        Assert.Equal("5 hr ago", DashboardText.CaughtAgo(AppLanguage.En, RelativeTimeBucket.Hours, 5));
        Assert.Equal("hace 5 h", DashboardText.CaughtAgo(AppLanguage.Es, RelativeTimeBucket.Hours, 5));
        Assert.Equal("il y a 5 h", DashboardText.CaughtAgo(AppLanguage.Fr, RelativeTimeBucket.Hours, 5));
        Assert.Equal("7일 전", DashboardText.CaughtAgo(AppLanguage.Ko, RelativeTimeBucket.Days, 7));
        Assert.Equal("7 d ago", DashboardText.CaughtAgo(AppLanguage.En, RelativeTimeBucket.Days, 7));
        Assert.Equal("vor 7 T.", DashboardText.CaughtAgo(AppLanguage.De, RelativeTimeBucket.Days, 7));
        Assert.Equal("", DashboardText.CaughtAgo(AppLanguage.Ko, RelativeTimeBucket.None, 0));
    }

    [Fact]
    public void StatusLineStringsFollowMacOSTranslations()
    {
        Assert.Equal("곧 깨어나요.", DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.Egg));
        Assert.Equal("Hatching soon.", DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.Egg));
        Assert.Equal("もうすぐ孵化します。", DashboardText.StatusLine(AppLanguage.Ja, CompanionStatusKind.Egg));
        Assert.Equal("Está a punto de eclosionar.", DashboardText.StatusLine(AppLanguage.Es, CompanionStatusKind.Egg));
        Assert.Equal("Bientôt l'éclosion.", DashboardText.StatusLine(AppLanguage.Fr, CompanionStatusKind.Egg));
        Assert.Equal("Vai chocar logo.", DashboardText.StatusLine(AppLanguage.Pt, CompanionStatusKind.Egg));
        Assert.Equal("Schlüpft bald.", DashboardText.StatusLine(AppLanguage.De, CompanionStatusKind.Egg));
        Assert.Equal("오늘은 조용히 자리를 지켜요.", DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.Idle));
        Assert.Equal("Keeping quiet today.", DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.Idle));
        Assert.Equal("오늘의 작업 흔적이 쌓이고 있어요.",
            DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.Working));
        Assert.Equal("Today's work is piling up.",
            DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.Working));
        Assert.Equal("지금은 집중 모드예요.", DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.Focus));
        Assert.Equal("In focus mode now.", DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.Focus));
        Assert.Equal("한도에 가까워요. 잠깐 쉬어도 괜찮아요.",
            DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.Tired));
        Assert.Equal("Close to the limit. A short break is fine.",
            DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.Tired));
        Assert.Equal("지금은 자고 있어요.", DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.Sleep));
        Assert.Equal("Sleeping now.", DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.Sleep));
        Assert.Equal("리자몽(으)로 진화했어요!",
            DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.LevelUp, "리자몽"));
        Assert.Equal("Evolved into Charizard!",
            DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.LevelUp, "Charizard"));
        Assert.Equal("성장했어요!", DashboardText.StatusLine(AppLanguage.Ko, CompanionStatusKind.LevelUp));
        Assert.Equal("It grew!", DashboardText.StatusLine(AppLanguage.En, CompanionStatusKind.LevelUp));
    }

    [Fact]
    public void LaunchAtLoginLabelFollowsMacOSTranslations()
    {
        Assert.Equal("로그인 시 자동 시작", DashboardText.LaunchAtLoginLabel(AppLanguage.Ko));
        Assert.Equal("Launch at login", DashboardText.LaunchAtLoginLabel(AppLanguage.En));
        Assert.Equal("ログイン時に自動起動", DashboardText.LaunchAtLoginLabel(AppLanguage.Ja));
        Assert.Equal("Iniciar al arrancar sesión", DashboardText.LaunchAtLoginLabel(AppLanguage.Es));
        Assert.Equal("Lancer à l'ouverture de session", DashboardText.LaunchAtLoginLabel(AppLanguage.Fr));
        Assert.Equal("Abrir ao iniciar sessão", DashboardText.LaunchAtLoginLabel(AppLanguage.Pt));
        Assert.Equal("Bei der Anmeldung starten", DashboardText.LaunchAtLoginLabel(AppLanguage.De));
    }
}
