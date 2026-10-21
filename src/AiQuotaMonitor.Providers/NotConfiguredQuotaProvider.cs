using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers;

public sealed class NotConfiguredQuotaProvider(ProviderId providerId) : IQuotaProvider
{
    public ProviderId ProviderId { get; } = providerId;

    public string Source => "未連線資料來源";

    public Task<ProviderSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observedAt = DateTimeOffset.UtcNow;
        var windows = new[]
        {
            Create(WindowId.FiveHour),
            Create(WindowId.Weekly)
        };

        return Task.FromResult(new ProviderSnapshot(ProviderId, windows));

        QuotaObservation Create(WindowId windowId) => new(
            ProviderId,
            windowId,
            remainingPercent: null,
            resetAtUtc: null,
            observedAtUtc: observedAt,
            Source,
            QuotaStatus.Unknown,
            errorCode: "source_not_configured");
    }
}
