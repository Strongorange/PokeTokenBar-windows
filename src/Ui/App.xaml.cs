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
    private DashboardWindow? _dashboard;
    private FloatingPetWindow? _pet;
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
        _sprites = new SpriteStore();
        _engine = new CompanionEngine(new CompanionEngineOptions
        {
            GrowthDifficulty = _settings.GrowthDifficulty,
            ShopDifficulty = _settings.ShopDifficulty,
        });
        _engine.Changed += OnCompanionChanged;
        _service = new UsageRefreshService();
        _service.StateChanged += OnStateChanged;
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
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _shutdown.Cancel();
        _trayIcon.Dispose();
        base.OnExit(e);
    }

    private ContextMenu BuildMenu()
    {
        var menu = new ContextMenu();
        var refresh = new MenuItem { Header = "_Refresh" };
        refresh.Click += async (_, _) => await RefreshFromUiAsync();
        var dashboard = new MenuItem { Header = "Open _dashboard" };
        dashboard.Click += (_, _) => ShowDashboard();
        _petToggle = new MenuItem { Header = "Floating _pet", IsCheckable = true };
        _petToggle.Click += (_, _) => SetPetEnabled(_petToggle.IsChecked);
        var diagnostics = new MenuItem { Header = "Open _diagnostics folder" };
        diagnostics.Click += (_, _) => OpenDiagnosticsFolder();
        var exit = new MenuItem { Header = "E_xit" };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(refresh);
        menu.Items.Add(dashboard);
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
            _trayIcon.ToolTipText =
                $"Today: {TokenFormatter.Compact(state.TodayTokens)} · Month: {TokenFormatter.Compact(state.MonthTokens)}";
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

    public bool PetEnabled => _settings.PetEnabled;

    public double PetSize => _settings.PetSize;

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
                _dashboard = new DashboardWindow(_engine);
                _dashboard.Closed += (_, _) => _dashboard = null;
                if (_service.Current is { } state)
                    _dashboard.Update(state);
                else
                    _dashboard.ShowRefreshing();
                _dashboard.UpdateGame(_engine.View());
            }
            _dashboard.Show();
            _dashboard.Activate();
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
