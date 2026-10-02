using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class DashboardWindow : Window
{
    private readonly CompanionEngine _engine;
    private readonly SpriteStore _sprites;
    private readonly CompanionCelebrationPlayer _celebrations;
    private readonly ShopFlow _flow;
    private readonly UsageHomeRenderer _usageHome;
    private readonly ShopCards _shopCards;
    private readonly CompanionHeader _companionHeader;
    private readonly DexTab _dexTab;
    private CompanionGameView? _lastView;

    public DashboardWindow(CompanionEngine engine, SpriteStore sprites)
    {
        InitializeComponent();
        _engine = engine;
        _sprites = sprites;
        CelebrationShiny.Children.Add(CelebrationGlyphs.Sparkle(24));
        CelebrationDitto.Children.Add(CelebrationGlyphs.TheaterMasks());
        CelebrationGlyphs.FillMintCluster(CelebrationMint);
        _celebrations = new CompanionCelebrationPlayer(
            CompanionTile, CompanionScale, CompanionRotation, CelebrationFlash,
            CelebrationShiny, CelebrationShinyScale,
            CelebrationDitto, CelebrationDittoScale,
            CelebrationCandy, CelebrationCandyText, CelebrationCandySlide,
            CelebrationMint, CelebrationMintScale);
        _flow = new ShopFlow(engine);
        _usageHome = new UsageHomeRenderer(
            this, TodayCaptionLabel, TodayBigNumber, TodayGroupedLabel, TodayCostLabel,
            WeekMonthRow, ProviderChips, ProviderDetail, ProvidersUnavailableText,
            ProvidersSection, TrendSection, TrendCaptionLabel, TrendPeakLabel,
            TrendPeakValue, TrendReadout, TrendModelsHost, TrendBarsHost, TrendTicksHost,
            TrendAxisHost, YearSection, YearHeatHost);
        _shopCards = new ShopCards(this, sprites, engine, _flow,
            () => UpdateGame(_engine.View()), BagCards, ShopCardsHost, SpendableAmount);
        _companionHeader = new CompanionHeader(
            this, sprites,
            CompanionName, RarityCapsule, RarityCapsuleText,
            CompanionDetail, StatusCapsule, StatusCapsuleText,
            CombatText, ProgressLabel, ProgressBar, CompanionStatusText,
            EvolutionScroll, EvolutionLine, CompanionSprite, CompanionSpritePlaceholder);
        _dexTab = new DexTab(
            this, sprites, engine, OpenSpeciesDetail, () => UpdateGame(_engine.View()),
            DexModeToggle, DexHeader, DexRarityFilter, DexList,
            CatchLogPanel, CatchHeader, CatchRarityFilter, CatchList,
            DexEmpty, DexEmptyTitle, DexEmptyHint, DexEmptySprite, DexEmptyPlaceholder,
            DexFooter, DexFooterStar, DexFooterInfo);
        LocalizeStaticText(engine.State.Language);
        HorizontalScrollSupport.Attach(YearHeatScroll);
        YearHeatScroll.SizeChanged += (_, _) => _usageHome.RerenderYear();
    }

    public void Relocalize(AppLanguage lang)
    {
        LocalizeStaticText(lang);
    }

    public void SelectDexTab()
    {
        MainTabs.SelectedIndex = 2;
    }

    private void LocalizeStaticText(AppLanguage lang)
    {
        var usageTab = (TabItem) MainTabs.Items[0];
        usageTab.Header = "_" + DashboardText.UsageTab(lang);
        var gameTab = (TabItem) MainTabs.Items[1];
        gameTab.Header = "_" + DashboardText.GameTab(lang);
        var dexTab = (TabItem) MainTabs.Items[2];
        dexTab.Header = "_" + DashboardText.DexTitle(lang);
        var shopTab = (TabItem) MainTabs.Items[3];
        shopTab.Header = "_" + DashboardText.ShopTitle(lang);
        DexModeDexRadio.Content = DashboardText.DexTitle(lang);
        DexModeLogRadio.Content = DashboardText.CatchLogTitle(lang);
        EventsHeader.Text = DashboardText.CompanionEventsLabel(lang);
        ShopHeader.Text = DashboardText.BagTitle(lang);
        SpendableLabel.Text = DashboardText.SpendableTokens(lang);
        ShopHintText.Text = DashboardText.ShopHint(lang);
        ProvidersHeader.Text = DashboardText.ProvidersTitle(lang);
        YearHeader.Text = DashboardText.LastYearTitle(lang);
        ExportButton.Content = DashboardText.ExportSave(lang);
        ImportButton.Content = DashboardText.ImportSave(lang);
        RefreshButton.Content = "_" + DashboardText.RefreshButton(lang);
        SettingsButton.Content = "_" + DashboardText.SettingsTitle(lang) + "…";
        UpdateSkipButton.Content = "_" + DashboardText.SkipThisVersion(lang);
        UpdateInstallButton.Content = "_" + DashboardText.UpdateButton(lang);
        RefreshUpdateBanner();
    }

    public void RefreshUpdateBanner()
    {
        App? app = System.Windows.Application.Current as App;
        var release = app?.UpdateChecker.Available;
        if (release is null || app is null || !app.UpdateNotificationsEnabled)
        {
            UpdateBanner.Visibility = Visibility.Collapsed;
            return;
        }
        UpdateBannerText.Text = DashboardText.UpdateAvailable(
            _engine.State.Language, release.Version, app.UpdateChecker.CurrentVersion);
        UpdateBanner.Visibility = Visibility.Visible;
    }

    private void OnUpdateSkipClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.SkipCurrentUpdate();
    }

    private void OnUpdateInstallClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.OpenReleasePage();
    }

    public void Update(UsageDisplayState state)
    {
        var lang = _engine.State.Language;
        _usageHome.Render(state, lang);
        RefreshedText.Text = DashboardText.RefreshedAt(lang,
            state.AsOfUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"));
    }

    public void ShowRefreshing()
    {
        RefreshedText.Text = DashboardText.Refreshing(_engine.State.Language);
    }

    public void UpdateGame(CompanionGameView view)
    {
        _celebrations.Play(_engine.DrainCelebrations());
        _celebrations.SetEggImminent(!view.HasActive && GameTabPresentation.EggImminent(view));
        _companionHeader.Render(view,
            view.HasActive ? _engine.Detail(view.ActiveSpeciesID) : null);
        _dexTab.Render(view);
        EventsList.Items.Clear();
        foreach (var item in view.RecentEvents)
            EventsList.Items.Add($"{item.At.ToLocalTime():MM-dd HH:mm}  {item.Text}");
        UpdateShop(view);
        var feedback = _flow.Feedback is { } feedbackOf ? feedbackOf(view.Language) : "";
        var lifetime = $"{DashboardText.LifetimeLabel(view.Language)} " +
                       $"{TokenFormatter.Grouped(view.LifetimeTokens)} {DashboardText.TokensUnit(view.Language)}";
        GameFooter.Text = feedback.Length > 0 ? $"{feedback} · {lifetime}" : lifetime;
    }

    private void UpdateShop(CompanionGameView view)
    {
        _lastView = view;
        _shopCards.RenderBag(view);
        _shopCards.RenderShop(view);
    }

    private void OnDexModeChanged(object sender, RoutedEventArgs e)
    {
        _dexTab.ShowLog(ReferenceEquals(sender, DexModeLogRadio));
        if (_lastView is { } view)
            UpdateGame(view);
    }

    private void OnDexDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_lastView is not { } view) return;
        var row = _dexTab.RowAt(DexList.SelectedIndex);
        if (row is null) return;
        OpenSpeciesDetail(row.SpeciesID);
    }

    private void OpenSpeciesDetail(int speciesID)
    {
        try
        {
            if (_engine.Detail(speciesID) is { } detail)
            {
                var window = new SpeciesDetailWindow(detail, _sprites, _engine) { Owner = this };
                window.Show();
            }
            else
            {
                _flow.SetFeedback(
                    language => DashboardText.NoCombatDetails(language, speciesID));
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

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.ShowSettings();
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
            _flow.Reset();
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
