using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class DashboardWindow : Window
{
    private readonly CompanionEngine _engine;
    private readonly SpriteStore _sprites;
    private readonly SpriteSlot _companionSprite;
    private CompanionGameView? _lastView;
    private bool _updatingDifficulty;
    private Func<AppLanguage, string>? _feedback;

    public DashboardWindow(CompanionEngine engine, SpriteStore sprites)
    {
        InitializeComponent();
        _engine = engine;
        _sprites = sprites;
        _companionSprite = new SpriteSlot(CompanionSprite, CompanionSpritePlaceholder);
        LocalizeStaticText(engine.State.Language);
    }

    private void LocalizeStaticText(AppLanguage lang)
    {
        var usageTab = (TabItem) MainTabs.Items[0];
        usageTab.Header = "_" + DashboardText.UsageTab(lang);
        var gameTab = (TabItem) MainTabs.Items[1];
        gameTab.Header = "_" + DashboardText.GameTab(lang);
        ProvidersHeader.Text = DashboardText.ProvidersTitle(lang);
        CombinedHeader.Text = DashboardText.CombinedTitle(lang);
        ExportButton.Content = DashboardText.ExportSave(lang);
        ImportButton.Content = DashboardText.ImportSave(lang);
        BuyButton.Content = DashboardText.BuySelected(lang);
        UseCandyButton.Content = DashboardText.UseItem(lang, DashboardText.ItemName(lang, ItemKind.RareCandy));
        UseMintButton.Content = DashboardText.UseItem(lang, DashboardText.ItemName(lang, ItemKind.Mint));
        UseAllButton.Content = DashboardText.UseAll(lang);
        ShopHeader.Text = DashboardText.ShopTitle(lang);
        GrowthLabel.Text = DashboardText.GrowthLabel(lang);
        ShopDifficultyLabel.Text = DashboardText.ShopTitle(lang);
        PetHeader.Text = DashboardText.FloatingPet(lang);
        PetSizeLabel.Text = DashboardText.SizeLabel(lang);
        RefreshButton.Content = "_" + DashboardText.RefreshButton(lang);
    }

    public void Update(UsageDisplayState state)
    {
        var lang = _engine.State.Language;
        ProvidersList.Items.Clear();
        foreach (var provider in state.Providers)
        {
            var availability = provider.Available ? "" : "  " + DashboardText.ProviderNotFound(lang);
            ProvidersList.Items.Add(
                $"{provider.DisplayName}: {DashboardText.TodayLabel(lang)} {TokenFormatter.Grouped(provider.TodayTokens)} · " +
                $"{DashboardText.MonthLabel(lang)} {TokenFormatter.Grouped(provider.MonthTokens)}{availability}");
        }
        CombinedText.Text =
            $"{DashboardText.TodayLabel(lang)} {TokenFormatter.Grouped(state.TodayTokens)} · " +
            $"{DashboardText.MonthLabel(lang)} {TokenFormatter.Grouped(state.MonthTokens)}";
        RefreshedText.Text = DashboardText.RefreshedAt(lang,
            state.AsOfUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public void ShowRefreshing()
    {
        RefreshedText.Text = DashboardText.Refreshing(_engine.State.Language);
    }

    public void UpdateGame(CompanionGameView view)
    {
        var lang = view.Language;
        var shiny = view.HasActive && view.IsShiny ? " ★" : "";
        var boost = view.HasGrowthBoost
            ? "  (" + DashboardText.GrowthBoostMark(lang, PokemonBalance.RepeatGrowthMultiplier) + ")" : "";
        var rarity = view.Rarity is { } value ? DashboardText.RarityLabel(lang, value) : "";
        CompanionName.Text = (view.HasActive ? view.ActiveName : DashboardText.TokenEgg(lang)) + shiny;
        CompanionDetail.Text = view.HasActive
            ? $"{rarity}{boost}"
            : DashboardText.EggHint(lang);

        if (view.HasActive)
        {
            ProgressLabel.Text =
                $"{DashboardText.StageLabel(lang, view.StageIndex + 1, view.TotalForms)} · " +
                $"{TokenFormatter.Grouped(view.StageUsed)} / {TokenFormatter.Grouped(view.StageThreshold)} " +
                DashboardText.TokensUnit(lang);
            ProgressBar.Value = view.StageProgress;
            StageLine.Text = string.Join(" → ", view.StageItems.Select(ItemText));
            _companionSprite.Update(_sprites, view.ActiveSpeciesID, true, view.IsShiny,
                view.ActiveUnownForm, "❔");
        }
        else
        {
            ProgressLabel.Text =
                $"{DashboardText.TokenEgg(lang)} · {TokenFormatter.Grouped(view.EggUsed)} / " +
                $"{TokenFormatter.Grouped(view.EggThreshold)} {DashboardText.TokensUnit(lang)}";
            ProgressBar.Value = view.EggProgress;
            StageLine.Text = "";
            _companionSprite.UpdateEgg(_sprites, "🥚");
        }
        UpdateCombatText(view);

        DexHeader.Text = $"{DashboardText.DexTitle(lang)}: {DashboardText.DexSpeciesCount(lang, view.DexCount)} · " +
                         $"{DashboardText.WalletLabel(lang)} {TokenFormatter.Grouped(view.AvailableTokens)} · " +
                         DashboardText.DetailHint(lang);
        DexList.Items.Clear();
        foreach (var row in view.DexRows)
            DexList.Items.Add(CreateDexTile(row, lang));
        EventsList.Items.Clear();
        foreach (var item in view.RecentEvents)
            EventsList.Items.Add($"{item.At.ToLocalTime():MM-dd HH:mm}  {item.Text}");
        UpdateShop(view);
        var feedback = _feedback is { } ? _feedback(lang) : "";
        GameFooter.Text = feedback.Length > 0
            ? $"{feedback} · {DashboardText.LifetimeLabel(lang)} {TokenFormatter.Grouped(view.LifetimeTokens)} {DashboardText.TokensUnit(lang)}"
            : $"{DashboardText.LifetimeLabel(lang)} {TokenFormatter.Grouped(view.LifetimeTokens)} {DashboardText.TokensUnit(lang)}";
    }

    private void UpdateShop(CompanionGameView view)
    {
        _lastView = view;
        var lang = view.Language;
        BagText.Text = view.Bag.Count == 0
            ? $"{DashboardText.BagLabel(lang)}: {DashboardText.BagEmpty(lang)}"
            : $"{DashboardText.BagLabel(lang)}: " + string.Join(" · ", view.Bag.Select(item =>
                $"{item.Label} ×{item.Count}"));
        ShopList.Items.Clear();
        foreach (var row in view.ShopRows)
        {
            var suffix = row.CanBuy ? ""
                : row.Item is { } owned && owned.IsPassive() && BagHas(owned, view)
                    ? "  " + DashboardText.OwnedSuffix(lang)
                    : "  " + DashboardText.NeedMoreTokens(lang);
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
        var lang = _engine.State.Language;
        if (_lastView is not { } view) return;
        var index = ShopList.SelectedIndex;
        if (index < 0 || index >= view.ShopRows.Count)
        {
            _feedback = _ => DashboardText.SelectShopRowFirst(lang);
            UpdateGame(_engine.View());
            return;
        }
        var row = view.ShopRows[index];
        var bought = row.Item is { } kind ? _engine.Buy(kind) : _engine.BuyEgg(row.EggTier);
        _feedback = language => bought
            ? DashboardText.BoughtItem(language, row.Label)
            : DashboardText.CannotBuyItem(language, row.Label);
        UpdateGame(_engine.View());
    }

    private void OnUseCandyClick(object sender, RoutedEventArgs e)
    {
        var result = _engine.UseRareCandy(1);
        _feedback = result switch
        {
            CandyUseResult.Graduated => DashboardText.CandyGraduated,
            CandyUseResult.Evolved => DashboardText.CandyEvolved,
            CandyUseResult.Progressed => DashboardText.CandyProgressed,
            _ => DashboardText.NoCandy,
        };
        UpdateGame(_engine.View());
    }

    private void OnUseAllCandyClick(object sender, RoutedEventArgs e)
    {
        var count = _engine.MaxRareCandyUseCount();
        if (count <= 0)
        {
            _feedback = DashboardText.NoCandy;
        }
        else
        {
            _engine.UseRareCandy(count);
            _feedback = language => DashboardText.UsedCandies(language, count);
        }
        UpdateGame(_engine.View());
    }

    private void OnUseMintClick(object sender, RoutedEventArgs e)
    {
        var nature = _engine.UseMint();
        _feedback = nature is { } picked
            ? language => DashboardText.MintUsed(language, picked.ToString())
            : DashboardText.NoMint;
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

    private void UpdateCombatText(CompanionGameView view)
    {
        var lang = view.Language;
        if (!view.HasActive || _engine.Detail(view.ActiveSpeciesID) is not { } detail)
        {
            CombatText.Text = "";
            return;
        }
        var raising = detail.Individuals.FirstOrDefault(individual => individual.IsRaising);
        var parts = new List<string>();
        if (raising is not null)
        {
            parts.Add($"Lv. {raising.Level}");
            if (raising.Gender.Length > 0) parts.Add(raising.Gender);
            if (raising.Nature.Length > 0) parts.Add(raising.Nature);
            if (raising.Ability.Length > 0)
                parts.Add(raising.Ability + (raising.AbilityIsHidden
                    ? $" ({DashboardText.HiddenMark(lang)})" : ""));
        }
        if (detail.Types.Count > 0) parts.Add(string.Join("/", detail.Types));
        parts.Add($"{DashboardText.BaseTotalLabel(lang)} {detail.BaseStatTotal}");
        CombatText.Text = string.Join(" · ", parts);
    }

    private UIElement CreateDexTile(CompanionDexRow row, AppLanguage lang)
    {
        var star = row.IsShiny ? " ★" : "";
        var raising = row.IsRaising ? $"  ← {DashboardText.RaisingLabel(lang)}" : "";
        var tile = new Grid
        {
            Width = 84,
            Margin = new Thickness(1),
            ToolTip = $"#{row.SpeciesID} {row.Name}{star} · {DashboardText.RarityLabel(lang, row.Rarity)}{raising}",
        };
        for (var i = 0; i < 3; i++) tile.RowDefinitions.Add(new RowDefinition());

        var caption = new Grid();
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        caption.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        caption.Children.Add(new TextBlock
        {
            Text = "#" + row.SpeciesID,
            FontSize = 10,
            Foreground = Brushes.Gray,
        });
        if (row.IsShiny)
        {
            var shiny = new TextBlock
            {
                Text = "★",
                FontSize = 10,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            Grid.SetColumn(shiny, 1);
            caption.Children.Add(shiny);
        }
        Grid.SetRow(caption, 0);
        tile.Children.Add(caption);

        var sprite = new Grid { Height = 64 };
        var image = new Image { Width = 64, Height = 64, Stretch = Stretch.Uniform };
        RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
        var placeholder = new TextBlock
        {
            Text = "❔",
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        sprite.Children.Add(image);
        sprite.Children.Add(placeholder);
        Grid.SetRow(sprite, 1);
        tile.Children.Add(sprite);
        new SpriteSlot(image, placeholder).Update(_sprites, row.SpeciesID, false, row.IsShiny, null, "❔");

        var name = new TextBlock
        {
            Text = (row.IsRaising ? "← " : "") + row.Name,
            FontSize = 11,
            TextAlignment = TextAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        Grid.SetRow(name, 2);
        tile.Children.Add(name);
        return tile;
    }

    private void OnDexDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastView is not { } view) return;
        var index = DexList.SelectedIndex;
        if (index < 0 || index >= view.DexRows.Count) return;
        var row = view.DexRows[index];
        try
        {
            if (_engine.Detail(row.SpeciesID) is { } detail)
            {
                var window = new SpeciesDetailWindow(detail, _sprites) { Owner = this };
                window.Show();
            }
            else
            {
                var speciesID = row.SpeciesID;
                _feedback = language => DashboardText.NoCombatDetails(language, speciesID);
                UpdateGame(_engine.View());
            }
        }
        catch (Exception ex)
        {
            AppLog.Write($"species detail window failed: {ex.Message}");
        }
    }

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
