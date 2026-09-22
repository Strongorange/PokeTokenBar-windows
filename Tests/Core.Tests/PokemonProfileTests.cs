using PokeTokenBar.Core;
using Xunit;

namespace PokeTokenBar.Core.Tests;

public class PokemonProfileTests
{
    [Fact]
    public void FnvSeedMatchesKnownVectors()
    {
        Assert.Equal(0xcbf29ce484222325ul, PokemonProfileMigration.Seed(""));
        Assert.Equal(0xaf63dc4c8601ec8cul, PokemonProfileMigration.Seed("a"));
        Assert.Equal(0x85944171f73967e8ul, PokemonProfileMigration.Seed("foobar"));
    }

    [Fact]
    public void ProfileRngSeedZeroUsesGoldenRatioConstant()
    {
        var a = new ProfileRng(0);
        var b = new ProfileRng(0x9E37_79B9_7F4A_7C15);
        Assert.Equal(a.Next(), b.Next());
    }

    [Fact]
    public void GeneratedIVsAreDeterministicAndInRange()
    {
        for (ulong seed = 1; seed < 50; seed++)
        {
            var profile = PokemonProfile.Generate(seed);
            var sameSeed = PokemonProfile.Generate(seed);
            Assert.Equal(profile.IVs.Hp, sameSeed.IVs.Hp);
            Assert.Equal(profile.IVs.Speed, sameSeed.IVs.Speed);
            Assert.InRange(profile.IVs.Hp, 0, 31);
            Assert.InRange(profile.IVs.Attack, 0, 31);
            Assert.InRange(profile.IVs.Defense, 0, 31);
            Assert.InRange(profile.IVs.SpecialAttack, 0, 31);
            Assert.InRange(profile.IVs.SpecialDefense, 0, 31);
            Assert.InRange(profile.IVs.Speed, 0, 31);
            Assert.Equal(5, profile.Level);
            Assert.Null(profile.Gender);
            Assert.Equal(0, profile.GrowthTokens);
        }
    }

    [Fact]
    public void EnrichRollsGenderFromGenderRate()
    {
        var genderless = PokemonProfile.Generate(42);
        genderless.Enrich(new PokemonDetails { GenderRate = -1 });
        Assert.Equal(PokemonGender.Genderless, genderless.Gender);

        var female = PokemonProfile.Generate(42);
        female.Enrich(new PokemonDetails { GenderRate = 8 });
        Assert.Equal(PokemonGender.Female, female.Gender);
    }

    [Fact]
    public void EnrichPicksAbilitySlots()
    {
        var details = new PokemonDetails
        {
            GenderRate = -1,
            Abilities =
            [
                new PokemonAbilityOption { Name = "static", Slot = 1 },
                new PokemonAbilityOption { Name = "lightning-rod", Slot = 2, IsHidden = true }
            ]
        };
        var profile = PokemonProfile.Generate(7);
        profile.Enrich(details);
        Assert.NotNull(profile.AbilitySlot);
        Assert.Contains(details.Abilities, a => a.Slot == profile.AbilitySlot && a.Name == profile.AbilityName);
    }

    [Fact]
    public void EnrichKeepsLastFourLevelUpMoves()
    {
        var details = new PokemonDetails
        {
            GenderRate = -1,
            Abilities = [new PokemonAbilityOption { Name = "a", Slot = 1 }],
            Moves =
            [
                new PokemonMoveOption { Name = "tackle", LearnMethods = [new PokemonMoveLearnMethod { Method = "level-up", Level = 1 }] },
                new PokemonMoveOption { Name = "growl", LearnMethods = [new PokemonMoveLearnMethod { Method = "level-up", Level = 1 }] },
                new PokemonMoveOption { Name = "thunder-shock", LearnMethods = [new PokemonMoveLearnMethod { Method = "level-up", Level = 9 }] },
                new PokemonMoveOption { Name = "tail-whip", LearnMethods = [new PokemonMoveLearnMethod { Method = "level-up", Level = 6 }] },
                new PokemonMoveOption { Name = "machine-only", LearnMethods = [new PokemonMoveLearnMethod { Method = "machine", Level = 0 }] }
            ]
        };
        var profile = PokemonProfile.Generate(7);
        profile.Level = 9;
        profile.Enrich(details);
        Assert.Equal(["growl", "tackle", "tail-whip", "thunder-shock"], profile.Moves.Select(m => m.Name).ToArray());
    }

    [Fact]
    public void AdvanceGrowthFollowsLevelCurve()
    {
        var profile = PokemonProfile.Generate(1);
        profile.AdvanceGrowth(375_000_000, Rarity.Common);
        Assert.Equal(52, profile.Level);
        profile.AdvanceGrowth(750_000_000, Rarity.Common);
        Assert.Equal(100, profile.Level);
    }

    [Fact]
    public void AdvanceGrowthNeverLosesLevels()
    {
        var profile = PokemonProfile.Generate(1);
        profile.AdvanceGrowth(750_000_000, Rarity.Common);
        Assert.Equal(100, profile.Level);
        profile.AdvanceGrowth(0, Rarity.Common);
        Assert.Equal(100, profile.Level);
        Assert.Equal(750_000_000, profile.GrowthTokens);
    }

    [Fact]
    public void RebasePreservesGrowthFraction()
    {
        var profile = PokemonProfile.Generate(1);
        profile.ApplyGrowth(375_000_000, Rarity.Common);
        profile.RebaseForSpeciesIdentity(Rarity.Common, Rarity.Legendary);
        Assert.Equal(3_000_000_000, profile.GrowthTokens);
        Assert.Null(profile.Gender);
        Assert.Empty(profile.Moves);
    }

    [Fact]
    public void SanitizeClampsCorruptValues()
    {
        var profile = new PokemonProfile
        {
            InstanceID = "",
            Level = 500,
            GrowthTokens = 9_000_000_000_000_000,
            IVs = new PokemonIVs { Hp = 99, Attack = -5, Defense = 15, SpecialAttack = 15, SpecialDefense = 15, Speed = 15 },
            Moves =
            [
                new PokemonKnownMove(new string('x', 100), 999),
                new PokemonKnownMove("second", 5),
                new PokemonKnownMove("third", 5),
                new PokemonKnownMove("fourth", 5),
                new PokemonKnownMove("fifth", 5)
            ]
        };
        profile.Sanitize();
        Assert.NotEqual("", profile.InstanceID);
        Assert.Equal(100, profile.Level);
        Assert.Equal(SaveTransfer.MaxTokenValue, profile.GrowthTokens);
        Assert.Equal(31, profile.IVs.Hp);
        Assert.Equal(0, profile.IVs.Attack);
        Assert.Equal(4, profile.Moves.Count);
        Assert.Equal(80, profile.Moves[0].Name.Length);
        Assert.Equal(100, profile.Moves[0].LearnedAtLevel);
    }

    [Fact]
    public void StatCalculatorMatchesMainSeriesFormulas()
    {
        var details = new PokemonDetails
        {
            BaseStats = new Dictionary<string, int>
            {
                ["hp"] = 100, ["attack"] = 100, ["defense"] = 100,
                ["special-attack"] = 100, ["special-defense"] = 100, ["speed"] = 100
            }
        };
        var profile = PokemonProfile.Generate(1);
        profile.IVs = new PokemonIVs { Hp = 31, Attack = 31, Defense = 31, SpecialAttack = 31, SpecialDefense = 31, Speed = 31 };
        profile.Level = 50;
        var stats = PokemonStatCalculator.Stats(details, profile, PokemonNature.Adamant);
        var byName = stats.ToDictionary(s => s.Name);
        Assert.Equal((2 * 100 + 31) * 50 / 100 + 50 + 10, byName["hp"].Value);
        var neutralAttack = (2 * 100 + 31) * 50 / 100 + 5;
        Assert.Equal((int)Math.Floor(neutralAttack * 1.1), byName["attack"].Value);
        var neutralSpecialAttack = (2 * 100 + 31) * 50 / 100 + 5;
        Assert.Equal((int)Math.Floor(neutralSpecialAttack * 0.9), byName["special-attack"].Value);
        var neutralNature = PokemonStatCalculator.Stats(details, profile, null);
        Assert.Equal(neutralAttack, neutralNature.ToDictionary(s => s.Name)["attack"].Value);
    }

    [Fact]
    public void DisplayScaleMaximumRoundsUpToHundreds()
    {
        Assert.Equal(300, PokemonStatCalculator.DisplayScaleMaximum([]));
        Assert.Equal(400, PokemonStatCalculator.DisplayScaleMaximum([350]));
        Assert.Equal(400, PokemonStatCalculator.DisplayScaleMaximum([400]));
    }
}
