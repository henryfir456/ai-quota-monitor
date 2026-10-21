namespace AiQuotaMonitor.Core;

public sealed class ProviderSnapshot
{
    public ProviderSnapshot(ProviderId providerId, IEnumerable<QuotaObservation> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        var materialized = windows.ToArray();
        if (materialized.Any(window => window.ProviderId != providerId))
        {
            throw new ArgumentException("所有額度視窗必須屬於同一個供應商。", nameof(windows));
        }

        ProviderId = providerId;
        Windows = Array.AsReadOnly(materialized);
    }

    public ProviderId ProviderId { get; }

    public IReadOnlyList<QuotaObservation> Windows { get; }
}
