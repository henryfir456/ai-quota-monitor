namespace AiQuotaMonitor.Core;

public sealed record QuotaObservation
{
    public QuotaObservation(
        ProviderId providerId,
        WindowId windowId,
        double? remainingPercent,
        DateTimeOffset? resetAtUtc,
        DateTimeOffset observedAtUtc,
        string source,
        QuotaStatus status,
        string? errorCode = null,
        bool isDemo = false)
    {
        if (remainingPercent is double value && (!double.IsFinite(value) || value < 0 || value > 100))
        {
            throw new ArgumentOutOfRangeException(nameof(remainingPercent), "剩餘百分比必須是 0 到 100 之間的有限數值。");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        ProviderId = providerId;
        WindowId = windowId;
        RemainingPercent = remainingPercent;
        ResetAtUtc = resetAtUtc?.ToUniversalTime();
        ObservedAtUtc = observedAtUtc.ToUniversalTime();
        Source = source;
        Status = status;
        ErrorCode = errorCode;
        IsDemo = isDemo;
    }

    public ProviderId ProviderId { get; }

    public WindowId WindowId { get; }

    public double? RemainingPercent { get; }

    public double? UsedPercent => RemainingPercent is double remaining ? 100 - remaining : null;

    public DateTimeOffset? ResetAtUtc { get; }

    public DateTimeOffset ObservedAtUtc { get; }

    public string Source { get; }

    public QuotaStatus Status { get; }

    public string? ErrorCode { get; }

    public bool IsDemo { get; }
}
