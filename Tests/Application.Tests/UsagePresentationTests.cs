using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Application.Tests;

public class UsagePresentationTests
{
    private static ProviderUsageSummary Provider(string id, string name, bool available,
        long today = 0, long week = 0, long month = 0) =>
        new(id, name, available, today, 0, month, 0, WeekTokens: week);

    [Fact]
    public void SelectableProvidersKeepAvailableWithAnyUsage()
    {
        var providers = new List<ProviderUsageSummary>
        {
            Provider("claude", "Claude", true, today: 500),
            Provider("codex", "Codex", true, week: 300),
            Provider("idle", "Idle", true),
            Provider("gone", "Gone", false, today: 900),
        };

        var selectable = UsagePresentation.SelectableProviders(providers);

        Assert.Equal(["claude", "codex"], selectable.Select(provider => provider.ProviderId));
    }

    [Fact]
    public void UnavailableNoteJoinsDisplayNamesAndIsEmptyWithoutUnavailable()
    {
        var providers = new List<ProviderUsageSummary>
        {
            Provider("claude", "Claude", true, today: 500),
            Provider("gone", "Gone", false),
            Provider("away", "Away", false),
        };

        var note = UsagePresentation.UnavailableNote(providers, AppLanguage.En);
        var suffix = DashboardText.ProviderNotFound(AppLanguage.En);

        Assert.Equal($"Gone {suffix} · Away {suffix}", note);
        Assert.Equal("", UsagePresentation.UnavailableNote(
            [Provider("claude", "Claude", true, today: 1)], AppLanguage.En));
    }

    [Fact]
    public void OrderedModelsSortByTokensDescendingAndTolerateNull()
    {
        Dictionary<string, long>? models = new()
        {
            ["provider/gpt-4o"] = 100,
            ["provider/claude-3"] = 900,
            ["gemini"] = 300,
        };

        var ordered = UsagePresentation.OrderedModels(models);

        Assert.Equal(["claude-3", "gemini", "gpt-4o"],
            ordered.Select(pair => UsagePresentation.ModelShortName(pair.Key)));
        Assert.Empty(UsagePresentation.OrderedModels(null));
    }

    [Fact]
    public void ModelShortNameCutsProviderPrefixAndKeepsBareNames()
    {
        Assert.Equal("gpt-4o", UsagePresentation.ModelShortName("openai/gpt-4o"));
        Assert.Equal("claude-3-5", UsagePresentation.ModelShortName("anthropic/claude-3-5"));
        Assert.Equal("gemini", UsagePresentation.ModelShortName("gemini"));
    }
}
