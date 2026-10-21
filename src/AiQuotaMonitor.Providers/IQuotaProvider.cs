using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers;

public interface IQuotaProvider
{
    ProviderId ProviderId { get; }

    string Source { get; }

    Task<ProviderSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
