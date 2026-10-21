using AiQuotaMonitor.Core;
using AiQuotaMonitor.Providers.Claude;

namespace AiQuotaMonitor.Tests;

public sealed class ClaudeStatuslineParserTests
{
    [Fact]
    public void ParsesOnlyOfficialRateLimitFieldsAndMapsSevenDayToWeekly()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "claude-statusline-usage.json");
        var snapshot = ClaudeStatuslineParser.Parse(File.ReadAllText(path), DateTimeOffset.UtcNow);

        var fiveHour = snapshot.Windows.Single(window => window.WindowId == WindowId.FiveHour);
        var weekly = snapshot.Windows.Single(window => window.WindowId == WindowId.Weekly);
        Assert.Equal(76.5d, fiveHour.RemainingPercent);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_800_000_000), fiveHour.ResetAtUtc);
        Assert.Equal(58.8d, weekly.RemainingPercent);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_800_600_000), weekly.ResetAtUtc);
    }

    [Fact]
    public void MissingRateLimitsRemainUnknown()
    {
        var snapshot = ClaudeStatuslineParser.Parse("{}", DateTimeOffset.UtcNow);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }

    [Theory]
    [InlineData(0d, 100d)]
    [InlineData(100d, 0d)]
    public void ZeroAndFullUsedValuesMapToTheOppositeRemainingEdge(double used, double remaining)
    {
        var json = $$"""{"rate_limits":{"five_hour":{"used_percentage":{{used}}}}}""";

        var observation = ClaudeStatuslineParser.Parse(json, DateTimeOffset.UtcNow)
            .Windows.Single(window => window.WindowId == WindowId.FiveHour);

        Assert.Equal(remaining, observation.RemainingPercent);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.1)]
    public void OutOfRangeUsedPercentageDoesNotProduceRemainingValue(double used)
    {
        var json = $$"""{"rate_limits":{"five_hour":{"used_percentage":{{used}}}}}""";

        var observation = ClaudeStatuslineParser.Parse(json, DateTimeOffset.UtcNow)
            .Windows.Single(window => window.WindowId == WindowId.FiveHour);

        Assert.Null(observation.RemainingPercent);
        Assert.Equal(QuotaStatus.Error, observation.Status);
    }

    [Fact]
    public void InvalidResetDoesNotInvalidateAValidPercentageOrGuessUnits()
    {
        const string json = """{"rate_limits":{"five_hour":{"used_percentage":23,"resets_at":"2026-10-21T10:00:00Z"}}}""";

        var observation = ClaudeStatuslineParser.Parse(json, DateTimeOffset.UtcNow)
            .Windows.Single(window => window.WindowId == WindowId.FiveHour);

        Assert.Equal(77d, observation.RemainingPercent);
        Assert.Null(observation.ResetAtUtc);
        Assert.Equal("invalid_reset_timestamp", observation.ErrorCode);
    }

    [Fact]
    public async Task UnconfiguredOrOldStatuslineDataIsUnknownOrStale()
    {
        var provider = new ClaudeStatuslineProvider();
        var initial = await provider.GetSnapshotAsync(CancellationToken.None);
        Assert.All(initial.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));

        provider.Accept("""{"rate_limits":{"five_hour":{"used_percentage":23},"seven_day":{"used_percentage":41}}}""",
            DateTimeOffset.UtcNow.AddMinutes(-16));
        var stale = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(stale.Windows, window => Assert.Equal(QuotaStatus.Stale, window.Status));
        Assert.Equal(77d, stale.Windows.Single(window => window.WindowId == WindowId.FiveHour).RemainingPercent);
    }

    [Fact]
    public async Task MissingRateLimitsReplacePreviousFreshSnapshotImmediately()
    {
        var provider = new ClaudeStatuslineProvider();
        provider.Accept("""{"rate_limits":{"five_hour":{"used_percentage":23},"seven_day":{"used_percentage":41}}}""");

        provider.Accept("""{"rate_limits":{}}""");
        var snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.All(snapshot.Windows, window => Assert.Equal(QuotaStatus.Unknown, window.Status));
        Assert.All(snapshot.Windows, window => Assert.Null(window.RemainingPercent));
    }
}
