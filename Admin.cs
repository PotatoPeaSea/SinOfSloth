using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using System.ServiceProcess;

namespace SinOfSloth;

// Administrator commands. Every command needs an elevated (Run as administrator) prompt.
public static class Admin
{
    private static readonly TimeSpan ServiceTimeout = TimeSpan.FromSeconds(30);

    public static int Run(string[] args)
    {
        // Checked before anything else, so a standard user cannot change config or the service.
        if (!IsAdministrator())
            return Fail("SinOfSloth management needs an Administrator prompt (right-click the terminal > Run as administrator).");

        try
        {
            return args[0].ToLowerInvariant() switch
            {
                "install" => Install(),
                "uninstall" => Uninstall(),
                "status" => Status(),
                "instant" => Change(s => s.Mode = GuardMode.Instant, "Mode: instant. LoL is closed as soon as it opens."),
                "delay" => Delay(args),
                "allow" => Allow(args),
                "disable" => Disable(),
                "enable" => Enable(),
                "help" or "-h" or "--help" => Usage(0),
                _ => Usage(1),
            };
        }
        catch (Exception ex)
        {
            return Fail($"Error: {ex.Message}");
        }
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static int Install()
    {
        var source = Environment.ProcessPath!;
        var target = Path.Combine(Settings.InstallDir, "SinOfSloth.exe");

        // Remove any previous version first, so its exe can be replaced.
        RemoveService();

        Directory.CreateDirectory(Settings.InstallDir);
        if (!string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            File.Copy(source, target, overwrite: true);

        Directory.CreateDirectory(Settings.Home);
        LockDownFolder(Settings.Home);
        if (!File.Exists(Settings.ConfigPath)) new Settings().Save();
        SetDisabled(false); // installing turns the service on, even if it was disabled before

        ToolRun("sc.exe", "create", Settings.ServiceName, "binPath=", $"\"{target}\"", "start=", "auto", "DisplayName=", Settings.DisplayName);
        ToolRun("sc.exe", "description", Settings.ServiceName, "Closes League of Legends when it opens. Managed with SinOfSloth.exe (Administrators only).");
        ToolRun("sc.exe", "failure", Settings.ServiceName, "reset=", "86400", "actions=", "restart/5000/restart/5000/restart/5000");
        ToolRun("sc.exe", "failureflag", Settings.ServiceName, "1"); // restart on any failure, not only crashes
        // Only SYSTEM and Administrators may start, stop, query or configure the service.
        ToolRun("sc.exe", "sdset", Settings.ServiceName, "D:(A;;CCLCSWRPWPDTLOCRRC;;;SY)(A;;CCDCLCSWRPWPDTLOCRSDRCWDWO;;;BA)");

        using var service = new ServiceController(Settings.ServiceName);
        service.Start();
        service.WaitForStatus(ServiceControllerStatus.Running, ServiceTimeout);

        Console.WriteLine($"Installed and running: {target}");
        Console.WriteLine($"Config: {Settings.ConfigPath}");
        Console.WriteLine("It starts automatically at boot. Check it with: SinOfSloth.exe status");
        return 0;
    }

    private static int Uninstall()
    {
        if (!ServiceExists())
        {
            Console.WriteLine("Not installed.");
            return 0;
        }

        RemoveService();
        Console.WriteLine("Service removed. To finish cleanup, delete these folders as Administrator:");
        Console.WriteLine($"  {Settings.InstallDir}");
        Console.WriteLine($"  {Settings.Home}");
        return 0;
    }

    private static int Status()
    {
        var settings = Settings.Read();
        var now = DateTime.UtcNow;
        var mode = settings.Mode == GuardMode.Instant ? "instant" : $"delay {settings.DelayMinutes} min";
        var allowed = settings.IsAllowed(now)
            ? $"until {settings.AllowUntilUtc!.Value.ToLocalTime():HH:mm}"
            : "no";

        var running = Guard.FindRunning(settings.ProcessNames);
        var runningText = running.Count == 0
            ? "none"
            : string.Join(", ", running.Select(p => $"{p.ProcessName} ({p.Id})"));
        running.ForEach(p => p.Dispose());

        var enabled = settings.Disabled ? "no (use enable to turn it back on)" : "yes";

        Console.WriteLine($"Service:   {ServiceState()}");
        Console.WriteLine($"Enabled:   {enabled}");
        Console.WriteLine($"Mode:      {mode}");
        Console.WriteLine($"Allowed:   {allowed}");
        Console.WriteLine($"Watching:  {string.Join(", ", settings.ProcessNames)}");
        Console.WriteLine($"Running:   {runningText}");
        Console.WriteLine($"Config:    {Settings.ConfigPath}");
        return 0;
    }

    private static int Change(Action<Settings> edit, string message)
    {
        var settings = Settings.Read();
        edit(settings);
        settings.Save();
        Console.WriteLine(message);
        return 0;
    }

    private static int Delay(string[] args)
    {
        if (!TryMinutes(args, out var minutes))
            return Fail("Usage: SinOfSloth.exe delay <minutes>   (example: delay 10)");

        return Change(s =>
        {
            s.Mode = GuardMode.Delay;
            s.DelayMinutes = minutes;
        }, $"Mode: delay. LoL is closed {minutes} min after it opens.");
    }

    private static int Allow(string[] args)
    {
        if (args.Length == 2 && args[1].Equals("off", StringComparison.OrdinalIgnoreCase))
            return Change(s => s.AllowUntilUtc = null, "Allowance cancelled. LoL is enforced again.");

        if (!TryMinutes(args, out var minutes))
            return Fail("Usage: SinOfSloth.exe allow <minutes>   or   SinOfSloth.exe allow off");

        var until = DateTime.UtcNow.AddMinutes(minutes);
        return Change(s => s.AllowUntilUtc = until, $"LoL is allowed until {until.ToLocalTime():HH:mm}.");
    }

    private static bool TryMinutes(string[] args, out double minutes)
    {
        minutes = 0;
        return args.Length == 2
            && double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out minutes)
            && minutes > 0;
    }

    private static bool ServiceExists()
    {
        try
        {
            using var service = new ServiceController(Settings.ServiceName);
            _ = service.Status; // throws if the service is not registered
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string ServiceState()
    {
        if (!ServiceExists()) return "not installed";
        using var service = new ServiceController(Settings.ServiceName);
        return service.Status.ToString();
    }

    private static int Disable()
    {
        if (!ServiceExists()) return Fail("Not installed. Run install first.");

        SetDisabled(true);
        WaitUntilStopped();
        Console.WriteLine("Disabled. SinOfSloth is stopped and will not restart. Turn it back on with: SinOfSloth.exe enable");
        return 0;
    }

    private static int Enable()
    {
        if (!ServiceExists()) return Fail("Not installed. Run install first.");

        SetDisabled(false);
        using var service = new ServiceController(Settings.ServiceName);
        if (service.Status == ServiceControllerStatus.Stopped) service.Start();
        service.WaitForStatus(ServiceControllerStatus.Running, ServiceTimeout);
        Console.WriteLine("Enabled. SinOfSloth is running and restarts if its process is killed.");
        return 0;
    }

    // The service refuses SCM stop requests, so it is stopped through config.json instead. It reads the flag,
    // exits cleanly, and a clean exit is not restarted.
    private static void StopService()
    {
        using (var service = new ServiceController(Settings.ServiceName))
        {
            if (service.Status == ServiceControllerStatus.Stopped) return;
        }

        SetDisabled(true);
        WaitUntilStopped();
    }

    private static void WaitUntilStopped()
    {
        using var service = new ServiceController(Settings.ServiceName);
        service.WaitForStatus(ServiceControllerStatus.Stopped, ServiceTimeout);
    }

    private static void SetDisabled(bool disabled)
    {
        var settings = Settings.Read();
        settings.Disabled = disabled;
        settings.Save();
    }

    private static void RemoveService()
    {
        if (!ServiceExists()) return;

        StopService();
        ToolRun("sc.exe", "delete", Settings.ServiceName);

        // The SCM keeps the entry until open handles close. Wait so a re-create does not fail.
        var deadline = DateTime.UtcNow + ServiceTimeout;
        while (ServiceExists() && DateTime.UtcNow < deadline) Thread.Sleep(250);
    }

    // SYSTEM and Administrators only. SIDs are used so this works on any Windows language.
    private static void LockDownFolder(string folder) =>
        ToolRun("icacls.exe", folder, "/inheritance:r", "/grant:r", "*S-1-5-18:(OI)(CI)F", "*S-1-5-32-544:(OI)(CI)F", "/T");

    private static void ToolRun(string exe, params string[] args)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
        foreach (var arg in args) psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Could not start {exe}.");
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{exe} {args[0]} failed with exit code {process.ExitCode}.");
    }

    private static int Usage(int exitCode)
    {
        Console.WriteLine("""
            SinOfSloth  (Administrator only)

              install            install and start the SinOfSloth service
              uninstall          stop and remove the service
              status             show service state, settings and running LoL processes
              instant            close LoL as soon as it opens
              delay <minutes>    close LoL this many minutes after it opens
              allow <minutes>    let LoL run for this long, then enforce again
              allow off          cancel an allowance
              disable            stop the service and keep it stopped until enable
              enable             turn the service back on and start it
            """);
        return exitCode;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 1;
    }
}
