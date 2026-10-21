namespace AiQuotaMonitor.Providers.Codex;

public static class CodexExecutableLocator
{
    public static string? Find()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var codexHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        return Find(
            path,
            localApplicationData,
            userProfile,
            File.Exists,
            Environment.GetEnvironmentVariable("CODEX_CLI_PATH"),
            appData,
            EnumerateWindowsDesktopPackages(programFiles),
            codexHome);
    }

    public static string? Find(
        string? pathVariable,
        string? localApplicationData,
        string? userProfile,
        Func<string, bool> fileExists,
        string? codexCliPath = null,
        string? appData = null,
        IEnumerable<string>? windowsDesktopPackageDirectories = null,
        string? codexHome = null)
    {
        ArgumentNullException.ThrowIfNull(fileExists);
        var candidates = new List<string>();

        var explicitCliPath = codexCliPath?.Trim('"');
        if (!string.IsNullOrWhiteSpace(explicitCliPath) &&
            string.Equals(Path.GetFileName(explicitCliPath), "codex.exe", StringComparison.OrdinalIgnoreCase))
        {
            candidates.Add(explicitCliPath);
        }

        if (!string.IsNullOrWhiteSpace(localApplicationData))
        {
            candidates.Add(Path.Combine(localApplicationData, "Programs", "OpenAI", "Codex", "bin", "codex.exe"));
            candidates.Add(Path.Combine(localApplicationData, "OpenAI", "Codex", "bin", "codex.exe"));
            candidates.Add(Path.Combine(localApplicationData, "Programs", "OpenAI", "Codex", "resources", "codex.exe"));
            candidates.Add(Path.Combine(localApplicationData, "Microsoft", "WindowsApps", "codex.exe"));
        }

        if (!string.IsNullOrWhiteSpace(appData))
        {
            var npmRoot = Path.Combine(appData, "npm", "node_modules");
            var nativeRelativePath = Path.Combine("vendor", "x86_64-pc-windows-msvc", "bin", "codex.exe");
            candidates.Add(Path.Combine(npmRoot, "@openai", "codex", "node_modules", "@openai", "codex-win32-x64", nativeRelativePath));
            candidates.Add(Path.Combine(npmRoot, "@openai", "codex-win32-x64", nativeRelativePath));
        }

        var resolvedCodexHome = string.IsNullOrWhiteSpace(codexHome)
            ? string.IsNullOrWhiteSpace(userProfile) ? null : Path.Combine(userProfile, ".codex")
            : codexHome.Trim().Trim('"');
        if (!string.IsNullOrWhiteSpace(resolvedCodexHome))
        {
            candidates.Add(Path.Combine(resolvedCodexHome, "bin", "codex.exe"));
            candidates.Add(Path.Combine(resolvedCodexHome, "packages", "standalone", "current", "bin", "codex.exe"));
            candidates.Add(Path.Combine(resolvedCodexHome, "packages", "standalone", "current", "codex.exe"));
            candidates.Add(Path.Combine(resolvedCodexHome, "packages", "app-server-daemon", "current", "bin", "codex.exe"));
        }

        if (!string.IsNullOrWhiteSpace(pathVariable))
        {
            candidates.AddRange(pathVariable
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(directory => Path.Combine(directory.Trim('"'), "codex.exe")));
        }

        foreach (var packageDirectory in windowsDesktopPackageDirectories ?? [])
        {
            candidates.Add(Path.Combine(packageDirectory, "app", "resources", "codex.exe"));
        }

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(fileExists);
    }

    private static IEnumerable<string> EnumerateWindowsDesktopPackages(string programFiles)
    {
        if (string.IsNullOrWhiteSpace(programFiles))
        {
            return [];
        }

        var windowsApps = Path.Combine(programFiles, "WindowsApps");
        try
        {
            return Directory.EnumerateDirectories(windowsApps, "OpenAI.Codex_*_x64__*")
                .OrderByDescending(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }
}
