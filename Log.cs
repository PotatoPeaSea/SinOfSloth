namespace LolGuard;

// Append-only log at Home\guard.log. Never throws: logging must not stop the guard.
public static class Log
{
    private const long MaxBytes = 1_000_000;
    private static readonly object Gate = new();

    public static void Write(string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {message}";
        if (Environment.UserInteractive) Console.WriteLine(line);

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Settings.Home);
                var path = Path.Combine(Settings.Home, "guard.log");
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                    File.Move(path, path + ".old", overwrite: true);
                File.AppendAllText(path, line + Environment.NewLine);
            }
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
