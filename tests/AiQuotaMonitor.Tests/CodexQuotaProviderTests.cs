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
        Assert.All(snapshot.Windows, window => Assert.Equal("account_unavailable", window.ErrorCode));
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
