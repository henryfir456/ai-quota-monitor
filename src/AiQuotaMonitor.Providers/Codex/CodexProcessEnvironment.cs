namespace AiQuotaMonitor.Providers.Codex;

public static class CodexProcessEnvironment
{
    private static readonly string[] AllowedNames =
    [
        "PATH",
        "SYSTEMROOT",
        "WINDIR",
        "USERPROFILE",
        "HOMEDRIVE",
        "HOMEPATH",
        "LOCALAPPDATA",
        "APPDATA",
        "TEMP",
        "TMP",
        "CODEX_HOME",
        "CODEX_CLI_PATH"
    ];

    public static IReadOnlyDictionary<string, string> Filter(IEnumerable<KeyValuePair<string, string?>> environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        var allowed = new HashSet<string>(AllowedNames, StringComparer.OrdinalIgnoreCase);
        return environment
            .Where(pair => allowed.Contains(pair.Key) && !string.IsNullOrEmpty(pair.Value))
            .GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(pair => pair.Key, pair => pair.Value!, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, string> ReadAllowed(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);
        return AllowedNames
            .Select(name => new KeyValuePair<string, string?>(name, getEnvironmentVariable(name)))
            .Where(pair => !string.IsNullOrEmpty(pair.Value))
            .ToDictionary(pair => pair.Key, pair => pair.Value!, StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyDictionary<string, string> ReadCurrent() => ReadAllowed(Environment.GetEnvironmentVariable);
}
