using AiQuotaMonitor.Core;
using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class CodexQuotaProviderTests
{
    [Fact]
    public async Task MissingExecutableIsUnknownAndDoesNotAttemptAnAlternativeSource()
    {
        var readAttempted = false;
        var provider = new CodexQuotaProvider(
            findExecutable: () => null,
            readRateLimits: (_, _) =>
            {
                readAttempted = true;
                return Task.FromResult("{}");
            });

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.False(readAttempted);
        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("codex_cli_not_found", window.ErrorCode));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Fact]
    public async Task NotLoggedInIsDistinctFromAnUnavailableAccountService()
    {
        var provider = new CodexQuotaProvider(
            findExecutable: () => "synthetic-codex-path",
            readRateLimits: (_, _) => throw new CodexNotLoggedInException());

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("codex_not_logged_in", window.ErrorCode));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Fact]
    public async Task TimeoutIsReportedWithoutTurningItIntoAQuotaValue()
    {
        var provider = new CodexQuotaProvider(
            findExecutable: () => "synthetic-codex-path",
            readRateLimits: (_, _) => throw new TimeoutException("synthetic timeout details"));

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Error, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("timeout", window.ErrorCode));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Fact]
    public async Task InvalidRpcJsonIsReportedAsParseFailure()
    {
        var provider = new CodexQuotaProvider(
            findExecutable: () => "synthetic-codex-path",
            readRateLimits: (_, _) => Task.FromResult("not-json"));

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Error, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("parse_failure", window.ErrorCode));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Fact]
    public async Task ClosedAppServerIsReportedAsUnreachable()
    {
        var provider = new CodexQuotaProvider(
            findExecutable: () => "synthetic-codex-path",
            readRateLimits: (_, _) => throw new EndOfStreamException("synthetic server text"));

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Error, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("app_server_unreachable", window.ErrorCode));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Fact]
    public async Task UnsupportedRpcMethodIsExplicitlyUnsupported()
    {
        var provider = new CodexQuotaProvider(
            findExecutable: () => "synthetic-codex-path",
            readRateLimits: (_, _) => throw new CodexRpcException(-32601));

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unsupported, window.Status));
    }

    [Fact]
    public async Task AccountErrorsDoNotExposeRpcMessageOrInventNumbers()
    {
        var provider = new CodexQuotaProvider(
            findExecutable: () => "synthetic-codex-path",
            readRateLimits: (_, _) => throw new CodexRpcException(-32001));

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
        Assert.All(snapshot.Windows, window => Assert.Equal("codex_account_unavailable", window.ErrorCode));
    }

    [Fact]
    public async Task UnexpectedProviderFailureUsesSanitizedErrorCode()
    {
        var provider = new CodexQuotaProvider(
            findExecutable: () => "synthetic-codex-path",
            readRateLimits: (_, _) => throw new InvalidOperationException("synthetic-not-for-display"));

        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Error, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("provider_failure", window.ErrorCode));
        Assert.All(snapshot.Windows, window => Assert.False(window.Source.Contains("synthetic-not-for-display", StringComparison.Ordinal)));
    }
}
