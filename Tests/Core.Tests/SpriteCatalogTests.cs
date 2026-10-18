using PokeTokenBar.Core;

namespace PokeTokenBar.Core.Tests;

public class SpriteCatalogTests
{
    [Theory]
    [InlineData(25, false, false, null, "25-s")]
    [InlineData(25, true, false, null, "25-a")]
    [InlineData(25, false, true, null, "25-shs")]
    [InlineData(25, true, true, null, "25-sha")]
    [InlineData(201, false, false, "a", "201-s")]
    [InlineData(201, true, true, "b", "201-b-sha")]
    [InlineData(201, true, false, "exclamation", "201-exclamation-a")]
    [InlineData(201, false, true, "question", "201-question-shs")]
    [InlineData(132, true, false, "b", "132-a")]
    [InlineData(412, true, false, "sandy", "412-sandy-a")]
    public void CacheKeyMatchesMacOSScheme(int speciesID, bool animated, bool shiny,
        string? rawForm, string expected)
    {
        Assert.Equal(expected, SpriteCatalog.CacheKey(speciesID, animated, shiny, Form(rawForm)));
    }

    [Theory]
    [InlineData(25, false, false, null,
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/25.png")]
    [InlineData(25, true, false, null,
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/generation-v/black-white/animated/25.gif")]
    [InlineData(25, true, true, null,
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/generation-v/black-white/animated/shiny/25.gif")]
    [InlineData(25, false, true, null,
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/shiny/25.png")]
    [InlineData(201, true, false, "question",
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/versions/generation-v/black-white/animated/201-question.gif")]
    [InlineData(201, false, false, "a",
        "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/201.png")]
    public void SpriteUrlMatchesMacOSScheme(int speciesID, bool animated, bool shiny,
        string? rawForm, string expected)
    {
        Assert.Equal(expected, SpriteCatalog.SpriteUrl(speciesID, animated, shiny, Form(rawForm)));
    }

    [Fact]
    public void CacheFileNamesCarryTheRightExtension()
    {
        Assert.Equal("25-a.gif", SpriteCatalog.CacheFileName(25, true, false));
        Assert.Equal("25-shs.png", SpriteCatalog.CacheFileName(25, false, true));
        Assert.Equal("201-b-a.gif", SpriteCatalog.CacheFileName(201, true, false, Form("b")));
    }

    [Fact]
    public void EggUsesFlatKeyAndRootUrl()
    {
        Assert.Equal("egg", SpriteCatalog.EggCacheKey);
        Assert.Equal("https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/egg.png",
            SpriteCatalog.EggUrl);
    }

    [Fact]
    public void ItemSpritesUseItemsBaseWithMacOSCacheKey()
    {
        Assert.Equal("item-rare-candy", SpriteCatalog.ItemCacheKey("rare-candy"));
        Assert.Equal("https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items/rare-candy.png",
            SpriteCatalog.ItemUrl("rare-candy"));
        Assert.Equal("https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/items/shiny-charm.png",
            SpriteCatalog.ItemUrl("shiny-charm"));
    }

    [Fact]
    public void UnownAssetNamesFollowPokeApiFormNaming()
    {
        Assert.Equal("201", SpriteCatalog.AssetName(201, Form("a")));
        Assert.Equal("201-b", SpriteCatalog.AssetName(201, Form("b")));
        Assert.Equal("201-exclamation", SpriteCatalog.AssetName(201, Form("exclamation")));
        Assert.Equal("201-question", SpriteCatalog.AssetName(201, Form("question")));
        Assert.Equal("201", SpriteCatalog.AssetName(201, null));
        Assert.Equal("25", SpriteCatalog.AssetName(25, Form("b")));
    }

    /// <summary>
    /// Port of the macOS form-sprite contract: variety forms use the variety PNG and the form GIF,
    /// and cache files keep the `<id>-<form>` name either way.
    /// </summary>
    [Fact]
    public void VarietyFormsUseTheVarietyPNGAndTheFormGif()
    {
        var sky = Form("sky");
        Assert.Equal("492-sky-a.gif", SpriteCatalog.CacheFileName(492, true, false, sky));
        Assert.Equal("10006", SpriteCatalog.StaticAssetName(492, sky));
        Assert.Equal("https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/10006.png",
            SpriteCatalog.SpriteUrl(492, false, false, sky));
        var sandy = Form("sandy");
        Assert.Equal("412-sandy", SpriteCatalog.StaticAssetName(412, sandy));
        Assert.Equal("https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/412-sandy.png",
            SpriteCatalog.SpriteUrl(412, false, false, sandy));
        Assert.Equal("492", SpriteCatalog.StaticAssetName(492, null));
    }

    private static PokemonForm? Form(string? raw) =>
        raw is null ? null : new PokemonForm(raw);
}

public class FloatingPetGeometryTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(3, 2, true)]
    [InlineData(-3, -2, true)]
    [InlineData(4, 0, false)]
    [InlineData(0, 4, false)]
    [InlineData(10, 0, false)]
    public void ClickVsDragThresholdIsExclusiveBelowSquaredDistance(double dx, double dy, bool expected)
    {
        Assert.Equal(expected, FloatingPetGeometry.IsClick(dx, dy));
    }

    [Theory]
    [InlineData(96, false, 96, 96)]
    [InlineData(96, true, 180, 168)]
    [InlineData(200, true, 200, 272)]
    [InlineData(200, false, 200, 200)]
    public void PanelSizeGrowsOnlyForBubble(double petSize, bool bubble,
        double expectedWidth, double expectedHeight)
    {
        var (width, height) = FloatingPetGeometry.PanelSize(petSize, bubble);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }

    [Theory]
    [InlineData(96, 96, 0)]
    [InlineData(96, 180, 42)]
    [InlineData(96, 240, 72)]
    [InlineData(200, 180, 0)]
    public void PanelXInsetCentersPetInsideWiderBubblePanel(double petSize, double panelWidth,
        double expected)
    {
        Assert.Equal(expected, FloatingPetGeometry.PanelXInset(petSize, panelWidth));
    }

    [Fact]
    public void BubbleConstantsMatchMacOSPanel()
    {
        Assert.Equal(16, FloatingPetGeometry.ClickThresholdSquared);
        Assert.Equal(72, FloatingPetGeometry.BubbleHeadroom);
        Assert.Equal(180, FloatingPetGeometry.BubbleMinWidth);
        Assert.Equal(8, FloatingPetGeometry.BubbleHorizontalPadding);
        Assert.Equal(164, FloatingPetGeometry.BubbleContentWidth);
        Assert.Equal(2, FloatingPetGeometry.BubbleBodyLineLimit);
    }
}
