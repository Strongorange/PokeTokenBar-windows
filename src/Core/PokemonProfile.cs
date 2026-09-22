namespace PokeTokenBar.Core;

public enum PokemonGender
{
    Male,
    Female,
    Genderless
}

public static class PokemonGenders
{
    public static string Raw(PokemonGender gender) => gender switch
    {
        PokemonGender.Male => "male",
        PokemonGender.Female => "female",
        PokemonGender.Genderless => "genderless",
        _ => "male"
    };

    public static PokemonGender? Parse(string? raw) => raw switch
    {
        "male" => PokemonGender.Male,
        "female" => PokemonGender.Female,
        "genderless" => PokemonGender.Genderless,
        _ => null
    };
}

public class PokemonIVs
{
    public int Hp { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int SpecialAttack { get; set; }
    public int SpecialDefense { get; set; }
    public int Speed { get; set; }

    public int this[string stat] => stat switch
    {
        "hp" => Hp,
        "attack" => Attack,
        "defense" => Defense,
        "special-attack" => SpecialAttack,
        "special-defense" => SpecialDefense,
        "speed" => Speed,
        _ => 0
    };
}

public class PokemonKnownMove
{
    public string Name { get; set; } = "";
    public int LearnedAtLevel { get; set; }

    public PokemonKnownMove() { }

    public PokemonKnownMove(string name, int learnedAtLevel)
    {
        Name = name;
        LearnedAtLevel = learnedAtLevel;
    }
}

public class PokemonAbilityOption
{
    public string Name { get; set; } = "";
    public int Slot { get; set; }
    public bool IsHidden { get; set; }
}

public class PokemonMoveLearnMethod
{
    public string Method { get; set; } = "";
    public int Level { get; set; }
}

public class PokemonMoveOption
{
    public string Name { get; set; } = "";
    public List<PokemonMoveLearnMethod> LearnMethods { get; set; } = [];
}

public class PokemonDetails
{
    public int SpeciesID { get; set; }
    public string Name { get; set; } = "";
    public int Height { get; set; }
    public int Weight { get; set; }
    public int? BaseExperience { get; set; }
    public int GenderRate { get; set; }
    public List<string> Types { get; set; } = [];
    public Dictionary<string, int> BaseStats { get; set; } = [];
    public List<PokemonAbilityOption> Abilities { get; set; } = [];
    public List<PokemonMoveOption> Moves { get; set; } = [];

    public const string PreferredVersionGroup = "black-2-white-2";

    public int BaseStatTotal => BaseStats.Values.Sum();

    public List<PokemonKnownMove> LevelUpMovesThrough(int level)
    {
        var bestByName = new Dictionary<string, int>();
        foreach (var move in Moves)
        {
            var learnedAt = move.LearnMethods
                .Where(m => m.Method == "level-up" && m.Level <= level)
                .Select(m => (int?)m.Level)
                .Min();
            if (learnedAt is not { } at) continue;
            bestByName[move.Name] = Math.Min(bestByName.TryGetValue(move.Name, out var existing) ? existing : at, at);
        }
        return bestByName
            .Select(kv => new PokemonKnownMove(kv.Key, kv.Value))
            .OrderBy(m => m.LearnedAtLevel)
            .ThenBy(m => m.Name, StringComparer.Ordinal)
            .ToList();
    }
}

public class PokemonProfile
{
    public string InstanceID { get; set; } = Guid.NewGuid().ToString();
    public ulong Seed { get; set; }
    public PokemonGender? Gender { get; set; }
    public PokemonIVs IVs { get; set; } = new();
    public int? AbilitySlot { get; set; }
    public string? AbilityName { get; set; }
    public bool AbilityIsHidden { get; set; }
    public int Level { get; set; } = 5;
    public long GrowthTokens { get; set; }
    public List<PokemonKnownMove> Moves { get; set; } = [];

    public static PokemonProfile Generate(ulong seed, long growthTokens = 0, string? instanceID = null)
    {
        var random = new ProfileRng(seed);
        var ivs = new PokemonIVs
        {
            Hp = (int)(random.Next() % 32),
            Attack = (int)(random.Next() % 32),
            Defense = (int)(random.Next() % 32),
            SpecialAttack = (int)(random.Next() % 32),
            SpecialDefense = (int)(random.Next() % 32),
            Speed = (int)(random.Next() % 32)
        };
        return new PokemonProfile
        {
            InstanceID = instanceID ?? Guid.NewGuid().ToString(),
            Seed = seed,
            Gender = null,
            IVs = ivs,
            AbilitySlot = null,
            AbilityName = null,
            AbilityIsHidden = false,
            Level = 5,
            GrowthTokens = Math.Max(0, growthTokens),
            Moves = []
        };
    }

    public void RebaseForSpeciesIdentity(Rarity oldRarity, Rarity rarity)
    {
        var fraction = Math.Min(1, Math.Max(0, GrowthTokens) / (double)PokemonBalance.GraduationTotal(oldRarity));
        var rebasedGrowth = (long)Math.Floor(fraction * PokemonBalance.GraduationTotal(rarity));
        Gender = null;
        AbilitySlot = null;
        AbilityName = null;
        AbilityIsHidden = false;
        Moves = [];
        GrowthTokens = rebasedGrowth;
        AdvanceGrowth(rebasedGrowth, rarity);
    }

    public void Enrich(PokemonDetails details)
    {
        var random = new ProfileRng(Seed ^ 0xA11B_1E5D_9EED);
        if (Gender is null)
        {
            Gender = details.GenderRate < 0
                ? PokemonGender.Genderless
                : (int)(random.Next() % 8) < details.GenderRate ? PokemonGender.Female : PokemonGender.Male;
        }

        var normal = details.Abilities.Where(a => !a.IsHidden).OrderBy(a => a.Slot).ToList();
        var hidden = details.Abilities.Where(a => a.IsHidden).OrderBy(a => a.Slot).ToList();
        if (AbilitySlot is null)
        {
            if (hidden.Count > 0 && random.Next() % 128 == 0)
            {
                AbilitySlot = hidden[(int)(random.Next() % (ulong)hidden.Count)].Slot;
                AbilityIsHidden = true;
            }
            else if (normal.Count > 0)
            {
                AbilitySlot = normal[(int)(random.Next() % (ulong)normal.Count)].Slot;
                AbilityIsHidden = false;
            }
            else if (details.Abilities.OrderBy(a => a.Slot).FirstOrDefault() is { } fallback)
            {
                AbilitySlot = fallback.Slot;
                AbilityIsHidden = fallback.IsHidden;
            }
        }
        var chosen = details.Abilities.FirstOrDefault(a => a.Slot == AbilitySlot && a.IsHidden == AbilityIsHidden)
                     ?? details.Abilities.FirstOrDefault(a => a.Slot == AbilitySlot)
                     ?? normal.FirstOrDefault()
                     ?? hidden.FirstOrDefault();
        AbilityName = chosen?.Name;
        if (chosen is not null)
        {
            AbilitySlot = chosen.Slot;
            AbilityIsHidden = chosen.IsHidden;
        }

        Moves = details.LevelUpMovesThrough(Level).TakeLast(4).ToList();
    }

    public void ApplyGrowth(long delta, Rarity rarity) =>
        AdvanceGrowth(GrowthTokens + Math.Max(0, delta), rarity);

    public void AdvanceGrowth(long candidate, Rarity rarity)
    {
        GrowthTokens = Math.Min(SaveTransfer.MaxTokenValue, Math.Max(GrowthTokens, Math.Max(0, candidate)));
        var total = Math.Max(1, PokemonBalance.GraduationTotal(rarity));
        var progress = Math.Min(1, GrowthTokens / (double)total);
        Level = Math.Min(100, Math.Max(Level, Math.Max(5, 5 + (int)Math.Floor(progress * 95))));
    }

    public void Sanitize()
    {
        Level = Math.Clamp(Level, 5, 100);
        GrowthTokens = Math.Clamp(GrowthTokens, 0, SaveTransfer.MaxTokenValue);
        IVs.Hp = Math.Clamp(IVs.Hp, 0, 31);
        IVs.Attack = Math.Clamp(IVs.Attack, 0, 31);
        IVs.Defense = Math.Clamp(IVs.Defense, 0, 31);
        IVs.SpecialAttack = Math.Clamp(IVs.SpecialAttack, 0, 31);
        IVs.SpecialDefense = Math.Clamp(IVs.SpecialDefense, 0, 31);
        IVs.Speed = Math.Clamp(IVs.Speed, 0, 31);
        Moves = Moves.Take(4)
            .Select(m => new PokemonKnownMove(m.Name.Length > 80 ? m.Name[..80] : m.Name,
                Math.Clamp(m.LearnedAtLevel, 0, 100)))
            .ToList();
        if (InstanceID.Length == 0) InstanceID = Guid.NewGuid().ToString();
    }
}

public readonly record struct PokemonComputedStat(string Name, int Base, int Iv, int Value);

public static class PokemonStatCalculator
{
    public static readonly string[] Order =
        ["hp", "attack", "defense", "special-attack", "special-defense", "speed"];

    public static int DisplayScaleMaximum(int[] values)
    {
        var highest = Math.Max(300, values.Length == 0 ? 300 : values.Max());
        var remainder = highest % 100;
        return remainder == 0 ? highest : highest + (100 - remainder);
    }

    public static List<PokemonComputedStat> Stats(PokemonDetails details, PokemonProfile profile, PokemonNature? nature)
    {
        var result = new List<PokemonComputedStat>();
        foreach (var name in Order)
        {
            if (!details.BaseStats.TryGetValue(name, out var baseStat)) continue;
            var iv = profile.IVs[name];
            var level = profile.Level;
            int value;
            if (name == "hp")
            {
                value = (2 * baseStat + iv) * level / 100 + level + 10;
            }
            else
            {
                var neutral = (2 * baseStat + iv) * level / 100 + 5;
                var modifier = nature?.Modifier(name) ?? 1.0;
                value = (int)Math.Floor(neutral * modifier);
            }
            result.Add(new PokemonComputedStat(name, baseStat, iv, value));
        }
        return result;
    }
}

public struct ProfileRng
{
    private ulong _state;

    public ProfileRng(ulong seed) => _state = seed == 0 ? 0x9E37_79B9_7F4A_7C15 : seed;

    public ulong Next()
    {
        _state = unchecked(_state + 0x9E37_79B9_7F4A_7C15);
        var value = _state;
        value = unchecked((value ^ (value >> 30)) * 0xBF58_476D_1CE4_E5B9);
        value = unchecked((value ^ (value >> 27)) * 0x94D0_49BB_1331_11EB);
        return value ^ (value >> 31);
    }
}

public static class PokemonProfileMigration
{
    public static ulong Seed(string text)
    {
        var hash = 0xcbf2_9ce4_8422_2325ul;
        foreach (var b in System.Text.Encoding.UTF8.GetBytes(text))
            hash = unchecked((hash ^ b) * 0x0000_0100_0000_01B3);
        return hash;
    }
}
