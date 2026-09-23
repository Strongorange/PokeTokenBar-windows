using System.IO;
using System.Windows;
using Microsoft.Win32;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class DashboardWindow : Window
{
    private readonly CompanionEngine _engine;

    public DashboardWindow(CompanionEngine engine)
    {
        InitializeComponent();
        _engine = engine;
    }

    public void Update(UsageDisplayState state)
    {
        ProvidersList.Items.Clear();
        foreach (var provider in state.Providers)
        {
            var availability = provider.Available ? "" : "  (not found)";
            ProvidersList.Items.Add(
                $"{provider.DisplayName}: today {TokenFormatter.Grouped(provider.TodayTokens)} · " +
                $"month {TokenFormatter.Grouped(provider.MonthTokens)}{availability}");
        }
        CombinedText.Text =
            $"Today {TokenFormatter.Grouped(state.TodayTokens)} · Month {TokenFormatter.Grouped(state.MonthTokens)}";
        RefreshedText.Text = $"Refreshed {state.AsOfUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    }

    public void ShowRefreshing()
    {
        RefreshedText.Text = "Refreshing…";
    }

    public void UpdateGame(CompanionGameView view)
    {
        var shiny = view.HasActive && view.IsShiny ? " ★" : "";
        var boost = view.HasGrowthBoost ? "  (growth boost)" : "";
        var rarity = view.Rarity is { } value ? RarityText(value) : "";
        CompanionName.Text = view.ActiveName + shiny;
        CompanionDetail.Text = view.HasActive
            ? $"{rarity}{boost}"
            : "Keep using your AI tools — the egg is incubating.";

        if (view.HasActive)
        {
            ProgressLabel.Text =
                $"Stage {view.StageIndex + 1}/{view.TotalForms} · {TokenFormatter.Grouped(view.StageUsed)} / " +
                $"{TokenFormatter.Grouped(view.StageThreshold)} tokens";
            ProgressBar.Value = view.StageProgress;
            StageLine.Text = string.Join(" → ", view.StageItems.Select(ItemText));
        }
        else
        {
            ProgressLabel.Text = $"Egg · {TokenFormatter.Grouped(view.EggUsed)} / " +
                $"{TokenFormatter.Grouped(view.EggThreshold)} tokens";
            ProgressBar.Value = view.EggProgress;
            StageLine.Text = "";
        }

        DexHeader.Text = $"Dex: {view.DexCount} species · wallet {TokenFormatter.Grouped(view.AvailableTokens)}";
        DexList.Items.Clear();
        foreach (var row in view.DexRows)
        {
            var star = row.IsShiny ? " ★" : "";
            var raising = row.IsRaising ? "  ← raising" : "";
            DexList.Items.Add($"#{row.SpeciesID} {row.Name}{star} · {RarityText(row.Rarity)}{raising}");
        }
        EventsList.Items.Clear();
        foreach (var item in view.RecentEvents)
            EventsList.Items.Add($"{item.At.ToLocalTime():MM-dd HH:mm}  {item.Text}");
        GameFooter.Text = $"Lifetime {TokenFormatter.Grouped(view.LifetimeTokens)} tokens";
    }

    private static string ItemText(CompanionStageItem item) =>
        item.Mystery ? item.Label : item.Label;

    private static string RarityText(Rarity rarity) => rarity switch
    {
        Rarity.Common => "common",
        Rarity.Uncommon => "uncommon",
        Rarity.Rare => "rare",
        Rarity.Legendary => "legendary",
        _ => ""
    };

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            await app.RefreshFromUiAsync();
    }

    private void OnExportClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = _engine.SuggestedExportFileName(),
            Filter = "PokeTokenBar save (*.json)|*.json",
        };
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            File.WriteAllBytes(dialog.FileName, _engine.ExportSave());
        }
        catch (Exception ex)
        {
            AppLog.Write($"save export failed: {ex.Message}");
            MessageBox.Show(this, $"Export failed: {ex.Message}", "PokeTokenBar",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnImportClick(object sender, RoutedEventArgs e)
    {
        var dialog = OpenFileDialog();
        if (dialog.ShowDialog(this) != true) return;
        try
        {
            var data = File.ReadAllBytes(dialog.FileName);
            var current = _engine.View();
            var confirm = MessageBox.Show(this,
                $"Import save from '{Path.GetFileName(dialog.FileName)}'?\n" +
                $"Current progress (dex {current.DexCount}, lifetime {current.LifetimeTokens}) " +
                "will be backed up and replaced.",
                "PokeTokenBar", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.OK) return;
            _engine.ImportSave(data);
            UpdateGame(_engine.View());
        }
        catch (Exception ex)
        {
            AppLog.Write($"save import failed: {ex.Message}");
            MessageBox.Show(this, $"Import failed: {ex.Message}", "PokeTokenBar",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static OpenFileDialog OpenFileDialog() => new()
    {
        Filter = "PokeTokenBar save (*.json)|*.json|All files (*.*)|*.*",
    };
}
