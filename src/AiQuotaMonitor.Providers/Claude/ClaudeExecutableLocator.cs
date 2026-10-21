namespace AiQuotaMonitor.Providers.Claude;

public static class ClaudeExecutableLocator
{
    private static readonly string[] ExecutableNames = ["claude.cmd", "claude.exe", "claude.bat"];

    public static string? Find()
    {
        return Find(
            Environment.GetEnvironmentVariable("PATH"),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            File.Exists,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    public static string? Find(
        string? pathVariable,
        string? localApplicationData,
        string? appData,
        Func<string, bool> fileExists,
        string? userProfile = null)
    {
        ArgumentNullException.ThrowIfNull(fileExists);
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(appData))
        {
            candidates.Add(Path.Combine(appData, "npm", "claude.cmd"));
        }

        if (!string.IsNullOrWhiteSpace(localApplicationData))
        {
            candidates.Add(Path.Combine(localApplicationData, "Programs", "Claude", "claude.exe"));
        }

        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            candidates.Add(Path.Combine(userProfile, ".local", "bin", "claude.exe"));
        }

        if (!string.IsNullOrWhiteSpace(pathVariable))
        {
            foreach (var directory in pathVariable.Split(Path.PathSeparator,
                         StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                foreach (var name in ExecutableNames)
                {
                    candidates.Add(Path.Combine(directory.Trim('"'), name));
                }
            }
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(fileExists);
    }
}
