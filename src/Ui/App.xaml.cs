using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using PokeTokenBar.Application;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Ui;

public partial class App : System.Windows.Application
{
    private UsageRefreshService _service = null!;
    private CompanionEngine _engine = null!;
    private SpriteStore _sprites = null!;
    private AppSettings _settings = null!;
    private TaskbarIcon _trayIcon = null!;
    private MenuItem _petToggle = null!;
    private UpdateChecker _updateChecker = null!;
    private string _announcedUpdate = "";
    private DashboardWindow? _dashboard;
    private SettingsWindow? _settingsWindow;
    private FloatingPetWindow? _pet;
    private readonly UsageRootOptions _rootOptions = new();
    private readonly CancellationTokenSource _shutdown = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Write($"ui unhandled exception: {args.Exception}");
            args.Handled = true;
        };
        _settings = AppSettingsFile.Load(AppSettingsFile.DefaultPath());
        ScanRootSettings.Apply(_rootOptions, _settings.ScanRoots);
        _sprites = new SpriteStore();
        _engine = new CompanionEngine(new CompanionEngineOptions
        {
            GrowthDifficulty = _settings.GrowthDifficulty,
            ShopDifficulty = _settings.ShopDifficulty,
        });
        _engine.Changed += OnCompanionChanged;
        _service = new UsageRefreshService(new UsageRefreshOptions { RootOptions = _rootOptions });
        _service.StateChanged += OnStateChanged;
        _updateChecker = new UpdateChecker(new UpdateCheckerOptions
        {
            CurrentVersion = (typeof(App).Assembly.GetName().Version ?? new Version()).ToString(3),
            ReadSkippedVersion = () => _settings.SkippedUpdateVersion,
            WriteSkippedVersion = version =>
            {
                _settings.SkippedUpdateVersion = version;
                SaveSettings();
            },
        });
        _updateChecker.Changed += () => Dispatcher.BeginInvoke(() => _dashboard?.RefreshUpdateBanner());
        _ = RefreshSafelyAsync();
        _ = RunLoopAsync();
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "PokeTokenBar",
            IconSource = new BitmapImage(new Uri("pack://application:,,,/Assets/pokeball.ico")),
            ContextMenu = BuildMenu(),
        };
        _trayIcon.TrayLeftMouseUp += (_, _) => ShowDashboard();
        if (_settings.PetEnabled) SetPetEnabled(true);
        _ = AutoCheckUpdateAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _shutdown.Cancel();
        _trayIcon.Dispose();
        base.OnExit(e);
    }

    private ContextMenu BuildMenu()
    {
        var lang = _engine.State.Language;
        var menu = new ContextMenu();
        var header = new MenuItem
        {
            Header = "PokeTokenBar " + (typeof(App).Assembly.GetName().Version ?? new Version()).ToString(3),
            IsEnabled = false,
        };
        var refresh = new MenuItem { Header = "_" + DashboardText.RefreshButton(lang) };
        refresh.Click += async (_, _) => await RefreshFromUiAsync();
        var dashboard = new MenuItem { Header = DashboardText.OpenDashboard(lang) };
        dashboard.Click += (_, _) => ShowDashboard();
        var settingsItem = new MenuItem { Header = DashboardText.SettingsTitle(lang) };
        settingsItem.Click += (_, _) => ShowSettings();
        _petToggle = new MenuItem { Header = DashboardText.FloatingPet(lang), IsCheckable = true };
        _petToggle.Click += (_, _) => SetPetEnabled(_petToggle.IsChecked);
        var diagnostics = new MenuItem { Header = DashboardText.OpenDiagnosticsFolder(lang) };
        diagnostics.Click += (_, _) => OpenDiagnosticsFolder();
        var exit = new MenuItem { Header = "_" + DashboardText.ExitApp(lang) };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(header);
        menu.Items.Add(refresh);
        menu.Items.Add(dashboard);
        menu.Items.Add(settingsItem);
        menu.Items.Add(_petToggle);
        menu.Items.Add(diagnostics);
        menu.Items.Add(new Separator());
        menu.Items.Add(exit);
        return menu;
    }

    public async Task RefreshFromUiAsync()
    {
        await RefreshSafelyAsync();
        if (_service.Current is { } state)
            _dashboard?.Update(state);
    }

    private async Task RefreshSafelyAsync()
    {
        try
        {
            await _service.RefreshAsync(_shutdown.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AppLog.Write($"manual refresh failed: {ex.Message}");
        }
    }

    private async Task RunLoopAsync()
    {
        try
        {
            await _service.RunAsync(_shutdown.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void OnStateChanged(UsageDisplayState state)
    {
        try
        {
            _engine.ApplyUsage(state);
        }
        catch (Exception ex)
        {
            AppLog.Write($"companion update failed: {ex.Message}");
        }
        Dispatcher.BeginInvoke(() =>
        {
            var lang = _engine.State.Language;
            _trayIcon.ToolTipText =
                $"{DashboardText.TodayLabel(lang)}: {TokenFormatter.Compact(state.TodayTokens)} · " +
                $"{DashboardText.MonthLabel(lang)}: {TokenFormatter.Compact(state.MonthTokens)}";
            _dashboard?.Update(state);
        });
    }

    private void OnCompanionChanged()
    {
        Dispatcher.BeginInvoke(() =>
        {
            var view = _engine.View();
            _dashboard?.UpdateGame(view);
            _pet?.Update(view);
            NotifyPendingEvents();
        });
    }

    private void NotifyPendingEvents()
    {
        try
        {
            var notices = _engine.DrainNotices();
            if (notices.Count == 0) return;
            var text = string.Join("\n", notices.Select(notice => notice.Text));
            _trayIcon.ShowBalloonTip("PokeTokenBar", text, BalloonIcon.Info);
        }
        catch (Exception ex)
        {
            AppLog.Write($"balloon notify failed: {ex.Message}");
        }
    }

    /// <summary>Startup/dashboard-open check (30-min debounce inside). Silent on failure.</summary>
    private async Task AutoCheckUpdateAsync()
    {
        try
        {
            var before = _updateChecker.Available?.Version;
            await _updateChecker.CheckAsync();
            if (!_settings.UpdateNotificationsEnabled) return;
            var release = _updateChecker.Available;
            if (release is null || release.Version == before || release.Version == _announcedUpdate) return;
            _announcedUpdate = release.Version;
            var lang = _engine.State.Language;
            _trayIcon.ShowBalloonTip("PokeTokenBar",
                DashboardText.UpdateAvailable(lang, release.Version, _updateChecker.CurrentVersion),
                BalloonIcon.Info);
        }
        catch (Exception ex)
        {
            AppLog.Write($"update check failed: {ex.Message}");
        }
    }

    /// <summary>Manual check from Settings — bypasses the debounce, no balloon.</summary>
    public Task ManualUpdateCheckAsync() => _updateChecker.CheckAsync(0);

    public void ApplyUpdateNotifications(bool enabled)
    {
        try
        {
            _settings.UpdateNotificationsEnabled = enabled;
            SaveSettings();
            _dashboard?.RefreshUpdateBanner();
        }
        catch (Exception ex)
        {
            AppLog.Write($"update notifications apply failed: {ex.Message}");
        }
    }

    public void SkipCurrentUpdate()
    {
        try
        {
            _updateChecker.SkipCurrent();
        }
        catch (Exception ex)
        {
            AppLog.Write($"update skip failed: {ex.Message}");
        }
    }

    public void OpenReleasePage()
    {
        try
        {
            var release = _updateChecker.UpdateTarget;
            if (release is null || !UpdateChecker.IsSafeReleaseUrl(release.Url)) return;
            Process.Start(new ProcessStartInfo { FileName = release.Url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppLog.Write($"release page open failed: {ex.Message}");
        }
    }

    public bool PetEnabled => _settings.PetEnabled;

    public double PetSize => _settings.PetSize;

    public IReadOnlyList<ScanRootEntry> ScanRoots => _settings.ScanRoots;

    public UpdateChecker UpdateChecker => _updateChecker;

    public bool UpdateNotificationsEnabled => _settings.UpdateNotificationsEnabled;

    public void ApplyLanguage(AppLanguage language)
    {
        try
        {
            _trayIcon.ContextMenu = BuildMenu();
            _dashboard?.Relocalize(language);
            if (_service.Current is { } state) _dashboard?.Update(state);
            _pet?.Relocalize(language);
        }
        catch (Exception ex)
        {
            AppLog.Write($"language apply failed: {ex.Message}");
        }
    }

    public void ApplyScanRoots(IReadOnlyList<ScanRootEntry> entries)
    {
        try
        {
            _settings.ScanRoots = entries;
            ScanRootSettings.Apply(_rootOptions, entries);
            SaveSettings();
            _ = RefreshFromUiAsync();
        }
        catch (Exception ex)
        {
            AppLog.Write($"scan roots apply failed: {ex.Message}");
        }
    }

    public void ShowSettings()
    {
        try
        {
            if (_settingsWindow is null)
            {
                _settingsWindow = new SettingsWindow(_engine);
                if (_dashboard is not null) _settingsWindow.Owner = _dashboard;
                else _settingsWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            }
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }
        catch (Exception ex)
        {
            _settingsWindow = null;
            AppLog.Write($"settings window open failed: {ex}");
        }
    }

    public void SetPetEnabled(bool enabled)
    {
        try
        {
            if (enabled)
            {
                _pet ??= CreatePetWindow();
                _pet.Show();
            }
            else
            {
                _pet?.Hide();
            }
            _settings.PetEnabled = enabled;
            if (_petToggle is not null) _petToggle.IsChecked = enabled;
            SaveSettings();
        }
        catch (Exception ex)
        {
            AppLog.Write($"pet toggle failed: {ex}");
        }
    }

    public void ApplyPetSize(double size)
    {
        var snapped = AppSettingsFile.ClampPetSize(Math.Round(size / 8.0) * 8);
        _settings.PetSize = snapped;
        _pet?.ApplySize(snapped);
        SaveSettings();
    }

    private FloatingPetWindow CreatePetWindow()
    {
        var pet = new FloatingPetWindow(_engine, _sprites, _settings.PetSize, OnPetMoved);
        pet.Place(_settings.PetX, _settings.PetY);
        pet.Update(_engine.View());
        return pet;
    }

    private void OnPetMoved(double x, double y)
    {
        _settings.PetX = x;
        _settings.PetY = y;
        SaveSettings();
    }

    private void SaveSettings()
    {
        try
        {
            AppSettingsFile.Save(AppSettingsFile.DefaultPath(), _settings);
        }
        catch (Exception ex)
        {
            AppLog.Write($"app settings save failed: {ex.Message}");
        }
    }

    public void ShowDashboard()
    {
        try
        {
            if (_dashboard is null)
            {
                _dashboard = new DashboardWindow(_engine, _sprites);
                _dashboard.Closed += (_, _) => _dashboard = null;
                if (_service.Current is { } state)
                    _dashboard.Update(state);
                else
                    _dashboard.ShowRefreshing();
                _dashboard.UpdateGame(_engine.View());
            }
            _dashboard.Show();
            _dashboard.Activate();
            _ = AutoCheckUpdateAsync();
        }
        catch (Exception ex)
        {
            _dashboard = null;
            AppLog.Write($"dashboard open failed: {ex}");
        }
    }

    public void ApplyDifficulty(double growth, double shop)
    {
        _settings.GrowthDifficulty = growth;
        _settings.ShopDifficulty = shop;
        SaveSettings();
    }

    private void OpenDiagnosticsFolder()
    {
        var folder = Path.GetDirectoryName(AppLog.LogFilePath);
        if (string.IsNullOrEmpty(folder)) return;
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }
}
