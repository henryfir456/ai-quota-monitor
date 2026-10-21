using System.Diagnostics;
using System.Text;
using AiQuotaMonitor.Core;
using AiQuotaMonitor.Providers.Claude;
using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class ClaudeStatuslineBridgeTests
{
    [Fact]
    public async Task PowerShellHelperSendsOnlyQuotaFieldsOverCurrentUserPipe()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var helperPath = FindHelperPath();
        Assert.True(File.Exists(helperPath));
        var provider = new ClaudeStatuslineProvider();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var hostTask = new ClaudeStatuslinePipeHost(provider).RunAsync(cancellation.Token);
        var result = await RunHelperAsync(helperPath,
            "{\"rate_limits\":{\"five_hour\":{\"used_percentage\":23.5,\"resets_at\":1800000000},\"seven_day\":{\"used_percentage\":41.2,\"resets_at\":1800600000}},\"session_id\":\"SYNTHETIC_SESSION_ONLY\",\"transcript_path\":\"SYNTHETIC_PATH_ONLY\"}");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("5h 76.5%", result.Output);
        Assert.Contains("7d 58.8%", result.Output);
        Assert.False(result.Output.Contains("SYNTHETIC_SESSION_ONLY", StringComparison.Ordinal));
        Assert.False(result.Output.Contains("SYNTHETIC_PATH_ONLY", StringComparison.Ordinal));
        Assert.False(result.Error.Contains("SYNTHETIC_SESSION_ONLY", StringComparison.Ordinal));
        Assert.False(result.Error.Contains("SYNTHETIC_PATH_ONLY", StringComparison.Ordinal));

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);
        Assert.Equal(76.5d, snapshot.Windows.Single(window => window.WindowId == WindowId.FiveHour).RemainingPercent);
        Assert.Equal(58.8d, snapshot.Windows.Single(window => window.WindowId == WindowId.Weekly).RemainingPercent);

        var unavailable = await RunHelperAsync(helperPath, "{\"rate_limits\":{}}");
        Assert.Equal(0, unavailable.ExitCode);
        Assert.Contains("N/A", unavailable.Output);

        var clearedSnapshot = await provider.GetSnapshotAsync(CancellationToken.None);
        Assert.All(clearedSnapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(clearedSnapshot.Windows, window => Assert.Null(window.RemainingPercent));

        cancellation.Cancel();
        await hostTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunHelperAsync(string helperPath, string input)
    {
        var powershellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell", "v1.0", "powershell.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = powershellPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(helperPath);
        var inheritedEnvironment = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .Where(entry => entry.Key is string && entry.Value is string)
            .Select(entry => new KeyValuePair<string, string?>((string)entry.Key, (string)entry.Value!));
        startInfo.Environment.Clear();
        foreach (var variable in CodexProcessEnvironment.Filter(inheritedEnvironment))
        {
            startInfo.Environment[variable.Key] = variable.Value;
        }

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start());
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(input);
        process.StandardInput.Close();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        return (process.ExitCode, await outputTask, await errorTask);
    }

    private static string FindHelperPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "src", "AiQuotaMonitor.App", "claude-statusline.ps1");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException("Claude statusline helper was not found in the source tree.");
    }
}
