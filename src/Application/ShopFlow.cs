using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

public enum EggConfirmStage
{
    None,
    Confirm,
    ShinyWarning
}

/// <summary>
/// Bag/shop interaction state machine extracted from the dashboard code-behind:
/// two-step use/purchase confirms (bag items, shop items, eggs incl. the
/// shiny re-confirm), the rare-candy stepper clamp, candy plan preview lines,
/// and the post-action footer feedback. Pure coordinator over CompanionEngine
/// with no UI dependencies so it gets unit coverage.
/// </summary>
public sealed class ShopFlow
{
    private readonly CompanionEngine _engine;

    public ShopFlow(CompanionEngine engine) => _engine = engine;

    public ItemKind? ConfirmingBagItem { get; private set; }
    public ItemKind? ConfirmingShopItem { get; private set; }
    public (Rarity? Tier, EggConfirmStage Stage) EggConfirm { get; private set; }
    public Func<AppLanguage, string>? Feedback { get; private set; }
    public int CandyCount { get; private set; } = 1;

    public int MaxCandyCount() => Math.Max(1, _engine.MaxRareCandyUseCount());

    public int ClampCandy()
    {
        CandyCount = Math.Clamp(CandyCount, 1, MaxCandyCount());
        return CandyCount;
    }

    public int StepCandy(int delta)
    {
        CandyCount = Math.Clamp(CandyCount + delta, 1, MaxCandyCount());
        return CandyCount;
    }

    public string CandyXpHint(AppLanguage lang) => "+(" +
        TokenFormatter.Compact((long) CandyCount * RareCandies.Xp) + " XP)";

    public static IReadOnlyList<(string Text, bool Secondary)> CandyPreviewLines(
        RareCandyUsePlan? plan, AppLanguage lang)
    {
        var lines = new List<(string, bool)>();
        if (plan is null) return lines;
        if (plan.Graduates)
            lines.Add((DashboardText.CandyGraduatesHint(lang), true));
        if (plan.DiscardedXP > 0)
            lines.Add((DashboardText.CandyDiscardedXP(
                lang, TokenFormatter.Compact(plan.DiscardedXP)), false));
        else if (plan.Evolves && !plan.Graduates)
            lines.Add((DashboardText.CandyCarryoverXP(
                lang, TokenFormatter.Compact(plan.CarryoverXP)), true));
        return lines;
    }

    public void BeginBagConfirm(ItemKind kind) => ConfirmingBagItem = kind;

    public void CancelBagConfirm() => ConfirmingBagItem = null;

    public void CommitBagConfirm()
    {
        if (ConfirmingBagItem is not { } kind) return;
        ConfirmingBagItem = null;
        if (kind == ItemKind.Mint)
        {
            var nature = _engine.UseMint();
            Feedback = nature is { } picked
                ? language => DashboardText.MintUsed(language, picked.ToString())
                : DashboardText.NoMint;
        }
        else
        {
            var result = _engine.UseRareCandy(CandyCount);
            Feedback = result switch
            {
                CandyUseResult.Graduated => DashboardText.CandyGraduated,
                CandyUseResult.Evolved => DashboardText.CandyEvolved,
                CandyUseResult.Progressed => DashboardText.CandyProgressed,
                _ => DashboardText.NoCandy,
            };
        }
    }

    public void BeginItemConfirm(ItemKind kind) => ConfirmingShopItem = kind;

    public void CancelItemConfirm() => ConfirmingShopItem = null;

    public void CommitItemBuy(string label)
    {
        if (ConfirmingShopItem is not { } kind) return;
        ConfirmingShopItem = null;
        var bought = _engine.Buy(kind);
        Feedback = language => bought
            ? DashboardText.BoughtItem(language, label)
            : DashboardText.CannotBuyItem(language, label);
    }

    public void BeginEggConfirm(Rarity? tier) =>
        EggConfirm = (tier, EggConfirmStage.Confirm);

    public void CancelEggConfirm() => EggConfirm = (null, EggConfirmStage.None);

    /// <summary>
    /// macOS EggCard parity: the first buy click on a shiny companion escalates
    /// to the discard warning; otherwise the purchase commits immediately.
    /// </summary>
    public void AdvanceEggConfirm(string label, bool currentIsShiny)
    {
        if (EggConfirm.Stage == EggConfirmStage.Confirm && currentIsShiny)
        {
            EggConfirm = (EggConfirm.Tier, EggConfirmStage.ShinyWarning);
            return;
        }
        CommitEggBuy(label);
    }

    public void CommitEggBuy(string label)
    {
        var tier = EggConfirm.Tier;
        EggConfirm = (null, EggConfirmStage.None);
        var bought = _engine.BuyEgg(tier);
        Feedback = language => bought
            ? DashboardText.BoughtItem(language, label)
            : DashboardText.CannotBuyItem(language, label);
    }

    public void SetFeedback(Func<AppLanguage, string>? feedback) => Feedback = feedback;

    public void Reset()
    {
        ConfirmingBagItem = null;
        ConfirmingShopItem = null;
        EggConfirm = (null, EggConfirmStage.None);
    }
}
