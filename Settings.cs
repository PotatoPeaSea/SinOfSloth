using System.Text.Json;
using System.Text.Json.Serialization;

namespace LolGuard;

public enum GuardMode
{
    Instant, // close as soon as a watched process appears
    Delay,   // close DelayMinutes after a watched process first appears
}

// config.json in Home. Only SYSTEM (the service) and Administrators can read or write it.
public sealed class Settings
{
    public const string ServiceName = "LolGuard";
    public const string DisplayName = "LoL Guard";

    public GuardMode Mode { get; set; } = GuardMode.Instant;
    public double DelayMinutes { get; set; } = 5;
    public DateTime? AllowUntilUtc { get; set; }
    public List<string> ProcessNames { get; set; } = DefaultProcessNames();

    // %ProgramData%\LolGuard by default. LOLGUARD_HOME overrides it, which is handy for testing.
    public static string Home { get; } =
        Environment.GetEnvironmentVariable("LOLGUARD_HOME") is { Length: > 0 } custom
            ? custom
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "LolGuard");

    public static string ConfigPath => Path.Combine(Home, "config.json");

    public static string InstallDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LolGuard");

    public bool IsAllowed(DateTime nowUtc) => AllowUntilUtc is { } until && until > nowUtc;

    // Throws if the file exists but is broken, so admin commands never overwrite it silently.
    public static Settings Read()
    {
        if (!File.Exists(ConfigPath)) return new Settings();
        var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllText(ConfigPath), JsonOptions) ?? new Settings();
        settings.ProcessNames ??= DefaultProcessNames();
        return settings;
    }

    public void Save()
    {
        Directory.CreateDirectory(Home);
        var temp = ConfigPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, JsonOptions));
        File.Move(temp, ConfigPath, overwrite: true);
    }

    private static List<string> DefaultProcessNames() =>
    [
        "League of Legends.exe",   // the game
        "LeagueClient.exe",        // the client / lobby
        "LeagueClientUx.exe",      // client UI
        "LeagueClientUxRender.exe",
        "vgc.exe",                 // Riot Vanguard service
        "vgtray.exe",              // Riot Vanguard tray
        "vgm.exe",                 // Riot Vanguard (in its install folder)
    ];

    // camelCase keys on disk. Matching is case-insensitive so hand-edited files still load.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}
