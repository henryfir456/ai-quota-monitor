using AiQuotaMonitor.App.ViewModels;
using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Tests;

public sealed class QuotaWindowViewModelTests
{
    [Fact]
    public void FailedRefreshKeepsLastSuccessfulTimestampAndDoesNotKeepQuotaNumber()
    {
        var successfulAt = DateTimeOffset.Parse("2026-10-21T00:00:00Z");
        var viewModel = new QuotaWindowViewModel(new QuotaObservation(
            ProviderId.Codex, WindowId.FiveHour, 65, successfulAt.AddHours(5), successfulAt,
            "Codex app-server", QuotaStatus.Fresh));

        viewModel.Update(new QuotaObservation(
            ProviderId.Codex, WindowId.FiveHour, null, null, successfulAt.AddMinutes(5),
            "Codex app-server", QuotaStatus.Unknown, "codex_not_logged_in"));

        Assert.Equal(successfulAt, viewModel.LastSuccessfulUpdateAtUtc);
        Assert.Contains("最後成功", viewModel.LastSuccessfulUpdateText, StringComparison.Ordinal);
        Assert.Equal("N/A", viewModel.RemainingText);
        Assert.Contains("Codex 尚未登入", viewModel.DiagnosticText, StringComparison.Ordinal);
        Assert.Equal("尚未登入", viewModel.StatusText);
    }

    [Fact]
    public void MissingInitialDataShowsSpecificReasonAndNoSuccessTimestamp()
    {
        var viewModel = new QuotaWindowViewModel(new QuotaObservation(
            ProviderId.Claude, WindowId.FiveHour, null, null, DateTimeOffset.UtcNow,
            "Claude Code official statusline", QuotaStatus.Unknown, "statusline_not_configured"));

        Assert.Null(viewModel.LastSuccessfulUpdateAtUtc);
        Assert.Contains("尚未設定 Claude Code statusline", viewModel.DiagnosticText, StringComparison.Ordinal);
        Assert.Contains("statusline_not_configured", viewModel.DiagnosticText, StringComparison.Ordinal);
    }
}
