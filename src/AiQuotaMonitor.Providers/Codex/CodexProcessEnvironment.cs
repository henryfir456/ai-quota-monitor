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
        "TMP"
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
}
