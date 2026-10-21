using AiQuotaMonitor.App.ViewModels;
using AiQuotaMonitor.Core;

namespace AiQuotaMonitor.Tests;

public sealed class ProviderCardViewModelTests
{
    [Fact]
    public void RefreshUpdatesVisibleDiagnosticAndKeepsSuccessfulTimestamp()
    {
        var card = new ProviderCardViewModel(ProviderId.Codex);
        var successfulAt = DateTimeOffset.Parse("2026-10-21T00:00:00Z");
        var initial = new ProviderSnapshot(ProviderId.Codex,
        [
            new QuotaObservation(ProviderId.Codex, WindowId.FiveHour, 65, successfulAt.AddHours(5), successfulAt,
                "Codex app-server account/rateLimits/read", QuotaStatus.Fresh),
            new QuotaObservation(ProviderId.Codex, WindowId.Weekly, 38, successfulAt.AddDays(7), successfulAt,
                "Codex app-server account/rateLimits/read", QuotaStatus.Fresh)
        ]);
        card.Apply(initial, showUsedPercent: false);
        var diagnosticChanged = false;
        card.PropertyChanged += (_, eventArgs) => diagnosticChanged |= eventArgs.PropertyName == nameof(card.DiagnosticText);

        var failed = new ProviderSnapshot(ProviderId.Codex,
        [
            new QuotaObservation(ProviderId.Codex, WindowId.FiveHour, null, null, successfulAt.AddMinutes(5),
                "Codex app-server account/rateLimits/read", QuotaStatus.Unknown, "codex_not_logged_in"),
            new QuotaObservation(ProviderId.Codex, WindowId.Weekly, null, null, successfulAt.AddMinutes(5),
                "Codex app-server account/rateLimits/read", QuotaStatus.Unknown, "codex_not_logged_in")
        ]);

        card.Apply(failed, showUsedPercent: false);

        Assert.True(diagnosticChanged);
        Assert.Contains("Codex 尚未登入", card.DiagnosticText, StringComparison.Ordinal);
        Assert.Contains("codex_not_logged_in", card.DiagnosticText, StringComparison.Ordinal);
        Assert.Equal("最後成功：2026/10/21 08:00:00 Asia/Taipei", card.LastSuccessfulUpdateText);
        Assert.Equal("尚未登入", card.StatusText);
        Assert.All(card.Windows, window => Assert.Equal("N/A", window.RemainingText));
    }
}
