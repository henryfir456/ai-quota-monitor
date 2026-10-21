using AiQuotaMonitor.Core;
using AiQuotaMonitor.Providers;

namespace AiQuotaMonitor.Tests;

public sealed class QuotaRefreshServiceTests
{
    [Fact]
    public async Task OneProviderFailureDoesNotHideOtherProviderData()
    {
        var service = new QuotaRefreshService(
        [
            new DelegateProvider(ProviderId.Claude, _ => throw new InvalidOperationException("synthetic failure")),
            new DelegateProvider(ProviderId.Codex, _ => Task.FromResult(Snapshot(ProviderId.Codex, 87)))
        ]);

        var result = await service.RefreshAsync();

        Assert.Equal(QuotaStatus.Error, result.Single(snapshot => snapshot.ProviderId == ProviderId.Claude).Windows[0].Status);
        var codex = result.Single(snapshot => snapshot.ProviderId == ProviderId.Codex).Windows[0];
        Assert.Equal(QuotaStatus.Fresh, codex.Status);
        Assert.Equal(87d, codex.RemainingPercent);
    }

    [Fact]
    public async Task ProviderTimeoutBecomesAnErrorWithoutCancellingOtherSources()
    {
        var timeout = TimeSpan.FromMilliseconds(25);
        var service = new QuotaRefreshService(
        [
            new DelegateProvider(ProviderId.Claude, async token =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return Snapshot(ProviderId.Claude, 50);
            }),
            new DelegateProvider(ProviderId.Codex, _ => Task.FromResult(Snapshot(ProviderId.Codex, 12)))
        ], timeout);

        var result = await service.RefreshAsync();

        var claude = result.Single(snapshot => snapshot.ProviderId == ProviderId.Claude).Windows[0];
        Assert.Equal(QuotaStatus.Error, claude.Status);
        Assert.Equal("timeout", claude.ErrorCode);
        Assert.Equal(12d, result.Single(snapshot => snapshot.ProviderId == ProviderId.Codex).Windows[0].RemainingPercent);
    }

    [Fact]
    public async Task CallerCancellationIsNotReportedAsAProviderFailure()
    {
        using var cancellation = new CancellationTokenSource();
        var service = new QuotaRefreshService(
        [
            new DelegateProvider(ProviderId.Codex, async token =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return Snapshot(ProviderId.Codex, 50);
            })
        ]);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RefreshAsync(cancellation.Token));
    }

    private static ProviderSnapshot Snapshot(ProviderId providerId, int remaining) => new(providerId,
    [
        new QuotaObservation(providerId, WindowId.FiveHour, remaining, null, DateTimeOffset.UtcNow, "synthetic-fixture", QuotaStatus.Fresh)
    ]);

    private sealed class DelegateProvider(
        ProviderId providerId,
        Func<CancellationToken, Task<ProviderSnapshot>> read) : IQuotaProvider
    {
        public ProviderId ProviderId { get; } = providerId;

        public string Source => "synthetic-fixture";

        public Task<ProviderSnapshot> GetSnapshotAsync(CancellationToken cancellationToken) => read(cancellationToken);
    }
}
