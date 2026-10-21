namespace AiQuotaMonitor.Providers.Codex;

public static class CodexExecutableLocator
{
    public static string? Find()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Find(path, localApplicationData, userProfile, File.Exists);
    }

    public static string? Find(
        string? pathVariable,
        string? localApplicationData,
        string? userProfile,
        Func<string, bool> fileExists)
    {
        ArgumentNullException.ThrowIfNull(fileExists);
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(localApplicationData))
        {
            candidates.Add(Path.Combine(localApplicationData, "Programs", "OpenAI", "Codex", "bin", "codex.exe"));
            candidates.Add(Path.Combine(localApplicationData, "OpenAI", "Codex", "bin", "codex.exe"));
        }

        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            candidates.Add(Path.Combine(userProfile, ".codex", "bin", "codex.exe"));
        }

        if (!string.IsNullOrWhiteSpace(pathVariable))
        {
            candidates.AddRange(pathVariable
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(directory => Path.Combine(directory.Trim('"'), "codex.exe")));
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(fileExists);
    }
}
