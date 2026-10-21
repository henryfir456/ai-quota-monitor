using AiQuotaMonitor.App.Services;
using AiQuotaMonitor.App.ViewModels;

namespace AiQuotaMonitor.Tests;

public sealed class MonitoringPauseTests
{
    [Theory]
    [InlineData(false, RefreshTrigger.Manual, true)]
    [InlineData(false, RefreshTrigger.Scheduled, true)]
    [InlineData(false, RefreshTrigger.Lifecycle, true)]
    [InlineData(true, RefreshTrigger.Manual, true)]
    [InlineData(true, RefreshTrigger.Scheduled, false)]
    [InlineData(true, RefreshTrigger.Lifecycle, false)]
    public void PausedMonitoringAllowsOnlyManualRefresh(bool isPaused, RefreshTrigger trigger, bool expected)
    {
        Assert.Equal(expected, MonitoringRefreshPolicy.CanStart(isPaused, false, trigger));
    }

    [Fact]
    public void PausedMonitoringDoesNotQueueLifecycleRefreshOrScheduleNextTick()
    {
        Assert.False(MonitoringRefreshPolicy.CanQueueLifecycle(isPaused: true, isRefreshing: true));
        Assert.True(MonitoringRefreshPolicy.CanQueueLifecycle(isPaused: false, isRefreshing: true));
        Assert.False(MonitoringRefreshPolicy.CanScheduleNext(isStarted: true, isPaused: true, isDisposed: false));
        Assert.True(MonitoringRefreshPolicy.CanScheduleNext(isStarted: true, isPaused: false, isDisposed: false));
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void PausedMonitoringSuppressesAlerts(bool isPaused, bool alertsEnabled, bool expected) =>
        Assert.Equal(expected, MonitoringRefreshPolicy.CanSendAlert(isPaused, alertsEnabled));

    [Fact]
    public void PauseToggleUpdatesStatusAndResumesWithoutStartingAProviderInTheTest()
    {
        var path = Path.Combine(Path.GetTempPath(), "AiQuotaMonitor-tests", Guid.NewGuid().ToString("N"), "settings.json");
        using var viewModel = new MainViewModel(new SettingsStore(path));

        viewModel.ToggleMonitoringPause();
        Assert.True(viewModel.IsMonitoringPaused);
        Assert.Equal("恢復監控", viewModel.MonitoringPauseButtonText);
        Assert.Contains("自動刷新與提醒已暫停", viewModel.MonitoringStatusText);
        Assert.Contains("已暫停", viewModel.StatusMessage);

        viewModel.ToggleMonitoringPause();
        Assert.False(viewModel.IsMonitoringPaused);
        Assert.Equal("暫停監控", viewModel.MonitoringPauseButtonText);
        Assert.Contains("已恢復", viewModel.StatusMessage);
    }
}
