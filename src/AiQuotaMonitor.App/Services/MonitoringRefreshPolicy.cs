namespace AiQuotaMonitor.App.Services;

public enum RefreshTrigger
{
    Manual,
    Scheduled,
    Lifecycle
}

public static class MonitoringRefreshPolicy
{
    public static bool CanStart(bool isPaused, bool isRefreshing, RefreshTrigger trigger) =>
        !isRefreshing && (!isPaused || trigger == RefreshTrigger.Manual);

    public static bool CanQueueLifecycle(bool isPaused, bool isRefreshing) =>
        !isPaused && isRefreshing;

    public static bool CanSendAlert(bool isPaused, bool alertsEnabled) =>
        !isPaused && alertsEnabled;

    public static bool CanScheduleNext(bool isStarted, bool isPaused, bool isDisposed) =>
        isStarted && !isPaused && !isDisposed;
}
