using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class CodexProcessEnvironmentTests
{
    [Fact]
    public void OnlyOsPathAndProfileVariablesArePassedToChildProcess()
    {
        var filtered = CodexProcessEnvironment.Filter(
        [
            new("PATH", "C:/Windows/System32"),
            new("USERPROFILE", "C:/Users/SyntheticUser"),
            new("LOCALAPPDATA", "C:/Users/SyntheticUser/AppData/Local"),
            new("OPENAI_API_KEY", "blocked-value"),
            new("CODEX_HOME", "C:/Synthetic/Credentials"),
            new("HOME", "C:/Synthetic/Credentials"),
            new("ANTHROPIC_API_KEY", "blocked-value"),
            new("UNEXPECTED_TOKEN_VALUE", "blocked-value")
        ]);

        Assert.Equal(3, filtered.Count);
        Assert.Contains("PATH", filtered.Keys);
        Assert.Contains("USERPROFILE", filtered.Keys);
        Assert.Contains("LOCALAPPDATA", filtered.Keys);
        Assert.DoesNotContain("OPENAI_API_KEY", filtered.Keys);
        Assert.DoesNotContain("CODEX_HOME", filtered.Keys);
        Assert.DoesNotContain("HOME", filtered.Keys);
        Assert.DoesNotContain(filtered.Values, value => value.Contains("blocked-value", StringComparison.Ordinal));
    }

    [Fact]
    public void AllowlistReaderQueriesOnlyExplicitNames()
    {
        var readNames = new List<string>();

        var environment = CodexProcessEnvironment.ReadAllowed(name =>
        {
            readNames.Add(name);
            return name == "PATH" ? "C:/Windows/System32" : null;
        });

        string[] expectedNames = ["PATH", "SYSTEMROOT", "WINDIR", "USERPROFILE", "HOMEDRIVE", "HOMEPATH", "LOCALAPPDATA", "APPDATA", "TEMP", "TMP"];
        Assert.Equal(expectedNames, readNames);
        Assert.Equal("C:/Windows/System32", environment["PATH"]);
        Assert.Single(environment);
    }
}
