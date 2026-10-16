using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace SinOfSloth;

// Polls the process list and closes watched processes according to config.json.
public sealed class Guard : BackgroundService
{
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(500);

    private Settings? _lastGood;
    private string? _lastError;
    private DateTime? _armedAtUtc; // when the current delay countdown started

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.Write($"Guard started. Config: {Settings.ConfigPath}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Tick(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Log.Write($"Tick failed: {ex.Message}");
            }

            await Task.Delay(Poll, stoppingToken);
        }

        Log.Write("Guard stopped.");
    }

    private void Tick(DateTime nowUtc)
    {
        var settings = LoadSettings();
        var running = FindRunning(settings.ProcessNames);

        if (running.Count == 0 || settings.IsAllowed(nowUtc))
        {
            // Nothing to enforce. The next countdown starts fresh when something shows up.
            _armedAtUtc = null;
            Dispose(running);
            return;
        }

        if (settings.Mode == GuardMode.Instant)
        {
            Close(running, "instant mode");
            return;
        }

        _armedAtUtc ??= nowUtc;
        var deadline = _armedAtUtc.Value.AddMinutes(Math.Max(0, settings.DelayMinutes));
        if (nowUtc >= deadline)
            Close(running, $"{settings.DelayMinutes} min delay elapsed");
        else
            Dispose(running);
    }

    // If config.json is broken, keep enforcing with the last good copy. With no good copy, use defaults (instant).
    private Settings LoadSettings()
    {
        try
        {
            _lastGood = Settings.Read();
            _lastError = null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            if (ex.Message != _lastError)
            {
                Log.Write($"config.json unreadable, using last good settings: {ex.Message}");
                _lastError = ex.Message;
            }
        }

        return _lastGood ?? new Settings();
    }

    public static List<Process> FindRunning(IEnumerable<string> names)
    {
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            var key = Path.GetFileNameWithoutExtension(name);
            if (!string.IsNullOrEmpty(key)) wanted.Add(key);
        }

        var found = new List<Process>();
        foreach (var process in Process.GetProcesses())
        {
            var matches = false;
            try
            {
                matches = wanted.Contains(process.ProcessName);
            }
            catch (Win32Exception) { }
            catch (InvalidOperationException) { }

            if (matches) found.Add(process);
            else process.Dispose();
        }

        return found;
    }

    private static void Close(List<Process> processes, string reason)
    {
        foreach (var process in processes)
        {
            var id = process.Id;
            try
            {
                var name = process.ProcessName;
                process.Kill(entireProcessTree: true);
                Log.Write($"Closed {name} (pid {id}): {reason}.");
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
            {
                // Already gone, or Windows refused. The next tick will try again if it is still running.
                Log.Write($"Could not close pid {id}: {ex.Message}");
            }
            finally
            {
                process.Dispose();
            }
        }
    }

    private static void Dispose(List<Process> processes)
    {
        foreach (var process in processes) process.Dispose();
    }
}
