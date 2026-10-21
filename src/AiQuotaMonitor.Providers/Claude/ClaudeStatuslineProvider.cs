using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers.Claude;

public sealed class ClaudeStatuslineProvider : IQuotaProvider
{
    private static readonly TimeSpan FreshnessWindow = TimeSpan.FromMinutes(15);
    private readonly object _sync = new();
    private ProviderSnapshot _snapshot = CreateUnavailable();

    public event Action<ProviderSnapshot>? SnapshotUpdated;

    public ProviderId ProviderId => ProviderId.Claude;

    public string Source => ClaudeStatuslineParser.SourceName;

    public Task<ProviderSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            return Task.FromResult(MarkStaleIfNeeded(_snapshot, DateTimeOffset.UtcNow));
        }
    }

    public void Accept(string json, DateTimeOffset? observedAtUtc = null)
    {
        var parsed = ClaudeStatuslineParser.Parse(json, observedAtUtc ?? DateTimeOffset.UtcNow);
        lock (_sync)
        {
            _snapshot = parsed;
        }

        SnapshotUpdated?.Invoke(parsed);
    }

    private static ProviderSnapshot MarkStaleIfNeeded(ProviderSnapshot snapshot, DateTimeOffset now)
    {
        return new ProviderSnapshot(ProviderId.Claude, snapshot.Windows.Select(window => new QuotaObservation(
            window.ProviderId,
            window.WindowId,
            window.RemainingPercent,
            window.ResetAtUtc,
            window.ObservedAtUtc,
            window.Source,
            window.Status == QuotaStatus.Fresh && now - window.ObservedAtUtc > FreshnessWindow
                ? QuotaStatus.Stale
                : window.Status,
            window.ErrorCode)));
    }

    private static ProviderSnapshot CreateUnavailable() => new(ProviderId.Claude,
    [
        New(WindowId.FiveHour),
        New(WindowId.Weekly)
    ]);

    private static QuotaObservation New(WindowId windowId) => new(
        ProviderId.Claude,
        windowId,
        null,
        null,
        DateTimeOffset.UtcNow,
        ClaudeStatuslineParser.SourceName,
        QuotaStatus.Unknown,
        "statusline_not_configured");
}
