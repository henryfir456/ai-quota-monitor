using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers.Codex;

public sealed class CodexQuotaProvider : IQuotaProvider
{
    private readonly Func<string?> _findExecutable;
    private readonly Func<string, CancellationToken, Task<string>> _readRateLimits;

    public CodexQuotaProvider(
        Func<string?>? findExecutable = null,
        Func<string, CancellationToken, Task<string>>? readRateLimits = null)
    {
        _findExecutable = findExecutable ?? CodexExecutableLocator.Find;
        _readRateLimits = readRateLimits ?? CodexAppServerClient.ReadRateLimitsAsync;
    }

    public ProviderId ProviderId => ProviderId.Codex;

    public string Source => CodexRateLimitParser.SourceName;

    public async Task<ProviderSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var executable = _findExecutable();
        if (string.IsNullOrWhiteSpace(executable))
        {
            return Empty(QuotaStatus.Unknown, "codex_cli_not_found");
        }

        try
        {
            var response = await _readRateLimits(executable, cancellationToken).ConfigureAwait(false);
            return CodexRateLimitParser.ParseResponse(response, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (CodexRpcException exception) when (exception.ErrorCode == -32601)
        {
            return Empty(QuotaStatus.Unsupported, "rpc_method_not_supported");
        }
        catch (CodexRpcException)
        {
            return Empty(QuotaStatus.Unknown, "codex_account_unavailable");
        }
        catch (CodexNotLoggedInException)
        {
            return Empty(QuotaStatus.Unknown, "codex_not_logged_in");
        }
        catch (FileNotFoundException)
        {
            return Empty(QuotaStatus.Unsupported, "codex_cli_unavailable");
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return Empty(QuotaStatus.Unsupported, "codex_cli_unavailable");
        }
        catch (TimeoutException)
        {
            return Empty(QuotaStatus.Error, "timeout");
        }
        catch (System.Text.Json.JsonException)
        {
            return Empty(QuotaStatus.Error, "parse_failure");
        }
        catch (IOException)
        {
            return Empty(QuotaStatus.Error, "app_server_unreachable");
        }
        catch (System.Net.Http.HttpRequestException)
        {
            return Empty(QuotaStatus.Error, "network_unreachable");
        }
        catch
        {
            return Empty(QuotaStatus.Error, "provider_failure");
        }
    }

    private ProviderSnapshot Empty(QuotaStatus status, string errorCode)
    {
        var observedAt = DateTimeOffset.UtcNow;
        return new ProviderSnapshot(ProviderId,
        [
            new QuotaObservation(ProviderId, WindowId.FiveHour, null, null, observedAt, Source, status, errorCode),
            new QuotaObservation(ProviderId, WindowId.Weekly, null, null, observedAt, Source, status, errorCode)
        ]);
    }
}
