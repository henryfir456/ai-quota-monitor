using System.Diagnostics;
using System.Text;
using AiQuotaMonitor.Core;
using AiQuotaMonitor.Providers.Claude;

namespace AiQuotaMonitor.Tests;

public sealed class ClaudeStatuslineBridgeTests
{
    private static readonly string[] PowerShellEnvironmentNames =
    [
        "PATH", "SYSTEMROOT", "WINDIR", "USERPROFILE", "HOMEDRIVE", "HOMEPATH",
        "LOCALAPPDATA", "APPDATA", "TEMP", "TMP", "COMSPEC",
        "PATHEXT", "ProgramFiles", "ProgramFiles(x86)", "ProgramW6432",
        "CommonProgramFiles", "CommonProgramFiles(x86)", "SYSTEMDRIVE", "COMPUTERNAME",
        "USERNAME", "USERDOMAIN", "PROCESSOR_ARCHITECTURE"
    ];

    [Fact]
    public async Task PowerShellStartsAndConsumesStdinWithAllowlistedEnvironment()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string command = "$null = [Console]::In.ReadToEnd(); [Console]::Out.WriteLine('stdin-consumed')";
        var result = await RunPowerShellAsync(["-Command", command], "synthetic-input");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("stdin-consumed", result.Output);
    }

    [Fact]
    public async Task PowerShellImportsJsonCmdletWithAllowlistedEnvironment()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const string command = "$ErrorActionPreference = 'Stop'; $raw = [Console]::In.ReadToEnd(); [Console]::Out.WriteLine('input-read'); Import-Module Microsoft.PowerShell.Utility -ErrorAction Stop; [Console]::Out.WriteLine('module-loaded'); $null = ConvertFrom-Json -InputObject $raw -ErrorAction Stop; [Console]::Out.WriteLine('json-parsed')";
        const string input = "{\"rate_limits\":{\"five_hour\":{\"used_percentage\":23.5,\"resets_at\":1800000000},\"seven_day\":{\"used_percentage\":41.2,\"resets_at\":1800600000}},\"session_id\":\"SYNTHETIC_SESSION_ONLY\",\"transcript_path\":\"SYNTHETIC_PATH_ONLY\"}";
        var result = await RunPowerShellAsync(["-Command", command], input);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("module-loaded", result.Output);
        Assert.Contains("json-parsed", result.Output);
    }

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
        var result = await RunPowerShellAsync(["-File", helperPath],
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

        var unavailable = await RunPowerShellAsync(["-File", helperPath], "{\"rate_limits\":{}}");
        Assert.Equal(0, unavailable.ExitCode);
        Assert.Contains("N/A", unavailable.Output);

        var clearedSnapshot = await provider.GetSnapshotAsync(CancellationToken.None);
        Assert.All(clearedSnapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(clearedSnapshot.Windows, window => Assert.Null(window.RemainingPercent));

        const string privateMalformedInput = "private malformed synthetic input";
        var malformed = await RunPowerShellAsync(["-File", helperPath], privateMalformedInput);
        Assert.Equal(0, malformed.ExitCode);
        Assert.Contains("N/A", malformed.Output);
        Assert.DoesNotContain(privateMalformedInput, malformed.Output, StringComparison.Ordinal);
        Assert.DoesNotContain(privateMalformedInput, malformed.Error, StringComparison.Ordinal);
        var parseFailure = await provider.GetSnapshotAsync(CancellationToken.None);
        Assert.All(parseFailure.Windows, window => Assert.Equal(QuotaStatus.Error, window.Status));
        Assert.All(parseFailure.Windows, window => Assert.Equal("parse_failure", window.ErrorCode));
        Assert.All(parseFailure.Windows, window => Assert.Null(window.RemainingPercent));

        cancellation.Cancel();
        await hostTask.WaitAsync(TimeSpan.FromSeconds(3));
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunPowerShellAsync(
        IReadOnlyList<string> arguments,
        string input)
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
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = new UTF8Encoding(false),
            StandardErrorEncoding = new UTF8Encoding(false)
        };
        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment.Clear();
        foreach (var name in PowerShellEnvironmentNames)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                startInfo.Environment[name] = value;
            }
        }

        var powerShellHome = Path.GetDirectoryName(powershellPath)!;
        startInfo.Environment["PSHOME"] = powerShellHome;
        startInfo.Environment["PSModulePath"] = Path.Combine(powerShellHome, "Modules");

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start());
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteLineAsync(input);
        await process.StandardInput.FlushAsync();
        process.StandardInput.Close();
        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));
        }
        catch (TimeoutException exception)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;
            foreach (var name in new[] { "USERPROFILE", "USERNAME", "USERDOMAIN", "COMPUTERNAME" })
            {
                var value = Environment.GetEnvironmentVariable(name);
                if (!string.IsNullOrWhiteSpace(value) && value.Length >= 3)
                {
                    output = output.Replace(value, "<redacted>", StringComparison.OrdinalIgnoreCase);
                    error = error.Replace(value, "<redacted>", StringComparison.OrdinalIgnoreCase);
                }
            }

            throw new TimeoutException($"PowerShell helper timed out. stdout: {output}; stderr: {error}", exception);
        }

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
