using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Ui;

public partial class SettingsWindow : Window
{
    private static readonly string[] ProviderKinds =
        [ScanRootSettings.ClaudeProvider, ScanRootSettings.CodexProvider, ScanRootSettings.OpenCodeProvider];

    private static readonly string[] ProviderNames = ["Claude Code", "Codex", "OpenCode"];

    private readonly CompanionEngine _engine;
    private readonly List<ScanRootEntry> _scanRoots = [];
    private bool _updating;
    private bool _updateChecking;

    public SettingsWindow(CompanionEngine engine)
    {
        InitializeComponent();
        _engine = engine;
        for (var i = 0; i < AppLanguages.All.Length; i++)
        {
            var language = AppLanguages.All[i];
            LanguageCombo.Items.Add(new ComboBoxItem { Content = language.Label(), Tag = language });
        }
        for (var i = 0; i < ProviderKinds.Length; i++)
            ProviderCombo.Items.Add(new ComboBoxItem { Content = ProviderNames[i], Tag = ProviderKinds[i] });
        ProviderCombo.SelectedIndex = 0;
        LoadCurrentValues();
        Relocalize(engine.State.Language);
    }

    private void LoadCurrentValues()
    {
        _updating = true;
        try
        {
            var view = _engine.View();
            GrowthSlider.Value = view.GrowthDifficulty;
            ShopSlider.Value = view.ShopDifficulty;
            GrowthValue.Text = FormatDifficulty(view.GrowthDifficulty);
            ShopValue.Text = FormatDifficulty(view.ShopDifficulty);
            var current = (int)Array.IndexOf(AppLanguages.All, _engine.State.Language);
            LanguageCombo.SelectedIndex = Math.Max(0, current);
            _scanRoots.Clear();
            if (System.Windows.Application.Current is App app)
            {
                PetEnabledCheck.IsChecked = app.PetEnabled;
                PetSizeSlider.Value = app.PetSize;
                UpdateNotificationsCheck.IsChecked = app.UpdateNotificationsEnabled;
                _scanRoots.AddRange(app.ScanRoots);
            }
            else
            {
                PetEnabledCheck.IsChecked = false;
                PetSizeSlider.Value = AppSettingsFile.DefaultPetSize;
                UpdateNotificationsCheck.IsChecked = true;
            }
            PetSizeValue.Text = $"{(int)PetSizeSlider.Value}px";
            RebuildScanList();
        }
        finally
        {
            _updating = false;
        }
    }

    private void Relocalize(AppLanguage lang)
    {
        Title = DashboardText.SettingsTitle(lang);
        GeneralHeader.Text = DashboardText.GeneralSectionTitle(lang);
        LanguageText.Text = DashboardText.LanguageLabel(lang);
        DifficultyHeader.Text = DashboardText.DifficultySection(lang);
        DifficultyHint.Text = DashboardText.DifficultyHint(lang);
        GrowthText.Text = DashboardText.GrowthLabel(lang);
        ShopText.Text = DashboardText.DifficultyShopLabel(lang);
        ScanHeader.Text = DashboardText.ScanFoldersTitle(lang);
        ScanHint.Text = DashboardText.ScanFoldersHint(lang);
        AddButton.Content = "_" + DashboardText.AddFolderButton(lang);
        RemoveButton.Content = "_" + DashboardText.RemoveButton(lang);
        PetHeader.Text = DashboardText.FloatingPet(lang);
        PetSizeText.Text = DashboardText.SizeLabel(lang);
        UpdateHeader.Text = DashboardText.UpdateSectionTitle(lang);
        UpdateNotificationsText.Text = DashboardText.UpdateNotificationsLabel(lang);
        CheckForUpdatesText.Text = DashboardText.CheckForUpdatesLabel(lang);
        CheckNowButton.Content = "_" + DashboardText.CheckNowButton(lang);
        ShowSkippedAgainButton.Content = "_" + DashboardText.ShowSkippedAgain(lang);
        UpdateInstallButton.Content = "_" + DashboardText.UpdateButton(lang);
    }

    private static readonly System.Windows.Media.SolidColorBrush ResultHighlightBrush =
        new(System.Windows.Media.Color.FromRgb(0xB4, 0x5F, 0x06));

    private static readonly System.Windows.Media.SolidColorBrush ResultMutedBrush =
        new(System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66));

    private static string FormatDifficulty(double value) => $"{value:0%}";

    private void OnLanguageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updating) return;
        if (LanguageCombo.SelectedItem is not ComboBoxItem item || item.Tag is not AppLanguage language) return;
        _engine.SetLanguage(language);
        Relocalize(language);
        if (System.Windows.Application.Current is App app)
            app.ApplyLanguage(language);
    }

    private void OnGrowthDifficultyChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _engine is null) return;
        var value = PokemonBalance.SnapDifficulty(e.NewValue);
        _updating = true;
        try
        {
            GrowthSlider.Value = value;
            GrowthValue.Text = FormatDifficulty(value);
            _engine.SetGrowthDifficulty(value);
            if (System.Windows.Application.Current is App app)
                app.ApplyDifficulty(value, ShopSlider.Value);
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnShopDifficultyChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _engine is null) return;
        var value = PokemonBalance.SnapDifficulty(e.NewValue);
        _updating = true;
        try
        {
            ShopSlider.Value = value;
            ShopValue.Text = FormatDifficulty(value);
            _engine.SetShopDifficulty(value);
            if (System.Windows.Application.Current is App app)
                app.ApplyDifficulty(GrowthSlider.Value, value);
        }
        finally
        {
            _updating = false;
        }
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (ProviderCombo.SelectedItem is not ComboBoxItem item || item.Tag is not string provider) return;
        var dialog = new OpenFolderDialog();
        if (dialog.ShowDialog(this) != true) return;
        var path = dialog.FolderName;
        if (_scanRoots.Any(root =>
                root.Provider == provider && string.Equals(root.Path, path, StringComparison.OrdinalIgnoreCase)))
            return;
        _scanRoots.Add(new ScanRootEntry(provider, path));
        ApplyScanRoots();
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e)
    {
        var index = ScanList.SelectedIndex;
        if (index < 0 || index >= _scanRoots.Count) return;
        _scanRoots.RemoveAt(index);
        ApplyScanRoots();
    }

    private void ApplyScanRoots()
    {
        RebuildScanList();
        if (System.Windows.Application.Current is App app)
            app.ApplyScanRoots(_scanRoots);
    }

    private void RebuildScanList()
    {
        ScanList.Items.Clear();
        foreach (var root in _scanRoots)
            ScanList.Items.Add($"{ProviderDisplayName(root.Provider)} — {root.Path}");
        if (ScanList.Items.Count > 0) ScanList.SelectedIndex = ScanList.Items.Count - 1;
    }

    private static string ProviderDisplayName(string provider)
    {
        var index = Array.IndexOf(ProviderKinds, provider);
        return index >= 0 ? ProviderNames[index] : provider;
    }

    private void OnPetEnabledChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _engine is null) return;
        if (System.Windows.Application.Current is App app)
            app.SetPetEnabled(PetEnabledCheck.IsChecked == true);
    }

    private void OnPetSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating || _engine is null) return;
        PetSizeValue.Text = $"{(int)e.NewValue}px";
        if (System.Windows.Application.Current is App app)
            app.ApplyPetSize(e.NewValue);
    }

    private void OnUpdateNotificationsChanged(object sender, RoutedEventArgs e)
    {
        if (_updating || _engine is null) return;
        if (System.Windows.Application.Current is App app)
            app.ApplyUpdateNotifications(UpdateNotificationsCheck.IsChecked == true);
    }

    private async void OnCheckNowClick(object sender, RoutedEventArgs e)
    {
        if (_updateChecking || _engine is null) return;
        if (System.Windows.Application.Current is not App app) return;
        _updateChecking = true;
        var label = CheckNowButton.Content;
        CheckNowButton.IsEnabled = false;
        CheckNowButton.Content = "…";
        try
        {
            await app.ManualUpdateCheckAsync();
        }
        catch
        {
        }
        finally
        {
            _updateChecking = false;
            CheckNowButton.IsEnabled = true;
            CheckNowButton.Content = label;
            RenderUpdateResult();
        }
    }

    private void RenderUpdateResult()
    {
        if (System.Windows.Application.Current is not App app)
        {
            UpdateResultRow.Visibility = Visibility.Collapsed;
            return;
        }
        UpdateResultRow.Visibility = Visibility.Visible;
        var lang = _engine.State.Language;
        var notice = app.UpdateChecker.SettingsNotice;
        switch (notice.Kind)
        {
            case UpdateNoticeKind.Offer:
                UpdateResultText.Text = DashboardText.UpdateFound(lang, notice.Version);
                UpdateResultText.Foreground = ResultHighlightBrush;
                UpdateInstallButton.Visibility = Visibility.Visible;
                ShowSkippedAgainButton.Visibility = Visibility.Collapsed;
                break;
            case UpdateNoticeKind.Skipped:
                UpdateResultText.Text = DashboardText.SkippedVersionText(lang, notice.Version);
                UpdateResultText.Foreground = ResultHighlightBrush;
                UpdateInstallButton.Visibility = Visibility.Visible;
                ShowSkippedAgainButton.Visibility = Visibility.Visible;
                break;
            default:
                UpdateResultText.Text = DashboardText.UpToDate(lang, app.UpdateChecker.CurrentVersion);
                UpdateResultText.Foreground = ResultMutedBrush;
                UpdateInstallButton.Visibility = Visibility.Collapsed;
                ShowSkippedAgainButton.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void OnShowSkippedAgainClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is not App app) return;
        app.UpdateChecker.ShowSkippedAgain();
        RenderUpdateResult();
    }

    private void OnInstallClick(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current is App app)
            app.OpenReleasePage();
    }
}
