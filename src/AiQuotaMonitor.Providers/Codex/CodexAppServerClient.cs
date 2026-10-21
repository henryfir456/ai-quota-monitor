using System.Diagnostics;
using System.Text;

namespace AiQuotaMonitor.Providers.Codex;

public static class CodexAppServerClient
{
    public static async Task<string> ReadRateLimitsAsync(string executablePath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardErrorEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };
        startInfo.ArgumentList.Add("app-server");
        var minimalEnvironment = CodexProcessEnvironment.ReadCurrent();
        startInfo.Environment.Clear();
        foreach (var pair in minimalEnvironment)
        {
            startInfo.Environment[pair.Key] = pair.Value;
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Codex app-server could not be started.");
        }

        var stderrDrain = process.StandardError.BaseStream.CopyToAsync(Stream.Null);
        try
        {
            using var writer = process.StandardInput;
            using var reader = process.StandardOutput;
            var rpc = new JsonLineRpcConnection(reader, writer);
            await rpc.RequestAsync(
                id: 0,
                method: "initialize",
                parameters: new
                {
                    clientInfo = new
                    {
                        name = "ai_quota_monitor",
                        title = "Windows AI Quota Monitor",
                        version = "0.1.0"
                    }
                },
                cancellationToken).ConfigureAwait(false);
            await rpc.NotifyAsync("initialized", cancellationToken).ConfigureAwait(false);
            return await rpc.RequestAsync(
                id: 1,
                method: "account/rateLimits/read",
                parameters: new { },
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception) when (process.HasExited)
            {
            }

            try
            {
                await process.WaitForExitAsync(CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                await stderrDrain.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
            }
        }
    }
}
