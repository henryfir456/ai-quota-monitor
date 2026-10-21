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

    [Fact]
    public void ExplicitDesktopCliPathIsCheckedBeforeOtherInstallations()
    {
        var expected = Path.Combine("C:", "Codex Desktop", "codex.exe");
        var calls = new List<string>();

        var result = CodexExecutableLocator.Find(
            pathVariable: "C:/npm/bin",
            localApplicationData: "C:/Users/SyntheticUser/AppData/Local",
            userProfile: "C:/Users/SyntheticUser",
            fileExists: path =>
            {
                calls.Add(path);
                return string.Equals(path, expected, StringComparison.OrdinalIgnoreCase);
            },
            codexCliPath: expected,
            appData: "C:/Users/SyntheticUser/AppData/Roaming",
            windowsDesktopPackageDirectories: []);

        Assert.Equal(expected, result);
        Assert.Equal(expected, calls[0]);
    }

    [Fact]
    public void FindsNpmNativeBinaryWithoutLaunchingTheCmdShim()
    {
        var appData = Path.Combine("C:", "Users", "SyntheticUser", "AppData", "Roaming");
        var expected = Path.Combine(appData, "npm", "node_modules", "@openai", "codex", "node_modules", "@openai", "codex-win32-x64", "vendor", "x86_64-pc-windows-msvc", "bin", "codex.exe");

        var result = CodexExecutableLocator.Find(
            pathVariable: Path.Combine(appData, "npm"),
            localApplicationData: Path.Combine("C:", "Users", "SyntheticUser", "AppData", "Local"),
            userProfile: Path.Combine("C:", "Users", "SyntheticUser"),
            fileExists: path => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase),
            appData: appData,
            windowsDesktopPackageDirectories: []);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FindsWindowsDesktopPackageNativeBinaryWhenPackageDirectoryIsReadable()
    {
        var packageRoot = Path.Combine("C:", "Program Files", "WindowsApps", "OpenAI.Codex_1.2.3.0_x64__2p2nqsd0c76g0");
        var expected = Path.Combine(packageRoot, "app", "resources", "codex.exe");

        var result = CodexExecutableLocator.Find(
            pathVariable: null,
            localApplicationData: "C:/Users/SyntheticUser/AppData/Local",
            userProfile: "C:/Users/SyntheticUser",
            fileExists: path => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase),
            appData: null,
            windowsDesktopPackageDirectories: [packageRoot]);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("packages/standalone/current/bin/codex.exe")]
    [InlineData("packages/app-server-daemon/current/bin/codex.exe")]
    public void FindsOfficialStandaloneOrManagedAppServerInstallUnderCodexHome(string relativePath)
    {
        var codexHome = Path.Combine("D:", "Codex Home");
        var expected = Path.Combine(codexHome, relativePath.Replace('/', Path.DirectorySeparatorChar));

        var result = CodexExecutableLocator.Find(
            pathVariable: null,
            localApplicationData: null,
            userProfile: null,
            fileExists: path => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase),
            codexHome: codexHome);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void FindsStandaloneInstallUnderDefaultCodexHome()
    {
        var userProfile = Path.Combine("C:", "Users", "SyntheticUser");
        var expected = Path.Combine(userProfile, ".codex", "packages", "standalone", "current", "bin", "codex.exe");

        var result = CodexExecutableLocator.Find(
            pathVariable: null,
            localApplicationData: null,
            userProfile: userProfile,
            fileExists: path => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase));

        Assert.Equal(expected, result);
    }
}
