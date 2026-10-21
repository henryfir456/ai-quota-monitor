using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers.Claude;

public sealed class ClaudeStatuslineProvider : IQuotaProvider
{
    private static readonly TimeSpan FreshnessWindow = TimeSpan.FromMinutes(15);
    private readonly object _sync = new();
    private ProviderSnapshot _snapshot;

    public ClaudeStatuslineProvider(
        string initialErrorCode = "statusline_not_configured",
        DateTimeOffset? initializedAtUtc = null)
    {
        _snapshot = CreateUnavailable(initialErrorCode, initializedAtUtc ?? DateTimeOffset.UtcNow);
    }

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

    public void SetUnavailableReason(string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ProviderSnapshot? updated = null;
        lock (_sync)
        {
            if (_snapshot.Windows.Any(window => window.Status == QuotaStatus.Fresh && window.RemainingPercent is not null))
            {
                return;
            }

            _snapshot = updated = CreateUnavailable(errorCode, DateTimeOffset.UtcNow);
        }

        SnapshotUpdated?.Invoke(updated);
    }

    private static ProviderSnapshot MarkStaleIfNeeded(ProviderSnapshot snapshot, DateTimeOffset now)
    {
        return new ProviderSnapshot(ProviderId.Claude, snapshot.Windows.Select(window =>
        {
            var timedOut = window.Status == QuotaStatus.Unknown &&
                           window.ErrorCode == "statusline_waiting_for_data" &&
                           now - window.ObservedAtUtc > FreshnessWindow;
            var stale = window.Status == QuotaStatus.Fresh && now - window.ObservedAtUtc > FreshnessWindow;
            return new QuotaObservation(
                window.ProviderId,
                window.WindowId,
                window.RemainingPercent,
                window.ResetAtUtc,
                window.ObservedAtUtc,
                window.Source,
                timedOut ? QuotaStatus.Error : stale ? QuotaStatus.Stale : window.Status,
                timedOut ? "timeout" : window.ErrorCode);
        }));
    }

    private static ProviderSnapshot CreateUnavailable(string errorCode, DateTimeOffset observedAtUtc) => new(ProviderId.Claude,
    [
        New(WindowId.FiveHour, errorCode, observedAtUtc),
        New(WindowId.Weekly, errorCode, observedAtUtc)
    ]);

    private static QuotaObservation New(WindowId windowId, string errorCode, DateTimeOffset observedAtUtc) => new(
        ProviderId.Claude,
        windowId,
        null,
        null,
        observedAtUtc,
        ClaudeStatuslineParser.SourceName,
        QuotaStatus.Unknown,
        errorCode);
}
