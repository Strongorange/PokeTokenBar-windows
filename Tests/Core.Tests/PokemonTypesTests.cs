using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

/// <summary>
/// 타입 배지 색상(#391) — 18개 타입의 차트 색과 PokeAPI 키 파싱.
/// macOS Badge.swift 포팅.
/// </summary>
public class PokemonTypesTests
{
    [Fact]
    public void ParsesCanonicalApiNames()
    {
        Assert.Equal(PokemonType.Fire, PokemonTypes.FromApiName("fire"));
        Assert.Equal(PokemonType.Grass, PokemonTypes.FromApiName("grass"));
        Assert.Equal(PokemonType.Fairy, PokemonTypes.FromApiName("fairy"));
    }

    /// 대소문자 섞인 입력도 허용 — 스냅샷은 소문자지만 방어적으로.
    [Fact]
    public void ParseIsCaseInsensitive()
    {
        Assert.Equal(PokemonType.Water, PokemonTypes.FromApiName("Water"));
        Assert.Equal(PokemonType.Psychic, PokemonTypes.FromApiName("PSYCHIC"));
    }

    /// 차트 밖(stellar·shadow·빈 값)은 색을 추측하지 않고 null — 중립 캡슐로 빠진다.
    [Fact]
    public void UnknownTypesResolveToNull()
    {
        Assert.Null(PokemonTypes.FromApiName("stellar"));
        Assert.Null(PokemonTypes.FromApiName("shadow"));
        Assert.Null(PokemonTypes.FromApiName(""));
        Assert.Null(PokemonTypes.FromApiName(null));
    }

    [Fact]
    public void ChartColorsMatchTheClassicPalette()
    {
        Assert.Equal(0xFF4422u, PokemonTypes.Hex(PokemonType.Fire));
        Assert.Equal(0x3399FFu, PokemonTypes.Hex(PokemonType.Water));
        Assert.Equal(0x77CC55u, PokemonTypes.Hex(PokemonType.Grass));
        Assert.Equal(0xFFCC33u, PokemonTypes.Hex(PokemonType.Electric));
        Assert.Equal(0xEE99EEu, PokemonTypes.Hex(PokemonType.Fairy));
        Assert.Equal(0x7766EEu, PokemonTypes.Hex(PokemonType.Dragon));
        Assert.Equal(0x775544u, PokemonTypes.Hex(PokemonType.Dark));
    }

    /// 18개 전부 파싱되고 색이 서로 다른 팔레트를 이룬다(노말/스틸 동일 계열 허용).
    [Fact]
    public void AllEighteenTypesRoundTripWithAColor()
    {
        var colors = new HashSet<uint>();
        foreach (PokemonType type in Enum.GetValues(typeof(PokemonType)))
        {
            var key = type.ToString().ToLowerInvariant();
            Assert.Equal(type, PokemonTypes.FromApiName(key));
            var hex = PokemonTypes.Hex(type);
            Assert.NotEqual(0u, hex);
            colors.Add(hex);
        }
        Assert.True(colors.Count >= 16, $"palette too narrow: {colors.Count} colors");
    }
}
