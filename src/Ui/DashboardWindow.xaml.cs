using System.IO;
using System.Windows;
using Microsoft.Win32;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class DashboardWindow : Window
{
    private readonly CompanionEngine _engine;
    private CompanionGameView? _lastView;
    private bool _updatingDifficulty;
    private string _feedback = "";

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
        UpdateShop(view);
        GameFooter.Text = _feedback.Length > 0
            ? $"{_feedback} · Lifetime {TokenFormatter.Grouped(view.LifetimeTokens)} tokens"
            : $"Lifetime {TokenFormatter.Grouped(view.LifetimeTokens)} tokens";
    }

    private void UpdateShop(CompanionGameView view)
    {
        _lastView = view;
        BagText.Text = view.Bag.Count == 0
            ? "Bag: empty"
            : "Bag: " + string.Join(" · ", view.Bag.Select(item =>
                $"{item.Label} ×{item.Count}"));
        ShopList.Items.Clear();
        foreach (var row in view.ShopRows)
        {
            var suffix = row.CanBuy ? ""
                : row.Item is { } owned && owned.IsPassive() && BagHas(owned, view)
                    ? "  (owned)"
                    : "  (need more tokens)";
            ShopList.Items.Add($"{row.Label} — {TokenFormatter.Grouped(row.Price)}{suffix}");
        }

        _updatingDifficulty = true;
        try
        {
            GrowthSlider.Value = view.GrowthDifficulty;
            ShopSlider.Value = view.ShopDifficulty;
            if (System.Windows.Application.Current is App app)
            {
                PetEnabledCheck.IsChecked = app.PetEnabled;
                PetSizeSlider.Value = app.PetSize;
                PetSizeValue.Text = $"{(int)app.PetSize}px";
            }
        }
        finally
        {
            _updatingDifficulty = false;
        }

        GrowthValue.Text = view.GrowthDifficulty.ToString("0.00");
        ShopValue.Text = view.ShopDifficulty.ToString("0.00");
    }

    private static bool BagHas(ItemKind kind, CompanionGameView view) =>
        view.Bag.Any(item => item.Kind == kind);

    private void OnBuyClick(object sender, RoutedEventArgs e)
    {
        if (_lastView is not { } view) return;
        var index = ShopList.SelectedIndex;
        if (index < 0 || index >= view.ShopRows.Count)
        {
            _feedback = "Select a shop row first";
            UpdateGame(_engine.View());
            return;
        }
        var row = view.ShopRows[index];
        var bought = row.Item is { } kind ? _engine.Buy(kind) : _engine.BuyEgg(row.EggTier);
        _feedback = bought ? $"Bought {row.Label}" : $"Cannot buy {row.Label} yet";
        UpdateGame(_engine.View());
    }

    private void OnUseCandyClick(object sender, RoutedEventArgs e)
    {
        var result = _engine.UseRareCandy(1);
        _feedback = result switch
        {
            CandyUseResult.Graduated => "Candy used — graduated into the dex!",
            CandyUseResult.Evolved => "Candy used — evolved!",
            CandyUseResult.Progressed => "Candy used — +100M XP",
            _ => "No candy to use",
        };
        UpdateGame(_engine.View());
    }

    private void OnUseAllCandyClick(object sender, RoutedEventArgs e)
    {
        var count = _engine.MaxRareCandyUseCount();
        if (count <= 0)
        {
            _feedback = "No candy to use";
        }
        else
        {
            _engine.UseRareCandy(count);
            _feedback = $"Used {count} candies";
        }
        UpdateGame(_engine.View());
    }

    private void OnUseMintClick(object sender, RoutedEventArgs e)
    {
        var nature = _engine.UseMint();
        _feedback = nature is { } picked ? $"Mint used — nature is now {picked}" : "No mint to use";
        UpdateGame(_engine.View());
    }

    private void OnGrowthDifficultyChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingDifficulty || _engine is null) return;
        var value = PokemonBalance.SnapDifficulty(e.NewValue);
        _updatingDifficulty = true;
        try
        {
            GrowthSlider.Value = value;
            GrowthValue.Text = value.ToString("0.00");
            _engine.SetGrowthDifficulty(value);
            PersistDifficulty();
            UpdateGame(_engine.View());
        }
        finally
        {
            _updatingDifficulty = false;
        }
    }

    private void OnShopDifficultyChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingDifficulty || _engine is null) return;
        var value = PokemonBalance.SnapDifficulty(e.NewValue);
        _updatingDifficulty = true;
        try
        {
            ShopSlider.Value = value;
            ShopValue.Text = value.ToString("0.00");
            _engine.SetShopDifficulty(value);
            PersistDifficulty();
            UpdateGame(_engine.View());
        }
        finally
        {
            _updatingDifficulty = false;
        }
    }

    private void OnPetEnabledChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingDifficulty || _engine is null) return;
        if (System.Windows.Application.Current is App app)
            app.SetPetEnabled(PetEnabledCheck.IsChecked == true);
    }

    private void OnPetSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingDifficulty || _engine is null) return;
        PetSizeValue.Text = $"{(int)e.NewValue}px";
        if (System.Windows.Application.Current is App app)
            app.ApplyPetSize(e.NewValue);
    }

    private void PersistDifficulty()
    {
        if (System.Windows.Application.Current is App app)
            app.ApplyDifficulty(GrowthSlider.Value, ShopSlider.Value);
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
