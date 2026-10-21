using System.Text.Json;
using AiQuotaMonitor.Core;
using AiQuotaMonitor.Providers.Codex;

namespace AiQuotaMonitor.Tests;

public sealed class CodexRateLimitParserTests
{
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse("2026-10-21T00:00:00Z");

    [Fact]
    public void MapsWindowsByDocumentedDurationAndConvertsUsedPercent()
    {
        var snapshot = ParseFixture("codex-five-hour-weekly.json");
        var fiveHour = snapshot.Windows.Single(window => window.WindowId == WindowId.FiveHour);
        var weekly = snapshot.Windows.Single(window => window.WindowId == WindowId.Weekly);

        Assert.Equal(65d, fiveHour.RemainingPercent);
        Assert.Equal(QuotaStatus.Fresh, fiveHour.Status);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), fiveHour.ResetAtUtc);
        Assert.Equal(38d, weekly.RemainingPercent);
        Assert.Equal(QuotaStatus.Fresh, weekly.Status);
    }

    [Fact]
    public void ParsesDecodedRateLimitBodyReturnedByJsonLineRpcClient()
    {
        const string json = """{"rateLimitsByLimitId":{"codex":{"primary":{"usedPercent":35,"windowDurationMins":300,"resetsAt":1800000000},"secondary":{"usedPercent":62,"windowDurationMins":10080,"resetsAt":1800600000}}}}""";

        var snapshot = CodexRateLimitParser.ParseResponse(json, ObservedAt);

        Assert.Equal(65d, snapshot.Windows.Single(w => w.WindowId == WindowId.FiveHour).RemainingPercent);
        Assert.Equal(38d, snapshot.Windows.Single(w => w.WindowId == WindowId.Weekly).RemainingPercent);
    }

    [Fact]
    public void WeeklyOnlyResponseDoesNotGuessFiveHourValue()
    {
        var snapshot = ParseFixture("codex-weekly-only.json");

        Assert.Equal(QuotaStatus.Unsupported, snapshot.Windows.Single(w => w.WindowId == WindowId.FiveHour).Status);
        Assert.Null(snapshot.Windows.Single(w => w.WindowId == WindowId.FiveHour).RemainingPercent);
        Assert.Equal(28d, snapshot.Windows.Single(w => w.WindowId == WindowId.Weekly).RemainingPercent);
    }

    [Fact]
    public void MissingResetRemainsNullWhileZeroUseMapsToFullRemaining()
    {
        var snapshot = ParseFixture("codex-missing-reset.json");
        var fiveHour = snapshot.Windows.Single(w => w.WindowId == WindowId.FiveHour);

        Assert.Equal(100d, fiveHour.RemainingPercent);
        Assert.Null(fiveHour.ResetAtUtc);
        Assert.Equal(QuotaStatus.Fresh, fiveHour.Status);
    }

    [Fact]
    public void InvalidUsedPercentDoesNotProduceQuotaNumber()
    {
        var observation = ParseFixture("codex-invalid-used-percent.json").Windows.Single(w => w.WindowId == WindowId.FiveHour);

        Assert.Null(observation.RemainingPercent);
        Assert.Equal(QuotaStatus.Error, observation.Status);
        Assert.Equal("invalid_used_percent", observation.ErrorCode);
    }

    [Fact]
    public void MillisecondShapedResetIsNotGuessedOrUsedAsUnixSeconds()
    {
        var observation = ParseFixture("codex-millisecond-reset.json").Windows.Single(w => w.WindowId == WindowId.FiveHour);

        Assert.Equal(60d, observation.RemainingPercent);
        Assert.Null(observation.ResetAtUtc);
        Assert.Equal("invalid_reset_timestamp", observation.ErrorCode);
    }

    [Fact]
    public void NonCodexBucketIsNotUsedAsCodexQuota()
    {
        const string json = """{"result":{"rateLimitsByLimitId":{"other":{"primary":{"usedPercent":1,"windowDurationMins":300}}}}}""";

        var snapshot = CodexRateLimitParser.ParseResponse(json, ObservedAt);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Fact]
    public void EmptyLegacyBucketIsReportedAsMissingQuotaInsteadOfUnsupportedSchema()
    {
        const string json = """{"rateLimits":{}}""";

        var snapshot = CodexRateLimitParser.ParseResponse(json, ObservedAt);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("codex_limit_bucket_missing", window.ErrorCode));
    }

    [Fact]
    public void UnknownDurationIsUnsupportedInsteadOfAssumingPrimaryIsFiveHours()
    {
        const string json = """{"result":{"rateLimits":{"limitId":"codex","primary":{"usedPercent":20,"windowDurationMins":1440}}}}""";

        var snapshot = CodexRateLimitParser.ParseResponse(json, ObservedAt);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unsupported, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Fact]
    public void DuplicateWindowsFailClosed()
    {
        const string json = """{"result":{"rateLimits":{"limitId":"codex","primary":{"usedPercent":20,"windowDurationMins":300},"secondary":{"usedPercent":40,"windowDurationMins":300}}}}""";

        var snapshot = CodexRateLimitParser.ParseResponse(json, ObservedAt);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Error, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("duplicate_window", window.ErrorCode));
    }

    [Fact]
    public void InvalidJsonReturnsSafeParseFailureState()
    {
        var snapshot = CodexRateLimitParser.ParseResponse("not json", ObservedAt);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Error, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Equal("parse_failure", window.ErrorCode));
    }

    private static ProviderSnapshot ParseFixture(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        return CodexRateLimitParser.ParseResponse(File.ReadAllText(path), ObservedAt);
    }
}
