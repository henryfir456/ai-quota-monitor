using AiQuotaMonitor.Providers.Claude;

namespace AiQuotaMonitor.Tests;

public sealed class ClaudeExecutableLocatorTests
{
    [Fact]
    public void FindsNpmInstalledClaudeCommandWithoutLaunchingIt()
    {
        var appData = Path.Combine("C:", "Users", "SyntheticUser", "AppData", "Roaming");
        var expected = Path.Combine(appData, "npm", "claude.cmd");
        var calls = new List<string>();

        var result = ClaudeExecutableLocator.Find(
            pathVariable: null,
            localApplicationData: "C:/Users/SyntheticUser/AppData/Local",
            appData: appData,
            fileExists: path =>
            {
                calls.Add(path);
                return string.Equals(path, expected, StringComparison.OrdinalIgnoreCase);
            });

        Assert.Equal(expected, result);
        Assert.Single(calls);
    }

    [Fact]
    public void MissingClaudeCommandIsNotReportedAsInstalled()
    {
        var result = ClaudeExecutableLocator.Find(
            pathVariable: "C:/NoClaudeHere",
            localApplicationData: "C:/Users/SyntheticUser/AppData/Local",
            appData: "C:/Users/SyntheticUser/AppData/Roaming",
            fileExists: _ => false);

        Assert.Null(result);
    }

    [Fact]
    public void FindsOfficialNativeWindowsInstallLocationWithoutLaunchingIt()
    {
        var userProfile = Path.Combine("C:", "Users", "SyntheticUser");
        var expected = Path.Combine(userProfile, ".local", "bin", "claude.exe");

        var result = ClaudeExecutableLocator.Find(
            pathVariable: null,
            localApplicationData: null,
            appData: null,
            fileExists: path => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase),
            userProfile: userProfile);

        Assert.Equal(expected, result);
    }
}
