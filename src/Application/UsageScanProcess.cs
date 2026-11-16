using System.Diagnostics;
using System.Text;
using System.Text.Json;
using PokeTokenBar.Core;
using PokeTokenBar.Platform.Windows;

namespace PokeTokenBar.Application;

/// <summary>
/// Runs the usage scan in a short-lived copy of the app, so the memory a scan touches is returned to Windows
/// when that process exits instead of staying with the long-running UI process.
/// </summary>
public static class UsageScanProcess
{
    public const string Argument = "--scan";
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    private static readonly JsonSerializerOptions Json = new();

    /// <summary>Entry point of the scan process: scans with the user's settings and writes the state to stdout.</summary>
    public static int RunChild()
    {
        try
        {
            var settings = AppSettingsFile.Load(AppSettingsFile.DefaultPath());
            var roots = new UsageRootOptions();
            ScanRootSettings.Apply(roots, settings.ScanRoots);
            new ModelRateFeed().LoadCached();
            var state = new UsageRefreshService(new UsageRefreshOptions { RootOptions = roots }).RefreshAll();
            using var stdout = Console.OpenStandardOutput();
            Write(stdout, state);
            return 0;
        }
        catch (Exception ex)
        {
            AppLog.Write($"scan process failed: {ex.GetType().Name}: {ex.Message}");
            return 1;
        }
    }

    /// <summary>Starts the scan process and returns the state it printed.</summary>
    public static UsageDisplayState Run(string executablePath, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(executablePath, Argument)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                StandardOutputEncoding = Encoding.UTF8,
            },
        };
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        try
        {
            process.WaitForExitAsync(deadline.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"scan process did not finish within {timeout}");
        }
        var json = output.GetAwaiter().GetResult();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"scan process exited with code {process.ExitCode}");
        return Read(json);
    }

    public static void Write(Stream stream, UsageDisplayState state) => JsonSerializer.Serialize(stream, state, Json);

    public static UsageDisplayState Read(string json) =>
        JsonSerializer.Deserialize<UsageDisplayState>(json, Json)
        ?? throw new InvalidDataException("scan process printed no state");
}
