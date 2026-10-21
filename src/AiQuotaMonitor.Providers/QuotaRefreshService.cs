using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers;

public sealed class QuotaRefreshService
{
    public static readonly TimeSpan DefaultProviderTimeout = TimeSpan.FromSeconds(10);

    private readonly IReadOnlyList<IQuotaProvider> _providers;
    private readonly TimeSpan _providerTimeout;

    public QuotaRefreshService(IEnumerable<IQuotaProvider> providers, TimeSpan? providerTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToArray();
        _providerTimeout = providerTimeout ?? DefaultProviderTimeout;
        if (_providerTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(providerTimeout));
        }

        if (_providers.Select(provider => provider.ProviderId).Distinct().Count() != _providers.Count)
        {
            throw new ArgumentException("每個供應商只能註冊一次。", nameof(providers));
        }
    }

    public async Task<IReadOnlyList<ProviderSnapshot>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Select(provider => RefreshProviderAsync(provider, cancellationToken));
        return await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task<ProviderSnapshot> RefreshProviderAsync(
        IQuotaProvider provider,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_providerTimeout);

        try
        {
            var result = await provider.GetSnapshotAsync(timeout.Token).ConfigureAwait(false);
            if (result.ProviderId != provider.ProviderId)
            {
                throw new InvalidOperationException("Provider returned a snapshot for a different provider.");
            }

            return result;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(provider, "timeout");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Failure(provider, "provider_failure");
        }
    }

    private static ProviderSnapshot Failure(IQuotaProvider provider, string errorCode)
    {
        var observedAt = DateTimeOffset.UtcNow;
        return new ProviderSnapshot(provider.ProviderId,
        [
            Error(WindowId.FiveHour),
            Error(WindowId.Weekly)
        ]);

        QuotaObservation Error(WindowId windowId) => new(
            provider.ProviderId,
            windowId,
            remainingPercent: null,
            resetAtUtc: null,
            observedAtUtc: observedAt,
            provider.Source,
            QuotaStatus.Error,
            errorCode);
    }
}
