namespace PokeTokenBar.Core;

public class EvoNode
{
    public int SpeciesID { get; set; }
    public List<EvoNode> Children { get; set; } = [];

    public EvoNode() { }

    public EvoNode(int speciesID, List<EvoNode>? children = null)
    {
        SpeciesID = speciesID;
        Children = children ?? [];
    }

    public int Depth => 1 + (Children.Count == 0 ? 0 : Children.Max(c => c.Depth));

    public EvoNode? NodeWithID(int id)
    {
        if (SpeciesID == id) return this;
        foreach (var child in Children)
        {
            var found = child.NodeWithID(id);
            if (found is not null) return found;
        }
        return null;
    }

    public List<int> FinalIDs => Children.Count == 0
        ? [SpeciesID]
        : Children.SelectMany(c => c.FinalIDs).ToList();

    public EvoNode? KeepingAnimatedSprites()
    {
        if (!PokemonAssets.HasAnimatedSprite(SpeciesID)) return null;
        return new EvoNode(SpeciesID, Children
            .Select(c => c.KeepingAnimatedSprites())
            .Where(c => c is not null)
            .Select(c => c!)
            .ToList());
    }
}

public class EvoLine
{
    public int BaseID { get; }
    public EvoNode Tree { get; }
    public Rarity Rarity { get; }
    public IReadOnlyDictionary<int, Dictionary<string, string>> Names { get; }

    public int TotalForms => Tree.Depth;

    public EvoLine(int baseID, EvoNode tree, Rarity rarity, IReadOnlyDictionary<int, Dictionary<string, string>> names)
    {
        BaseID = baseID;
        Tree = tree.KeepingAnimatedSprites() ?? new EvoNode(baseID);
        Rarity = rarity;
        Names = names;
    }

    public string LocalizedName(int id, AppLanguage lang) =>
        Names.TryGetValue(id, out var byLang) && lang.ResolveName(byLang) is { } name
            ? name
            : $"#{id}";
}

public enum EvoLineItemContentKind
{
    Species,
    Mystery
}

public readonly record struct EvoLineItemContent(EvoLineItemContentKind Kind, int SpeciesID)
{
    public static EvoLineItemContent Species(int id) => new(EvoLineItemContentKind.Species, id);
    public static EvoLineItemContent Mystery() => new(EvoLineItemContentKind.Mystery, 0);
}

public enum EvoLineItemState
{
    Done,
    Current,
    Future
}

public readonly record struct EvoLineItem(EvoLineItemContent Content, EvoLineItemState State);
