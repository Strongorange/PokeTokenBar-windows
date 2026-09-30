using System.Text;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

[Collection("Application diagnostics")]
public class ShopFlowTests : IDisposable
{
    private readonly string _dir;

    public ShopFlowTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "ptb-shopflow-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        Diagnostics.Configure(_dir);
    }

    public void Dispose()
    {
        AppLog.ResetToDefault();
        try { Directory.Delete(_dir, true); } catch { }
    }

    private CompanionEngine BuildEngine(string fileName = "shopflow-state.json") =>
        new(new CompanionEngineOptions
        {
            StateFilePath = Path.Combine(_dir, fileName),
            Clock = () => new DateTimeOffset(2026, 9, 30, 12, 0, 0, TimeSpan.Zero),
            NextRoll = () => 1,
            Lines = PokemonLineSource.Load(
                new MemoryStream(Encoding.UTF8.GetBytes(PokemonLineSourceTests.MinimalSnapshot))),
        });

    private void FundWallet(CompanionEngine engine, long tokens)
    {
        engine.ApplyUsage(
            new Dictionary<string, long> { ["claude_code"] = 0 }, "2026-09-30", true);
        engine.ApplyUsage(
            new Dictionary<string, long> { ["claude_code"] = tokens }, "2026-09-30", true);
    }

    [Fact]
    public void CandyStepperClampsToAvailableCount()
    {
        var engine = BuildEngine();
        FundWallet(engine, 5_000_000);
        engine.State.Inventory[ItemKinds.Raw(ItemKind.RareCandy)] = 3;
        var flow = new ShopFlow(engine);

        Assert.Equal(3, flow.MaxCandyCount());
        Assert.Equal(1, flow.CandyCount);

        Assert.Equal(2, flow.StepCandy(1));
        Assert.Equal(3, flow.StepCandy(5));
        Assert.Equal(1, flow.StepCandy(-9));

        engine.State.Inventory[ItemKinds.Raw(ItemKind.RareCandy)] = 1;
        Assert.Equal(1, flow.ClampCandy());
        Assert.Equal(1, flow.StepCandy(1));
    }

    [Fact]
    public void CandyXpHintScalesWithCount()
    {
        var engine = BuildEngine();
        FundWallet(engine, 5_000_000);
        engine.State.Inventory[ItemKinds.Raw(ItemKind.RareCandy)] = 5;
        var flow = new ShopFlow(engine);

        flow.StepCandy(2);
        Assert.Equal(3, flow.CandyCount);
        Assert.Equal($"+({TokenFormatter.Compact(3 * RareCandies.Xp)} XP)",
            flow.CandyXpHint(AppLanguage.En));
    }

    [Fact]
    public void CandyPreviewLinesFollowMacOSOrdering()
    {
        var lang = AppLanguage.En;

        Assert.Empty(ShopFlow.CandyPreviewLines(null, lang));

        var graduated = new RareCandyUsePlan(8, true, true, 0, 50_000_000);
        var lines = ShopFlow.CandyPreviewLines(graduated, lang);
        Assert.Equal(2, lines.Count);
        Assert.Equal(DashboardText.CandyGraduatesHint(lang), lines[0].Text);
        Assert.True(lines[0].Secondary);
        Assert.Equal(
            DashboardText.CandyDiscardedXP(lang, TokenFormatter.Compact(50_000_000)),
            lines[1].Text);
        Assert.False(lines[1].Secondary);

        var discarded = new RareCandyUsePlan(2, true, false, 0, 10_000_000);
        var discardedLines = ShopFlow.CandyPreviewLines(discarded, lang);
        Assert.Single(discardedLines);
        Assert.False(discardedLines[0].Secondary);

        var carryover = new RareCandyUsePlan(2, true, false, 30_000_000, 0);
        var carryoverLines = ShopFlow.CandyPreviewLines(carryover, lang);
        Assert.Single(carryoverLines);
        Assert.Equal(
            DashboardText.CandyCarryoverXP(lang, TokenFormatter.Compact(30_000_000)),
            carryoverLines[0].Text);
        Assert.True(carryoverLines[0].Secondary);
    }

    [Fact]
    public void BagConfirmRunsTwoStepsAndCommitsThroughEngine()
    {
        var engine = BuildEngine();
        FundWallet(engine, 5_000_000);
        engine.State.Inventory[ItemKinds.Raw(ItemKind.Mint)] = 2;
        var flow = new ShopFlow(engine);

        flow.BeginBagConfirm(ItemKind.Mint);
        Assert.Equal(ItemKind.Mint, flow.ConfirmingBagItem);

        flow.CancelBagConfirm();
        Assert.Null(flow.ConfirmingBagItem);
        Assert.Null(flow.Feedback);
        Assert.Equal(2, engine.State.Inventory[ItemKinds.Raw(ItemKind.Mint)]);

        flow.BeginBagConfirm(ItemKind.Mint);
        flow.CommitBagConfirm();
        Assert.Null(flow.ConfirmingBagItem);
        Assert.NotNull(flow.Feedback);
        Assert.Equal(1, engine.State.Inventory[ItemKinds.Raw(ItemKind.Mint)]);
        Assert.NotEqual("", flow.Feedback!(AppLanguage.En));
    }

    [Fact]
    public void ItemConfirmCommitsPurchaseAndClearsState()
    {
        var engine = BuildEngine();
        FundWallet(engine, 600_000_000);
        var flow = new ShopFlow(engine);
        var label = engine.View().ShopRows.Single(row => row.Item == ItemKind.Mint).Label;

        flow.BeginItemConfirm(ItemKind.Mint);
        Assert.Equal(ItemKind.Mint, flow.ConfirmingShopItem);

        flow.CancelItemConfirm();
        Assert.Null(flow.ConfirmingShopItem);
        Assert.Equal(0, engine.State.Inventory.GetValueOrDefault(ItemKinds.Raw(ItemKind.Mint)));

        flow.BeginItemConfirm(ItemKind.Mint);
        flow.CommitItemBuy(label);
        Assert.Null(flow.ConfirmingShopItem);
        Assert.Equal(1, engine.State.Inventory.GetValueOrDefault(ItemKinds.Raw(ItemKind.Mint)));
        Assert.Equal(DashboardText.BoughtItem(AppLanguage.En, label),
            flow.Feedback!(AppLanguage.En));
    }

    [Fact]
    public void EggConfirmCommitsImmediatelyForNonShinyCompanion()
    {
        var engine = BuildEngine();
        FundWallet(engine, 5_000_000);
        engine.State.UsedSinceInstall = 2_000_000_000;
        var flow = new ShopFlow(engine);
        var row = engine.View().ShopRows.Single(row => row.Item is null && row.EggTier is null);
        Assert.NotNull(engine.State.Active);
        Assert.False(engine.View().IsShiny);

        flow.BeginEggConfirm(row.EggTier);
        Assert.Equal((Rarity?) null, flow.EggConfirm.Tier);
        Assert.Equal(EggConfirmStage.Confirm, flow.EggConfirm.Stage);

        flow.AdvanceEggConfirm(row.Label, engine.View().IsShiny);
        Assert.Equal(EggConfirmStage.None, flow.EggConfirm.Stage);
        Assert.Null(engine.State.Active);
        Assert.True(engine.View().IsEgg);
        Assert.Equal(DashboardText.BoughtItem(AppLanguage.En, row.Label),
            flow.Feedback!(AppLanguage.En));
    }

    [Fact]
    public void EggConfirmEscalatesToShinyWarningBeforeCommitting()
    {
        var engine = BuildEngine();
        FundWallet(engine, 5_000_000);
        engine.State.UsedSinceInstall = 2_000_000_000;
        engine.State.Active!.IsShiny = true;
        var flow = new ShopFlow(engine);
        var row = engine.View().ShopRows.Single(row => row.Item is null && row.EggTier is null);

        flow.BeginEggConfirm(row.EggTier);
        flow.AdvanceEggConfirm(row.Label, engine.View().IsShiny);
        Assert.Equal(EggConfirmStage.ShinyWarning, flow.EggConfirm.Stage);
        Assert.NotNull(engine.State.Active);

        flow.AdvanceEggConfirm(row.Label, false);
        Assert.Equal(EggConfirmStage.None, flow.EggConfirm.Stage);
        Assert.Null(engine.State.Active);
    }

    [Fact]
    public void EggConfirmCancelReturnsToIdleAndKeepsCompanion()
    {
        var engine = BuildEngine();
        FundWallet(engine, 600_000_000);
        var flow = new ShopFlow(engine);
        var row = engine.View().ShopRows.Single(row => row.Item is null && row.EggTier is null);

        flow.BeginEggConfirm(row.EggTier);
        flow.CancelEggConfirm();
        Assert.Equal(EggConfirmStage.None, flow.EggConfirm.Stage);
        Assert.NotNull(engine.State.Active);
    }

    [Fact]
    public void ResetClearsAllConfirmState()
    {
        var engine = BuildEngine();
        var flow = new ShopFlow(engine);

        flow.BeginBagConfirm(ItemKind.Mint);
        flow.BeginItemConfirm(ItemKind.RareCandy);
        flow.BeginEggConfirm(Rarity.Rare);

        flow.Reset();

        Assert.Null(flow.ConfirmingBagItem);
        Assert.Null(flow.ConfirmingShopItem);
        Assert.Equal((Rarity?) null, flow.EggConfirm.Tier);
        Assert.Equal(EggConfirmStage.None, flow.EggConfirm.Stage);
    }
}
