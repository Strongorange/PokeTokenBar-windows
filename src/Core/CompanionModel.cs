using System.Text.Json;
using System.Text.Json.Nodes;

namespace PokeTokenBar.Core;

public class MonState
{
    public int BaseID { get; set; }
    public List<int> PathIDs { get; set; } = [];
    public List<int> PlannedPathIDs { get; set; } = [];
    public int StageIndex { get; set; }
    public long UsedAtStage { get; set; }
    public Rarity Rarity { get; set; }
    public int TotalForms { get; set; }
    public bool IsShiny { get; set; }
    public PokemonNature? Nature { get; set; }
    public UnownForm? UnownForm { get; set; }
    public PokemonProfile? Profile { get; set; }
    public bool HasGrowthBoost { get; set; }
    public int? DittoDisguise { get; set; }
    public bool DittoRevealed { get; set; }

    public int CurrentID => PathIDs.Count == 0
        ? BaseID
        : PathIDs[Math.Min(StageIndex, PathIDs.Count - 1)];

    public long PhaseThreshold => PokemonBalance.PhaseThreshold(
        Rarity, TotalForms, StageIndex,
        HasGrowthBoost ? PokemonBalance.RepeatGrowthMultiplier : 1);

    public MonState() { }

    public MonState(int baseID, List<int> pathIDs, List<int>? plannedPathIDs, int stageIndex, long usedAtStage,
        Rarity rarity, int totalForms, bool isShiny = false, PokemonNature? nature = null,
        PokemonProfile? profile = null, bool hasGrowthBoost = false,
        int? dittoDisguise = null, bool dittoRevealed = false, UnownForm? unownForm = null)
    {
        BaseID = baseID;
        PathIDs = pathIDs;
        PlannedPathIDs = plannedPathIDs is { Count: > 0 } ? plannedPathIDs : pathIDs;
        StageIndex = stageIndex;
        UsedAtStage = usedAtStage;
        Rarity = rarity;
        TotalForms = totalForms;
        IsShiny = isShiny;
        Nature = nature;
        Profile = profile;
        HasGrowthBoost = hasGrowthBoost;
        DittoDisguise = dittoDisguise;
        DittoRevealed = dittoRevealed;
        UnownForm = UnownForms.Resolved(baseID, unownForm);
    }
}

public class DexEntry
{
    public const int CurrentNamesVersion = 1;

    public string ID { get; set; } = Guid.NewGuid().ToString();
    public int BaseID { get; set; }
    public int FinalID { get; set; }
    public List<int> ChainOrder { get; set; } = [];
    public Rarity Rarity { get; set; }
    public DateTimeOffset? CaughtAt { get; set; }
    public bool IsShiny { get; set; }
    public PokemonNature? Nature { get; set; }
    public UnownForm? UnownForm { get; set; }
    public PokemonProfile? Profile { get; set; }
    public Dictionary<int, Dictionary<string, string>>? Names { get; set; }
    public int? NamesVersion { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }

    public bool NeedsNamesRefresh =>
        NamesVersion != CurrentNamesVersion
        || ChainOrder.Any(id => Names is null || !Names.TryGetValue(id, out var byLang) || byLang.Count == 0);

    public bool IsReleased => ReleasedAt is not null;

    public DexEntry() { }

    public DexEntry(int baseID, int finalID, List<int> chainOrder, Rarity rarity,
        DateTimeOffset? caughtAt, bool isShiny = false, PokemonNature? nature = null,
        PokemonProfile? profile = null, Dictionary<int, Dictionary<string, string>>? names = null,
        DateTimeOffset? releasedAt = null, UnownForm? unownForm = null,
        string? id = null)
    {
        ID = id ?? Guid.NewGuid().ToString();
        BaseID = baseID;
        FinalID = finalID;
        ChainOrder = chainOrder;
        Rarity = rarity;
        CaughtAt = caughtAt;
        IsShiny = isShiny;
        Nature = nature;
        Profile = profile;
        Names = names;
        NamesVersion = chainOrder.All(id2 => names is not null && names.TryGetValue(id2, out var byLang) && byLang.Count > 0)
            ? CurrentNamesVersion
            : null;
        ReleasedAt = releasedAt;
        UnownForm = UnownForms.Resolved(baseID, unownForm);
    }
}

public class CompanionState
{
    public bool InstallBaselineSet { get; set; }
    public long UsedSinceInstall { get; set; }
    public long SpentTokens { get; set; }
    public long EggUsage { get; set; }
    public Rarity? EggTier { get; set; }
    public int? PendingHatchID { get; set; }
    public UnownForm? PendingUnownForm { get; set; }
    public Dictionary<string, long>? ClaimedTodayTokensByProvider { get; set; }
    public string LastDate { get; set; } = "";
    public MonState? Active { get; set; }
    public int? RepresentativeSpeciesID { get; set; }
    public UnownForm? RepresentativeUnownForm { get; set; }
    public List<DexEntry> Dex { get; set; } = [];
    public HashSet<string> CollectedFinals { get; set; } = [];
    public AppLanguage Language { get; set; } = AppLanguages.SystemDefault();
    public Dictionary<string, long> Inventory { get; set; } = [];
    public Dictionary<string, int> CandyGrantTier { get; set; } = [];
    public bool CandyFeatureSeeded { get; set; }

    public bool OwnsSpecies(int speciesID, UnownForm? unownForm = null)
    {
        var form = UnownForms.Resolved(speciesID, unownForm);
        if (Dex.Any(entry => entry.ChainOrder.Contains(speciesID)
                && UnownForms.Resolved(speciesID, entry.UnownForm) == form))
            return true;
        if (Active is null) return false;
        return Active.PathIDs.Take(Active.StageIndex + 1).Contains(speciesID)
               && UnownForms.Resolved(speciesID, Active.UnownForm) == form;
    }

    public bool HasCollectedFinal(int baseID) =>
        CollectedFinals.Any(key => key.StartsWith($"{baseID}:", StringComparison.Ordinal));

    public HashSet<UnownForm> CollectedUnownForms()
    {
        var forms = Dex
            .Where(entry => entry.ChainOrder.Contains(UnownForms.SpeciesID))
            .Select(entry => UnownForms.Resolved(UnownForms.SpeciesID, entry.UnownForm))
            .Where(f => f is not null)
            .Select(f => f!.Value)
            .ToHashSet();
        if (Active is not null
            && Active.PathIDs.Take(Active.StageIndex + 1).Contains(UnownForms.SpeciesID)
            && UnownForms.Resolved(UnownForms.SpeciesID, Active.UnownForm) is { } activeForm)
        {
            forms.Add(activeForm);
        }
        return forms;
    }

    public bool OwnsShinySpecies(int speciesID, UnownForm? unownForm = null)
    {
        var form = UnownForms.Resolved(speciesID, unownForm);
        if (Dex.Any(entry => entry.IsShiny
                && entry.ChainOrder.Contains(speciesID)
                && UnownForms.Resolved(speciesID, entry.UnownForm) == form))
            return true;
        if (Active is null) return false;
        if (!Active.PathIDs.Take(Active.StageIndex + 1).Contains(speciesID)) return false;
        if (UnownForms.Resolved(speciesID, Active.UnownForm) != form) return false;
        if (!Active.IsShiny) return false;
        return Active.DittoDisguise is null || Active.DittoRevealed;
    }

    public void ReconcileRepresentativeSelection()
    {
        if (RepresentativeSpeciesID is not { } selected)
        {
            RepresentativeUnownForm = null;
            return;
        }
        RepresentativeUnownForm = UnownForms.Resolved(selected, RepresentativeUnownForm);
        if (!OwnsSpecies(selected, RepresentativeUnownForm))
        {
            RepresentativeSpeciesID = null;
            RepresentativeUnownForm = null;
        }
    }
}

public enum SaveDateMode
{
    ReferenceEpochDouble,
    Iso8601
}

public static class CompanionStateCodec
{
    private static readonly DateTimeOffset ReferenceEpoch = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static DateTimeOffset? ReadDate(JsonElement e)
    {
        switch (e.ValueKind)
        {
            case JsonValueKind.Number:
                {
                    var seconds = e.GetDouble();
                    if (!double.IsFinite(seconds)) return null;
                    return ReferenceEpoch + TimeSpan.FromSeconds(seconds);
                }
            case JsonValueKind.String:
                return IsoDates.Date(e.GetString());
            default:
                return null;
        }
    }

    public static JsonNode WriteDate(DateTimeOffset date, SaveDateMode mode)
    {
        if (mode == SaveDateMode.Iso8601)
            return JsonValue.Create(IsoDates.Format(date))!;
        var seconds = (date - ReferenceEpoch).TotalSeconds;
        return JsonValue.Create(seconds)!;
    }

    public static PokemonProfile? ReadProfile(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        try
        {
            var profile = new PokemonProfile
            {
                InstanceID = e.TryGetProperty("instanceID", out var iid) && iid.ValueKind == JsonValueKind.String
                    ? iid.GetString()! : "",
                Seed = e.TryGetProperty("seed", out var seed) && seed.ValueKind == JsonValueKind.Number
                    ? seed.GetUInt64() : 0,
                Gender = e.TryGetProperty("gender", out var g) ? PokemonGenders.Parse(g.GetString()) : null,
                IVs = ReadIVs(e),
                AbilitySlot = e.TryGetProperty("abilitySlot", out var slot) && slot.ValueKind == JsonValueKind.Number
                    ? slot.GetInt32() : null,
                AbilityName = e.TryGetProperty("abilityName", out var an) && an.ValueKind == JsonValueKind.String
                    ? an.GetString() : null,
                AbilityIsHidden = e.TryGetProperty("abilityIsHidden", out var ah) && ah.ValueKind == JsonValueKind.True,
                Level = e.TryGetProperty("level", out var lvl) && lvl.ValueKind == JsonValueKind.Number ? lvl.GetInt32() : 5,
                GrowthTokens = e.TryGetProperty("growthTokens", out var gt) && gt.ValueKind == JsonValueKind.Number
                    ? gt.GetInt64() : 0,
                Moves = []
            };
            if (e.TryGetProperty("moves", out var moves) && moves.ValueKind == JsonValueKind.Array)
            {
                foreach (var move in moves.EnumerateArray())
                {
                    if (move.ValueKind != JsonValueKind.Object) continue;
                    var name = move.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
                        ? n.GetString()! : "";
                    var level = move.TryGetProperty("learnedAtLevel", out var l) && l.ValueKind == JsonValueKind.Number
                        ? l.GetInt32() : 0;
                    profile.Moves.Add(new PokemonKnownMove(name, level));
                }
            }
            return profile;
        }
        catch
        {
            return null;
        }
    }

    private static PokemonIVs ReadIVs(JsonElement e)
    {
        var ivs = new PokemonIVs();
        if (e.TryGetProperty("ivs", out var v) && v.ValueKind == JsonValueKind.Object)
        {
            ivs.Hp = ReadInt(v, "hp");
            ivs.Attack = ReadInt(v, "attack");
            ivs.Defense = ReadInt(v, "defense");
            ivs.SpecialAttack = ReadInt(v, "specialAttack");
            ivs.SpecialDefense = ReadInt(v, "specialDefense");
            ivs.Speed = ReadInt(v, "speed");
        }
        return ivs;
    }

    private static int ReadInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    public static JsonObject WriteProfile(PokemonProfile profile, SaveDateMode mode)
    {
        var o = new JsonObject
        {
            ["instanceID"] = profile.InstanceID,
            ["seed"] = profile.Seed,
        };
        if (profile.Gender is { } gender) o["gender"] = PokemonGenders.Raw(gender);
        o["ivs"] = new JsonObject
        {
            ["hp"] = profile.IVs.Hp,
            ["attack"] = profile.IVs.Attack,
            ["defense"] = profile.IVs.Defense,
            ["specialAttack"] = profile.IVs.SpecialAttack,
            ["specialDefense"] = profile.IVs.SpecialDefense,
            ["speed"] = profile.IVs.Speed
        };
        if (profile.AbilitySlot is { } slot) o["abilitySlot"] = slot;
        if (profile.AbilityName is { } abilityName) o["abilityName"] = abilityName;
        o["abilityIsHidden"] = profile.AbilityIsHidden;
        o["level"] = profile.Level;
        o["growthTokens"] = profile.GrowthTokens;
        o["moves"] = new JsonArray(profile.Moves
            .Select(m => (JsonNode)new JsonObject
            {
                ["name"] = m.Name,
                ["learnedAtLevel"] = m.LearnedAtLevel
            })
            .ToArray());
        return o;
    }

    public static MonState? ReadMon(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        try
        {
            foreach (var required in new[] { "baseID", "stageIndex", "usedAtStage", "totalForms" })
                if (!e.TryGetProperty(required, out var v) || v.ValueKind != JsonValueKind.Number) return null;
            if (!e.TryGetProperty("rarity", out var rarityElement) || rarityElement.ValueKind != JsonValueKind.String) return null;
            var rarity = Rarities.Parse(rarityElement.GetString());
            if (rarity is null) return null;
            var baseID = e.GetProperty("baseID").GetInt32();
            var pathIDs = new List<int>();
            if (e.TryGetProperty("pathIDs", out var paths) && paths.ValueKind == JsonValueKind.Array)
                foreach (var p in paths.EnumerateArray())
                    if (p.ValueKind == JsonValueKind.Number) pathIDs.Add(p.GetInt32());
            if (pathIDs.Count == 0) return null;
            var planned = new List<int>();
            if (e.TryGetProperty("plannedPathIDs", out var plan) && plan.ValueKind == JsonValueKind.Array)
                foreach (var p in plan.EnumerateArray())
                    if (p.ValueKind == JsonValueKind.Number) planned.Add(p.GetInt32());
            var decodedStage = e.GetProperty("stageIndex").GetInt32();
            return new MonState(
                baseID,
                pathIDs,
                planned,
                Math.Clamp(decodedStage, 0, pathIDs.Count - 1),
                e.GetProperty("usedAtStage").GetInt64(),
                rarity.Value,
                e.GetProperty("totalForms").GetInt32(),
                e.TryGetProperty("isShiny", out var sh) && sh.ValueKind == JsonValueKind.True,
                e.TryGetProperty("nature", out var n) ? PokemonNatures.Parse(n.GetString()) : null,
                e.TryGetProperty("profile", out var p2) ? ReadProfile(p2) : null,
                e.TryGetProperty("hasGrowthBoost", out var hb) && hb.ValueKind == JsonValueKind.True,
                e.TryGetProperty("dittoDisguise", out var dd) && dd.ValueKind == JsonValueKind.Number ? dd.GetInt32() : null,
                e.TryGetProperty("dittoRevealed", out var dr) && dr.ValueKind == JsonValueKind.True,
                e.TryGetProperty("unownForm", out var uf) ? UnownForms.Parse(uf.GetString()) : null);
        }
        catch
        {
            return null;
        }
    }

    public static JsonObject WriteMon(MonState mon, SaveDateMode mode)
    {
        var o = new JsonObject
        {
            ["baseID"] = mon.BaseID,
            ["pathIDs"] = new JsonArray(mon.PathIDs.Select(p => (JsonNode)p).ToArray()),
            ["plannedPathIDs"] = new JsonArray(mon.PlannedPathIDs.Select(p => (JsonNode)p).ToArray()),
            ["stageIndex"] = mon.StageIndex,
            ["usedAtStage"] = mon.UsedAtStage,
            ["rarity"] = Rarities.Raw(mon.Rarity),
            ["totalForms"] = mon.TotalForms,
            ["isShiny"] = mon.IsShiny
        };
        if (mon.Nature is { } nature) o["nature"] = PokemonNatures.Raw(nature);
        if (mon.UnownForm is { } unown) o["unownForm"] = UnownForms.Raw(unown);
        if (mon.Profile is { } profile) o["profile"] = WriteProfile(profile, mode);
        o["hasGrowthBoost"] = mon.HasGrowthBoost;
        if (mon.DittoDisguise is { } disguise) o["dittoDisguise"] = disguise;
        o["dittoRevealed"] = mon.DittoRevealed;
        return o;
    }

    public static DexEntry? ReadDexEntry(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object) return null;
        try
        {
            if (!e.TryGetProperty("baseID", out var b) || b.ValueKind != JsonValueKind.Number) return null;
            if (!e.TryGetProperty("finalID", out var f) || f.ValueKind != JsonValueKind.Number) return null;
            if (!e.TryGetProperty("rarity", out var r) || r.ValueKind != JsonValueKind.String) return null;
            var rarity = Rarities.Parse(r.GetString());
            if (rarity is null) return null;
            if (!e.TryGetProperty("chainOrder", out var chain) || chain.ValueKind != JsonValueKind.Array) return null;
            var baseID = b.GetInt32();
            var chainOrder = new List<int>();
            foreach (var c in chain.EnumerateArray())
                if (c.ValueKind == JsonValueKind.Number) chainOrder.Add(c.GetInt32());
            var entry = new DexEntry(
                baseID,
                f.GetInt32(),
                chainOrder,
                rarity.Value,
                e.TryGetProperty("caughtAt", out var ca) ? ReadDate(ca) : null,
                e.TryGetProperty("isShiny", out var sh) && sh.ValueKind == JsonValueKind.True,
                e.TryGetProperty("nature", out var n) ? PokemonNatures.Parse(n.GetString()) : null,
                e.TryGetProperty("profile", out var p) ? ReadProfile(p) : null,
                ReadNames(e),
                e.TryGetProperty("releasedAt", out var ra) ? ReadDate(ra) : null,
                e.TryGetProperty("unownForm", out var uf) ? UnownForms.Parse(uf.GetString()) : null,
                e.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String ? id.GetString() : null);
            if (e.TryGetProperty("namesVersion", out var nv) && nv.ValueKind == JsonValueKind.Number)
                entry.NamesVersion = nv.GetInt32();
            return entry;
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<int, Dictionary<string, string>>? ReadNames(JsonElement e)
    {
        if (!e.TryGetProperty("names", out var names) || names.ValueKind != JsonValueKind.Object)
            return null;
        var result = new Dictionary<int, Dictionary<string, string>>();
        foreach (var prop in names.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out var speciesID) || prop.Value.ValueKind != JsonValueKind.Object) continue;
            var byLang = new Dictionary<string, string>();
            foreach (var lang in prop.Value.EnumerateObject())
                if (lang.Value.ValueKind == JsonValueKind.String)
                    byLang[lang.Name] = lang.Value.GetString()!;
            result[speciesID] = byLang;
        }
        return result;
    }

    public static JsonObject WriteDexEntry(DexEntry entry, SaveDateMode mode)
    {
        var o = new JsonObject
        {
            ["id"] = entry.ID,
            ["baseID"] = entry.BaseID,
            ["finalID"] = entry.FinalID,
            ["chainOrder"] = new JsonArray(entry.ChainOrder.Select(c => (JsonNode)c).ToArray()),
            ["rarity"] = Rarities.Raw(entry.Rarity)
        };
        if (entry.CaughtAt is { } caughtAt) o["caughtAt"] = WriteDate(caughtAt, mode);
        o["isShiny"] = entry.IsShiny;
        if (entry.Nature is { } nature) o["nature"] = PokemonNatures.Raw(nature);
        if (entry.UnownForm is { } unown) o["unownForm"] = UnownForms.Raw(unown);
        if (entry.Profile is { } profile) o["profile"] = WriteProfile(profile, mode);
        if (entry.Names is { } names)
        {
            var namesObject = new JsonObject();
            foreach (var species in names.Keys.OrderBy(k => k))
            {
                namesObject[species.ToString()] = new JsonObject(names[species]
                    .Select(kv => KeyValuePair.Create<string, JsonNode?>(kv.Key, kv.Value)));
            }
            o["names"] = namesObject;
        }
        if (entry.NamesVersion is { } version) o["namesVersion"] = version;
        if (entry.ReleasedAt is { } releasedAt) o["releasedAt"] = WriteDate(releasedAt, mode);
        return o;
    }

    public static CompanionState Read(JsonElement e)
    {
        var state = new CompanionState();
        if (e.ValueKind != JsonValueKind.Object) throw new JsonException("state is not an object");

        bool ReadBool(string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;
        long ReadLong(string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt64() : 0;
        string ReadString(string name) =>
            e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

        state.InstallBaselineSet = ReadBool("installBaselineSet");
        state.UsedSinceInstall = ReadLong("usedSinceInstall");
        state.SpentTokens = ReadLong("spentTokens");
        state.EggUsage = ReadLong("eggUsage");
        state.EggTier = e.TryGetProperty("eggTier", out var et) ? Rarities.Parse(et.GetString()) : null;
        state.PendingHatchID = e.TryGetProperty("pendingHatchID", out var ph) && ph.ValueKind == JsonValueKind.Number
            ? ph.GetInt32() : null;
        state.PendingUnownForm = UnownForms.Resolved(state.PendingHatchID ?? 0,
            e.TryGetProperty("pendingUnownForm", out var puf) ? UnownForms.Parse(puf.GetString()) : null);
        if (e.TryGetProperty("claimedTodayTokensByProvider", out var claimed))
        {
            var map = new Dictionary<string, long>();
            if (claimed.ValueKind == JsonValueKind.Object)
                foreach (var prop in claimed.EnumerateObject())
                    if (prop.Value.ValueKind == JsonValueKind.Number)
                        map[prop.Name] = prop.Value.GetInt64();
            state.ClaimedTodayTokensByProvider = map;
        }
        state.LastDate = ReadString("lastDate");
        state.Active = e.TryGetProperty("active", out var active) ? ReadMon(active) : null;
        state.RepresentativeSpeciesID = e.TryGetProperty("representativeSpeciesID", out var rs)
            && rs.ValueKind == JsonValueKind.Number ? rs.GetInt32() : null;
        state.RepresentativeUnownForm = UnownForms.Resolved(state.RepresentativeSpeciesID ?? 0,
            e.TryGetProperty("representativeUnownForm", out var ruf) ? UnownForms.Parse(ruf.GetString()) : null);
        var dex = new List<DexEntry>();
        if (e.TryGetProperty("dex", out var dexElement) && dexElement.ValueKind == JsonValueKind.Array)
            foreach (var item in dexElement.EnumerateArray())
            {
                var entry = ReadDexEntry(item);
                if (entry is not null) dex.Add(entry);
            }
        state.Dex = dex;
        var finals = new HashSet<string>();
        if (e.TryGetProperty("collectedFinals", out var cf) && cf.ValueKind == JsonValueKind.Array)
            foreach (var item in cf.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String) finals.Add(item.GetString()!);
        state.CollectedFinals = finals;
        state.Language = e.TryGetProperty("language", out var lang)
            ? AppLanguages.Parse(lang.GetString()) ?? AppLanguages.SystemDefault()
            : AppLanguages.SystemDefault();
        var inventory = new Dictionary<string, long>();
        if (e.TryGetProperty("inventory", out var inv) && inv.ValueKind == JsonValueKind.Object)
            foreach (var prop in inv.EnumerateObject())
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    inventory[prop.Name] = prop.Value.GetInt64();
        state.Inventory = inventory;
        var grantTier = new Dictionary<string, int>();
        if (e.TryGetProperty("candyGrantTier", out var cgt) && cgt.ValueKind == JsonValueKind.Object)
            foreach (var prop in cgt.EnumerateObject())
                if (prop.Value.ValueKind == JsonValueKind.Number)
                    grantTier[prop.Name] = prop.Value.GetInt32();
        state.CandyGrantTier = grantTier;
        state.CandyFeatureSeeded = ReadBool("candyFeatureSeeded");
        return state;
    }

    public static JsonObject Write(CompanionState state, SaveDateMode mode)
    {
        var o = new JsonObject
        {
            ["installBaselineSet"] = state.InstallBaselineSet,
            ["usedSinceInstall"] = state.UsedSinceInstall,
            ["spentTokens"] = state.SpentTokens,
            ["eggUsage"] = state.EggUsage
        };
        if (state.EggTier is { } tier) o["eggTier"] = Rarities.Raw(tier);
        if (state.PendingHatchID is { } pending) o["pendingHatchID"] = pending;
        if (state.PendingUnownForm is { } pendingForm) o["pendingUnownForm"] = UnownForms.Raw(pendingForm);
        if (state.ClaimedTodayTokensByProvider is { } claimed)
        {
            o["claimedTodayTokensByProvider"] = new JsonObject(claimed
                .Select(kv => KeyValuePair.Create<string, JsonNode?>(kv.Key, kv.Value)));
        }
        o["lastDate"] = state.LastDate;
        if (state.Active is { } active) o["active"] = WriteMon(active, mode);
        if (state.RepresentativeSpeciesID is { } representative) o["representativeSpeciesID"] = representative;
        if (state.RepresentativeUnownForm is { } representativeForm)
            o["representativeUnownForm"] = UnownForms.Raw(representativeForm);
        o["dex"] = new JsonArray(state.Dex
            .Select(entry => (JsonNode)WriteDexEntry(entry, mode))
            .ToArray());
        o["collectedFinals"] = new JsonArray(state.CollectedFinals
            .OrderBy(k => k, StringComparer.Ordinal)
            .Select(k => (JsonNode)k)
            .ToArray());
        o["language"] = AppLanguages.Raw(state.Language);
        o["inventory"] = new JsonObject(state.Inventory
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => KeyValuePair.Create<string, JsonNode?>(kv.Key, kv.Value)));
        o["candyGrantTier"] = new JsonObject(state.CandyGrantTier
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => KeyValuePair.Create<string, JsonNode?>(kv.Key, kv.Value)));
        o["candyFeatureSeeded"] = state.CandyFeatureSeeded;
        return o;
    }
}
