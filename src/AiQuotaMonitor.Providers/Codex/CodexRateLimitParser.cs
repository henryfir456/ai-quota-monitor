using System.Text.Json;
using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Providers.Codex;

public static class CodexRateLimitParser
{
    public const string SourceName = "Codex app-server account/rateLimits/read";

    public static ProviderSnapshot ParseResponse(string json, DateTimeOffset observedAtUtc)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return Empty(QuotaStatus.Unsupported, "unsupported_response", observedAtUtc);
            }

            // JsonLineRpcConnection returns the decoded JSON-RPC result body. Also accept
            // a full envelope so parser fixtures and future callers can validate it directly.
            var result = root.TryGetProperty("result", out var envelopeResult)
                ? envelopeResult
                : root;
            if (result.ValueKind != JsonValueKind.Object)
            {
                return Empty(QuotaStatus.Unsupported, "unsupported_response", observedAtUtc);
            }

            var limits = FindCodexBucket(result);
            if (limits is null)
            {
                return Empty(QuotaStatus.Unknown, "codex_limit_bucket_missing", observedAtUtc);
            }

            var fiveHour = ParseCandidate(limits.Value, "primary", observedAtUtc);
            var secondary = ParseCandidate(limits.Value, "secondary", observedAtUtc);
            var candidates = new[] { fiveHour, secondary }.Where(candidate => candidate is not null).Cast<Candidate>().ToArray();
            var mapped = new Dictionary<WindowId, Candidate>();

            foreach (var candidate in candidates)
            {
                if (candidate.WindowId == WindowId.Other)
                {
                    continue;
                }

                if (!mapped.TryAdd(candidate.WindowId, candidate))
                {
                    return Empty(QuotaStatus.Error, "duplicate_window", observedAtUtc);
                }
            }

            if (mapped.Count == 0)
            {
                return Empty(QuotaStatus.Unsupported, "window_duration_unavailable", observedAtUtc);
            }

            var hasWeekly = mapped.ContainsKey(WindowId.Weekly);
            var observations = new[]
            {
                ToObservation(WindowId.FiveHour, mapped.GetValueOrDefault(WindowId.FiveHour), hasWeekly, observedAtUtc),
                ToObservation(WindowId.Weekly, mapped.GetValueOrDefault(WindowId.Weekly), hasWeekly, observedAtUtc)
            };
            return new ProviderSnapshot(ProviderId.Codex, observations);
        }
        catch (JsonException)
        {
            return Empty(QuotaStatus.Error, "parse_failure", observedAtUtc);
        }
    }

    private static JsonElement? FindCodexBucket(JsonElement result)
    {
        if (result.TryGetProperty("rateLimitsByLimitId", out var buckets) &&
            buckets.ValueKind == JsonValueKind.Object &&
            buckets.TryGetProperty("codex", out var codexBucket) &&
            codexBucket.ValueKind == JsonValueKind.Object)
        {
            return codexBucket;
        }

        if (result.TryGetProperty("rateLimits", out var legacy) && legacy.ValueKind == JsonValueKind.Object)
        {
            if (legacy.TryGetProperty("limitId", out var limitId))
            {
                if (limitId.ValueKind == JsonValueKind.String && limitId.GetString() == "codex")
                {
                    return legacy;
                }

                if (limitId.ValueKind == JsonValueKind.Null && HasLegacyQuotaWindows(legacy))
                {
                    return legacy;
                }
            }
            else if (HasLegacyQuotaWindows(legacy))
            {
                // Older single-bucket responses omit limitId. Treat it as Codex only
                // when at least one official window field is actually present.
                return legacy;
            }
        }

        return null;
    }

    private static bool HasLegacyQuotaWindows(JsonElement legacy) =>
        (legacy.TryGetProperty("primary", out var primary) && primary.ValueKind != JsonValueKind.Null) ||
        (legacy.TryGetProperty("secondary", out var secondary) && secondary.ValueKind != JsonValueKind.Null);

    private static Candidate? ParseCandidate(JsonElement limits, string name, DateTimeOffset observedAtUtc)
    {
        if (!limits.TryGetProperty(name, out var window) || window.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (window.ValueKind != JsonValueKind.Object ||
            !window.TryGetProperty("windowDurationMins", out var durationElement) ||
            durationElement.ValueKind != JsonValueKind.Number ||
            !durationElement.TryGetInt64(out var duration))
        {
            return null;
        }

        var windowId = duration switch
        {
            300 => WindowId.FiveHour,
            10080 => WindowId.Weekly,
            _ => WindowId.Other
        };

        if (windowId == WindowId.Other)
        {
            return new Candidate(windowId, null, null, null, null);
        }

        if (!window.TryGetProperty("usedPercent", out var usedElement) ||
            usedElement.ValueKind != JsonValueKind.Number ||
            !usedElement.TryGetInt32(out var used) ||
            used is < 0 or > 100)
        {
            return new Candidate(windowId, null, null, QuotaStatus.Error, "invalid_used_percent");
        }

        var resetAtUtc = ParseResetAt(window, out var resetError);
        return new Candidate(
            windowId,
            100d - used,
            resetAtUtc,
            QuotaStatus.Fresh,
            resetError);
    }

    private static DateTimeOffset? ParseResetAt(JsonElement window, out string? errorCode)
    {
        errorCode = null;
        if (!window.TryGetProperty("resetsAt", out var resetElement) || resetElement.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (resetElement.ValueKind != JsonValueKind.Number || !resetElement.TryGetInt64(out var epochSeconds))
        {
            errorCode = "invalid_reset_timestamp";
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(epochSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            errorCode = "invalid_reset_timestamp";
            return null;
        }
    }

    private static QuotaObservation ToObservation(
        WindowId windowId,
        Candidate? candidate,
        bool hasWeekly,
        DateTimeOffset observedAtUtc)
    {
        if (candidate is null && windowId == WindowId.FiveHour && hasWeekly)
        {
            return new QuotaObservation(
                ProviderId.Codex, windowId, null, null, observedAtUtc, SourceName,
                QuotaStatus.Unsupported, "five_hour_window_not_reported");
        }

        if (candidate is null)
        {
            return new QuotaObservation(
                ProviderId.Codex, windowId, null, null, observedAtUtc, SourceName,
                QuotaStatus.Unknown, "window_not_reported");
        }

        return new QuotaObservation(
            ProviderId.Codex,
            windowId,
            candidate.RemainingPercent,
            candidate.ResetAtUtc,
            observedAtUtc,
            SourceName,
            candidate.Status ?? QuotaStatus.Unknown,
            candidate.ErrorCode);
    }

    private static ProviderSnapshot Empty(QuotaStatus status, string errorCode, DateTimeOffset observedAtUtc) =>
        new(ProviderId.Codex,
        [
            new QuotaObservation(ProviderId.Codex, WindowId.FiveHour, null, null, observedAtUtc, SourceName, status, errorCode),
            new QuotaObservation(ProviderId.Codex, WindowId.Weekly, null, null, observedAtUtc, SourceName, status, errorCode)
        ]);

    private sealed record Candidate(
        WindowId WindowId,
        double? RemainingPercent,
        DateTimeOffset? ResetAtUtc,
        QuotaStatus? Status,
        string? ErrorCode);
}
