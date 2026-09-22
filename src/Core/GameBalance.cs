namespace PokeTokenBar.Core;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Legendary
}

public static class Rarities
{
    public static string Raw(Rarity rarity) => rarity switch
    {
        Rarity.Common => "common",
        Rarity.Uncommon => "uncommon",
        Rarity.Rare => "rare",
        Rarity.Legendary => "legendary",
        _ => "common"
    };

    public static Rarity? Parse(string? raw)
    {
        if (raw is null) return null;
        foreach (var rarity in Enum.GetValues<Rarity>())
            if (Raw(rarity) == raw) return rarity;
        return null;
    }

    public static int SortRank(this Rarity rarity) => rarity switch
    {
        Rarity.Common => 0,
        Rarity.Uncommon => 1,
        Rarity.Rare => 2,
        Rarity.Legendary => 3,
        _ => 0
    };

    public static int? CaptureRateCeiling(this Rarity rarity) => rarity switch
    {
        Rarity.Rare => 45,
        Rarity.Uncommon => 120,
        Rarity.Common => 255,
        Rarity.Legendary => null,
        _ => null
    };

    public static bool Includes(this Rarity rarity, int captureRate) =>
        rarity.CaptureRateCeiling() is { } ceiling && captureRate <= ceiling;

    public static Rarity From(int captureRate, bool isLegendary, bool isMythical)
    {
        if (isLegendary || isMythical) return Rarity.Legendary;
        if (Rarity.Rare.Includes(captureRate)) return Rarity.Rare;
        if (Rarity.Uncommon.Includes(captureRate)) return Rarity.Uncommon;
        return Rarity.Common;
    }
}

public static class PokemonBalance
{
    public const long EggHatchThreshold = 5_000_000;
    public const int RepeatGrowthMultiplier = 2;

    public static long GraduationTotal(Rarity rarity) => rarity switch
    {
        Rarity.Common => 750_000_000,
        Rarity.Uncommon => 1_875_000_000,
        Rarity.Rare => 3_000_000_000,
        Rarity.Legendary => 6_000_000_000,
        _ => 750_000_000
    };

    public static long PhaseThreshold(Rarity rarity, int totalForms, int stageIndex, int growthMultiplier = 1)
    {
        var kk = Math.Max(1, totalForms);
        var i = stageIndex + 1;
        var total = (double)GraduationTotal(rarity);
        var denom = kk * (kk + 1) / 2.0;
        var standardThreshold = (long)Math.Round(total * i / denom, MidpointRounding.AwayFromZero);
        return Math.Max(1, (long)Math.Round(standardThreshold / (double)Math.Max(1, growthMultiplier),
            MidpointRounding.AwayFromZero));
    }

    public const double DifficultyLowerBound = 0.1;
    public const double DifficultyUpperBound = 2.0;
    public const double DefaultDifficulty = 1.0;

    public static double ClampDifficulty(double value) =>
        double.IsFinite(value)
            ? Math.Clamp(value, DifficultyLowerBound, DifficultyUpperBound)
            : DefaultDifficulty;

    public static long Scaled(long baseline, double difficulty) =>
        (long)Math.Round(baseline * ClampDifficulty(difficulty), MidpointRounding.AwayFromZero);

    private const double DefaultSnapWidth = 0.01;

    public static double DifficultyAtPosition(double position)
    {
        var p = Math.Clamp(position, 0, 1);
        if (Math.Abs(p - DifficultyPosition(DefaultDifficulty)) < DefaultSnapWidth) return DefaultDifficulty;
        return SnapDifficulty(DifficultyLowerBound * Math.Pow(DifficultyUpperBound / DifficultyLowerBound, p));
    }

    public static double DifficultyPosition(double value) =>
        Math.Log(ClampDifficulty(value) / DifficultyLowerBound) /
        Math.Log(DifficultyUpperBound / DifficultyLowerBound);

    public static double SnapDifficulty(double value)
    {
        if (value <= 0) return DifficultyLowerBound;
        var magnitude = Math.Pow(10, Math.Floor(Math.Log10(value)) - 1);
        return Math.Round(value / magnitude, MidpointRounding.AwayFromZero) * magnitude;
    }
}

public enum ItemKind
{
    RareCandy,
    Mint,
    ShinyCharm
}

public static class ItemKinds
{
    public static string Raw(ItemKind kind) => kind switch
    {
        ItemKind.RareCandy => "rareCandy",
        ItemKind.Mint => "mint",
        ItemKind.ShinyCharm => "shinyCharm",
        _ => "rareCandy"
    };

    public static ItemKind? Parse(string? raw) => raw switch
    {
        "rareCandy" => ItemKind.RareCandy,
        "mint" => ItemKind.Mint,
        "shinyCharm" => ItemKind.ShinyCharm,
        _ => null
    };

    public static string? SpriteName(this ItemKind kind) => kind switch
    {
        ItemKind.RareCandy => "rare-candy",
        ItemKind.Mint => null,
        ItemKind.ShinyCharm => "shiny-charm",
        _ => null
    };

    public static string FallbackEmoji(this ItemKind kind) => kind switch
    {
        ItemKind.RareCandy => "🍬",
        ItemKind.Mint => "🌿",
        ItemKind.ShinyCharm => "✨",
        _ => "🍬"
    };

    public static long? ShopPrice(this ItemKind kind) => kind switch
    {
        ItemKind.RareCandy => RareCandies.Price,
        ItemKind.Mint => Mints.Price,
        ItemKind.ShinyCharm => ShinyCharms.Price,
        _ => null
    };

    public static bool IsPassive(this ItemKind kind) => kind == ItemKind.ShinyCharm;
}

public static class RareCandies
{
    public const long Xp = 100_000_000;
    public const int WeeklyGrant = 5;
    public const long Price = 500_000_000;
}

public static class Mints
{
    public const long Price = 100_000_000;
}

public static class ShinyCharms
{
    public const long Price = 3_000_000_000;
    public const ulong ShinyDenominator = 48;
}

public static class FreshEggs
{
    public const long Price = 1_000_000_000;

    public static readonly Rarity?[] ShopTiers = [null, Rarity.Uncommon, Rarity.Rare];

    public static long PriceGuaranteeing(Rarity? tier)
    {
        if (tier is null) return Price;
        var multiplier = (double)PokemonBalance.GraduationTotal(tier.Value) /
                         PokemonBalance.GraduationTotal(Rarity.Common);
        return (long)Math.Round(Price * multiplier, MidpointRounding.AwayFromZero);
    }
}

public readonly record struct ShopEntry(ItemKind? Item, Rarity? EggTier)
{
    public static ShopEntry FromItem(ItemKind kind) => new(kind, null);
    public static ShopEntry FromEgg(Rarity? tier) => new(null, tier);

    public long Price => Item is { } kind
        ? kind.ShopPrice() ?? 0
        : FreshEggs.PriceGuaranteeing(EggTier);
}

public enum WindowClass
{
    Session,
    Weekly
}

public readonly record struct CandyWindow(string Key, string Name, WindowClass Kind, double Utilization);

public readonly record struct CandyGrant(string WindowKey, string WindowName, int Count)
{
    public static int CountFor(WindowClass kind) =>
        kind == WindowClass.Weekly ? RareCandies.WeeklyGrant : 1;
}

public static class PokemonAssets
{
    public static bool HasAnimatedSprite(int speciesID) =>
        speciesID is >= 1 and <= 649;
}

public static class PokemonOdds
{
    public const ulong ShinyDenominator = 64;
    public const ulong DittoDisguiseDenominator = 128;
    public const int DittoSpeciesID = 132;

    public static bool RollsShiny(ulong roll, bool shinyCharmOwned) =>
        roll % (shinyCharmOwned ? ShinyCharms.ShinyDenominator : ShinyDenominator) == 0;

    public static bool DittoDisguiseHit(ulong roll) =>
        roll % DittoDisguiseDenominator == 0;
}
