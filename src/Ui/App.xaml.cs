using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using PokeTokenBar.Application;
using PokeTokenBar.Core;

namespace PokeTokenBar.Ui;

public partial class App : System.Windows.Application
{
    private UsageRefreshService _service = null!;
    private CompanionEngine _engine = null!;
    private TaskbarIcon _trayIcon = null!;
    private DashboardWindow? _dashboard;
    private readonly CancellationTokenSource _shutdown = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Write($"ui unhandled exception: {args.Exception}");
            args.Handled = true;
        };
        _engine = new CompanionEngine();
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
        var diagnostics = new MenuItem { Header = "Open _diagnostics folder" };
        diagnostics.Click += (_, _) => OpenDiagnosticsFolder();
        var exit = new MenuItem { Header = "E_xit" };
        exit.Click += (_, _) => Shutdown();
        menu.Items.Add(refresh);
        menu.Items.Add(dashboard);
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
        Dispatcher.BeginInvoke(() => _dashboard?.UpdateGame(_engine.View()));
    }

    private void ShowDashboard()
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

    private void OpenDiagnosticsFolder()
    {
        var folder = Path.GetDirectoryName(AppLog.LogFilePath);
        if (string.IsNullOrEmpty(folder)) return;
        Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
    }
}
