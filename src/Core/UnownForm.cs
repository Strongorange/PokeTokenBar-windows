using System.Text.Json;

namespace PokeTokenBar.Core;

public enum UnownForm
{
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Exclamation,
    Question
}

public static class UnownForms
{
    public const int SpeciesID = 201;

    public static readonly UnownForm[] All =
    [
        UnownForm.A, UnownForm.B, UnownForm.C, UnownForm.D, UnownForm.E, UnownForm.F, UnownForm.G,
        UnownForm.H, UnownForm.I, UnownForm.J, UnownForm.K, UnownForm.L, UnownForm.M,
        UnownForm.N, UnownForm.O, UnownForm.P, UnownForm.Q, UnownForm.R, UnownForm.S, UnownForm.T,
        UnownForm.U, UnownForm.V, UnownForm.W, UnownForm.X, UnownForm.Y, UnownForm.Z,
        UnownForm.Exclamation, UnownForm.Question
    ];

    public static string Raw(UnownForm form) => form switch
    {
        UnownForm.Exclamation => "exclamation",
        UnownForm.Question => "question",
        _ => form.ToString().ToLowerInvariant()
    };

    public static UnownForm? Parse(string? raw)
    {
        if (raw is null) return null;
        foreach (var form in All)
            if (Raw(form) == raw) return form;
        return null;
    }

    public static string Symbol(UnownForm form) => form switch
    {
        UnownForm.Exclamation => "!",
        UnownForm.Question => "?",
        _ => Raw(form).ToUpperInvariant()
    };

    public static int SortOrder(UnownForm form) => Array.IndexOf(All, form);

    public static UnownForm Roll(ulong roll, IReadOnlySet<UnownForm> collected)
    {
        var weights = All.Select(f => CollectionWeight.Adjusted(2, collected.Contains(f))).ToArray();
        var total = weights.Sum();
        var remaining = (int)(roll % (ulong)total);
        for (var i = 0; i < All.Length - 1; i++)
        {
            remaining -= weights[i];
            if (remaining < 0) return All[i];
        }
        return All[^1];
    }

    public static UnownForm? Resolved(int speciesID, UnownForm? form) =>
        speciesID == SpeciesID ? form ?? UnownForm.A : null;

    public static string DisplayName(string name, int speciesID, UnownForm? form)
    {
        var resolved = Resolved(speciesID, form);
        return resolved is null ? name : $"{name} [{Symbol(resolved.Value)}]";
    }
}

public static class CollectionWeight
{
    public static int Adjusted(int weight, bool isCollected) =>
        isCollected ? Math.Max(1, weight / 2) : Math.Max(1, weight);
}
