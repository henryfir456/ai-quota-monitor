using AiQuotaMonitor.App.Services;
using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Tests;

public sealed class ProviderDiagnosticFormatterTests
{
    [Theory]
    [InlineData(ProviderId.Claude, "statusline_not_configured", "尚未設定 Claude Code statusline")]
    [InlineData(ProviderId.Claude, "claude_cli_not_found", "找不到 Claude Code CLI")]
    [InlineData(ProviderId.Claude, "statusline_rate_limits_missing", "Claude Code 未回報額度欄位")]
    [InlineData(ProviderId.Claude, "timeout", "statusline 已設定，但 15 分鐘內沒有收到資料")]
    [InlineData(ProviderId.Claude, "helper_unavailable", "Claude statusline 指向的 Monitor helper 不存在")]
    [InlineData(ProviderId.Claude, "parse_failure", "額度資料解析失敗")]
    [InlineData(ProviderId.Codex, "codex_cli_not_found", "找不到 Codex CLI")]
    [InlineData(ProviderId.Codex, "codex_not_logged_in", "Codex 尚未登入")]
    [InlineData(ProviderId.Codex, "timeout", "Codex App Server 查詢逾時")]
    [InlineData(ProviderId.Codex, "rpc_method_not_supported", "目前 Codex 版本不支援額度方法")]
    public void DescribesConcreteProviderCauseAndNextAction(ProviderId providerId, string errorCode, string expectedReason)
    {
        var observation = new QuotaObservation(
            providerId,
            WindowId.FiveHour,
            null,
            null,
            DateTimeOffset.Parse("2026-10-21T00:00:00Z"),
            "synthetic source",
            errorCode == "timeout" ? QuotaStatus.Error : QuotaStatus.Unknown,
            errorCode);

        var diagnostic = ProviderDiagnosticFormatter.Describe(observation);

        Assert.Contains(expectedReason, diagnostic.Reason, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(diagnostic.Recommendation));
        Assert.Contains(errorCode, diagnostic.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ParseFailureAndStaleDataHaveDifferentStatuses()
    {
        var observedAt = DateTimeOffset.UtcNow;
        var parseFailure = ProviderDiagnosticFormatter.Describe(new QuotaObservation(
            ProviderId.Codex, WindowId.Weekly, null, null, observedAt, "source", QuotaStatus.Error, "parse_failure"));
        var stale = ProviderDiagnosticFormatter.Describe(new QuotaObservation(
            ProviderId.Claude, WindowId.Weekly, 72, null, observedAt.AddMinutes(-20), "source", QuotaStatus.Stale, "stale_data"));

        Assert.Equal("資料解析失敗", parseFailure.Status);
        Assert.Equal("資料已過期", stale.Status);
        Assert.NotEqual(parseFailure.Reason, stale.Reason);
    }

    [Theory]
    [InlineData(ProviderId.Claude, "statusline_not_configured", "尚未設定")]
    [InlineData(ProviderId.Claude, "claude_cli_not_found", "CLI 未安裝")]
    [InlineData(ProviderId.Codex, "codex_not_logged_in", "尚未登入")]
    [InlineData(ProviderId.Codex, "app_server_unreachable", "無法連線")]
    public void ShowsConcreteStateAlongsideTheDiagnostic(ProviderId providerId, string errorCode, string expectedStatus)
    {
        var status = ProviderDiagnosticFormatter.Describe(new QuotaObservation(
            providerId, WindowId.FiveHour, null, null, DateTimeOffset.UtcNow,
            "synthetic source", errorCode == "app_server_unreachable" ? QuotaStatus.Error : QuotaStatus.Unknown,
            errorCode)).Status;

        Assert.Equal(expectedStatus, status);
    }
}
