using PokeTokenBar.Core;

namespace PokeTokenBar.Application;

/// <summary>What the floating pet displays right now, as one shared value.</summary>
public readonly record struct FloatingSubject(bool Egg, int SpeciesID, bool Shiny, PokemonForm? Form);

/// <summary>
/// Locates the floating pet's subject in the dex grid. The pet shows the
/// representative when one is pinned, otherwise the active companion — an egg
/// floats only when no representative is pinned and has no dex row to reveal.
/// The pet window and the dex grid both ask here, so what floats and what the
/// grid highlights can never drift apart.
/// </summary>
public static class DexFocus
{
    /// <summary>The pet's current subject (species, shiny, form, egg state).</summary>
    public static FloatingSubject Resolve(CompanionGameView view) =>
        view.RepresentativeSpeciesID is { } representative
            ? new FloatingSubject(false, representative, view.RepresentativeIsShiny,
                view.RepresentativeForm)
            : new FloatingSubject(view.IsEgg, view.ActiveSpeciesID, view.IsShiny, view.ActiveForm);

    /// <summary>
    /// The species the dex grid should highlight for the floating subject, or
    /// null when the egg floats (eggs have no dex row).
    /// </summary>
    public static int? DexSpeciesID(CompanionGameView view)
    {
        var subject = Resolve(view);
        return subject.Egg ? null : subject.SpeciesID;
    }

    /// <summary>
    /// Position of the species within the visible (already filtered and
    /// sorted) rows — null when the current filters hide the subject, so the
    /// reveal becomes a no-op instead of scrolling somewhere wrong.
    /// </summary>
    public static int? VisibleRowIndex(IReadOnlyList<CompanionDexRow> visibleRows, int? speciesID)
    {
        if (speciesID is not { } id) return null;
        for (var index = 0; index < visibleRows.Count; index++)
            if (visibleRows[index].SpeciesID == id)
                return index;
        return null;
    }
}
