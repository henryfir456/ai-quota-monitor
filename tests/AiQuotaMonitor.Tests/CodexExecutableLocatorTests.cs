using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class CodexExecutableLocatorTests
{
    [Fact]
    public void FindsOnlyKnownExecutableNamesWithoutOpeningCredentialFiles()
    {
        var expected = Path.Combine("C:", "Users", "SyntheticUser", "Programs", "OpenAI", "Codex", "bin", "codex.exe");
        var calls = new List<string>();

        var result = CodexExecutableLocator.Find(
            pathVariable: null,
            localApplicationData: Path.Combine("C:", "Users", "SyntheticUser"),
            userProfile: Path.Combine("C:", "Users", "SyntheticUser"),
            fileExists: path =>
            {
                calls.Add(path);
                return string.Equals(path, expected, StringComparison.OrdinalIgnoreCase);
            });

        Assert.Equal(expected, result);
        Assert.All(calls, path => Assert.True(Path.GetFileName(path).Equals("codex.exe", StringComparison.OrdinalIgnoreCase)));
        Assert.DoesNotContain(calls, path => path.Contains("auth.json", StringComparison.OrdinalIgnoreCase));
    }
}
