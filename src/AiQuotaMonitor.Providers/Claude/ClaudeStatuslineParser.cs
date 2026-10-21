using System.Text.Json;
using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers.Claude;

public static class ClaudeStatuslineParser
{
    public const string SourceName = "Claude Code official statusline JSON (user configured)";

    public static ProviderSnapshot Parse(string json, DateTimeOffset observedAtUtc)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("rate_limits", out var limits) || limits.ValueKind != JsonValueKind.Object)
            {
                return Empty(QuotaStatus.Unknown, "statusline_rate_limits_missing", observedAtUtc);
            }

            return new ProviderSnapshot(ProviderId.Claude,
            [
                ParseWindow(limits, "five_hour", WindowId.FiveHour, observedAtUtc),
                ParseWindow(limits, "seven_day", WindowId.Weekly, observedAtUtc)
            ]);
        }
        catch (JsonException)
        {
            return Empty(QuotaStatus.Unsupported, "unsupported_statusline_data", observedAtUtc);
        }
    }

    private static QuotaObservation ParseWindow(
        JsonElement limits,
        string name,
        WindowId windowId,
        DateTimeOffset observedAtUtc)
    {
        if (!limits.TryGetProperty(name, out var window) || window.ValueKind != JsonValueKind.Object)
        {
            return New(windowId, null, null, observedAtUtc, QuotaStatus.Unknown, "window_not_reported");
        }

        if (!window.TryGetProperty("used_percentage", out var usedElement) ||
            usedElement.ValueKind != JsonValueKind.Number ||
            !usedElement.TryGetDouble(out var used) ||
            !double.IsFinite(used) || used is < 0 or > 100)
        {
            return New(windowId, null, null, observedAtUtc, QuotaStatus.Error, "invalid_used_percent");
        }

        DateTimeOffset? resetAt = null;
        string? resetError = null;
        if (window.TryGetProperty("resets_at", out var resetElement) && resetElement.ValueKind != JsonValueKind.Null)
        {
            if (resetElement.ValueKind == JsonValueKind.Number &&
                resetElement.TryGetInt64(out var epochSeconds))
            {
                try
                {
                    resetAt = DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
                }
                catch (ArgumentOutOfRangeException)
                {
                    resetError = "invalid_reset_timestamp";
                }
            }
            else
            {
                resetError = "invalid_reset_timestamp";
            }
        }

        return New(windowId, 100d - used, resetAt, observedAtUtc, QuotaStatus.Fresh, resetError);
    }

    private static QuotaObservation New(
        WindowId windowId,
        double? remaining,
        DateTimeOffset? resetAt,
        DateTimeOffset observedAt,
        QuotaStatus status,
        string? errorCode) => new(
            ProviderId.Claude, windowId, remaining, resetAt, observedAt, SourceName, status, errorCode);

    private static ProviderSnapshot Empty(QuotaStatus status, string errorCode, DateTimeOffset observedAt) =>
        new(ProviderId.Claude,
        [
            New(WindowId.FiveHour, null, null, observedAt, status, errorCode),
            New(WindowId.Weekly, null, null, observedAt, status, errorCode)
        ]);
}
