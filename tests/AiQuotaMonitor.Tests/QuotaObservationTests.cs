using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Tests;

public sealed class QuotaObservationTests
{
    [Theory]
    [InlineData(0, 100)]
    [InlineData(42, 58)]
    [InlineData(100, 0)]
    public void UsedPercentIsDerivedOnlyFromARealRemainingValue(double remaining, double expectedUsed)
    {
        var observation = Create(remaining);

        Assert.Equal(expectedUsed, observation.UsedPercent);
    }

    [Fact]
    public void MissingRemainingPercentRemainsUnknown()
    {
        var observation = Create(null);

        Assert.Null(observation.RemainingPercent);
        Assert.Null(observation.UsedPercent);
    }

    [Fact]
    public void NonFiniteRemainingPercentIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(double.NaN));
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(double.PositiveInfinity));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void RemainingPercentOutsideSupportedRangeIsRejected(double remaining)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(remaining));
    }

    private static QuotaObservation Create(double? remaining) => new(
        ProviderId.Codex,
        WindowId.FiveHour,
        remaining,
        resetAtUtc: null,
        observedAtUtc: DateTimeOffset.Parse("2026-10-21T00:00:00+00:00"),
        source: "synthetic-fixture",
        status: QuotaStatus.Fresh);
}
