using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using AiQuotaMonitor.App.Services;
using AiQuotaMonitor.Core;
using AiQuotaMonitor.Providers;
using AiQuotaMonitor.Providers.Claude;
using AiQuotaMonitor.Providers.Codex;
using Microsoft.Win32;

namespace AiQuotaMonitor.App.ViewModels;

public sealed record QuotaAlert(string ProviderName, string WindowName, double RemainingPercent, int ThresholdPercent);

public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private const int ManualRefreshThrottleSeconds = 10;
    private static readonly TimeSpan MaximumRetryInterval = TimeSpan.FromMinutes(30);
    private readonly QuotaRefreshService _refreshService;
    private readonly ClaudeStatuslineProvider _claudeProvider;
    private readonly ClaudeStatuslinePipeHost _claudePipeHost;
    private readonly SettingsStore _settingsStore;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly DispatcherTimer _refreshTimer;
    private readonly DispatcherTimer _clockTimer;
    private readonly Dictionary<ProviderId, ProviderCardViewModel> _cardsByProvider;
    private readonly Dictionary<string, string> _lastAlertResetByKey = new(StringComparer.Ordinal);
    private AppSettings _settings;
    private bool _isRefreshing;
    private bool _started;
    private bool _isCollapsed;
    private int _consecutiveFailures;
    private bool _refreshAfterCurrent;
    private DateTimeOffset? _lastManualRefreshAt;
    private string _statusMessage = "啟動後立即以唯讀方式查詢；沒有來源資料時顯示 UNKNOWN/N/A。";

    public MainViewModel(SettingsStore? settingsStore = null)
    {
        _settingsStore = settingsStore ?? new SettingsStore();
        _settings = _settingsStore.Load();
        _claudeProvider = new ClaudeStatuslineProvider();
        _claudePipeHost = new ClaudeStatuslinePipeHost(_claudeProvider);
        _refreshService = new QuotaRefreshService(
        [
            _claudeProvider,
            new CodexQuotaProvider()
        ]);
        _claudeProvider.SnapshotUpdated += OnClaudeSnapshotUpdated;
        Providers = new ObservableCollection<ProviderCardViewModel>
        {
            new(ProviderId.Claude),
            new(ProviderId.Codex)
        };
        _cardsByProvider = Providers.ToDictionary(card => card.ProviderId);

        RefreshCommand = new RelayCommand(() => _ = RefreshAsync(isManual: true), () => CanManualRefresh);
        ToggleThemeCommand = new RelayCommand(ToggleTheme);
        ToggleUsageCommand = new RelayCommand(ToggleUsageDisplay);
        ToggleCollapseCommand = new RelayCommand(() => IsCollapsed = !IsCollapsed);
        OpenSettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke());
        OpenFloatingPanelCommand = new RelayCommand(() => FloatingPanelRequested?.Invoke());

        _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_settings.RefreshIntervalSeconds) };
        _refreshTimer.Tick += RefreshTimer_Tick;
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += ClockTimer_Tick;
        SystemEvents.PowerModeChanged += PowerModeChanged;
        NetworkChange.NetworkAvailabilityChanged += NetworkAvailabilityChanged;

        foreach (var card in Providers)
        {
            foreach (var window in card.Windows)
            {
                window.ShowUsedPercent = _settings.ShowUsedPercent;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action<QuotaAlert>? LowQuotaAlert;

    public event Action? SettingsRequested;

    public event Action? FloatingPanelRequested;

    public event Action<ThemeMode>? ThemeChanged;

    public ObservableCollection<ProviderCardViewModel> Providers { get; }

    public RelayCommand RefreshCommand { get; }

    public RelayCommand ToggleThemeCommand { get; }

    public RelayCommand ToggleUsageCommand { get; }

    public RelayCommand ToggleCollapseCommand { get; }

    public RelayCommand OpenSettingsCommand { get; }

    public RelayCommand OpenFloatingPanelCommand { get; }

    public bool IsRefreshing
    {
        get => _isRefreshing;
        private set
        {
            if (SetField(ref _isRefreshing, value))
            {
                RefreshCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanManualRefresh));
            }
        }
    }

    public bool CanManualRefresh => !IsRefreshing &&
        (_lastManualRefreshAt is not DateTimeOffset last || DateTimeOffset.UtcNow - last >= TimeSpan.FromSeconds(ManualRefreshThrottleSeconds));

    public bool IsCollapsed
    {
        get => _isCollapsed;
        set
        {
            if (SetField(ref _isCollapsed, value))
            {
                OnPropertyChanged(nameof(CollapseButtonText));
            }
        }
    }

    public string CollapseButtonText => IsCollapsed ? "展開卡片" : "收合卡片";

    public string UsageButtonText => _settings.ShowUsedPercent ? "顯示剩餘" : "顯示已用";

    public string ThemeButtonText => _settings.Theme == ThemeMode.Dark ? "淺色主題" : "深色主題";

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public AppSettings Settings => _settings;

    public async Task StartAsync()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _ = _claudePipeHost.RunAsync(_lifetime.Token);
        _clockTimer.Start();
        await RefreshAsync();
        ScheduleNextRefresh();
    }

    public async Task RefreshAsync(bool isManual = false)
    {
        if (IsRefreshing || _lifetime.IsCancellationRequested || (isManual && !CanManualRefresh))
        {
            return;
        }

        if (isManual)
        {
            _lastManualRefreshAt = DateTimeOffset.UtcNow;
            OnPropertyChanged(nameof(CanManualRefresh));
            OnPropertyChanged(nameof(RefreshCooldownText));
            RefreshCommand.RaiseCanExecuteChanged();
        }

        IsRefreshing = true;
        StatusMessage = "正在以唯讀方式查詢來源；不會執行登入或推論…";
        try
        {
            var snapshots = await _refreshService.RefreshAsync(_lifetime.Token);
            foreach (var snapshot in snapshots)
            {
                if (_cardsByProvider.TryGetValue(snapshot.ProviderId, out var card))
                {
                    card.Apply(snapshot, _settings.ShowUsedPercent);
                }

                CheckLowQuotaAlerts(snapshot);
            }

            var hasFailure = snapshots.SelectMany(snapshot => snapshot.Windows).Any(window => window.Status == QuotaStatus.Error);
            _consecutiveFailures = hasFailure ? Math.Min(_consecutiveFailures + 1, 6) : 0;
            StatusMessage = hasFailure
                ? $"部分來源查詢失敗；將於 {NextRefreshInterval.TotalMinutes:0} 分鐘後重試。"
                : $"最後查詢：{DateTimeOffset.Now:yyyy/MM/dd HH:mm:ss} · 下次約 {NextRefreshInterval.TotalMinutes:0} 分鐘後刷新";
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        finally
        {
            IsRefreshing = false;
            OnPropertyChanged(nameof(RefreshCooldownText));
            if (_refreshAfterCurrent && !_lifetime.IsCancellationRequested)
            {
                _refreshAfterCurrent = false;
                _ = RefreshAsync();
            }
            else
            {
                _refreshAfterCurrent = false;
                ScheduleNextRefresh();
            }
        }
    }

    public string RefreshCooldownText
    {
        get
        {
            if (_lastManualRefreshAt is not DateTimeOffset last)
            {
                return "立即刷新";
            }

            var remaining = TimeSpan.FromSeconds(ManualRefreshThrottleSeconds) - (DateTimeOffset.UtcNow - last);
            return remaining > TimeSpan.Zero ? $"重新整理需等待 {(int)Math.Ceiling(remaining.TotalSeconds)} 秒" : "立即刷新";
        }
    }

    public bool ApplySettings(AppSettings settings)
    {
        var normalized = settings.Normalize();
        var previous = _settings;
        try
        {
            _settingsStore.Save(normalized);
            if (previous.StartWithWindows != normalized.StartWithWindows)
            {
                StartAtLoginRegistrar.Apply(normalized.StartWithWindows);
            }
        }
        catch
        {
            try
            {
                _settingsStore.Save(previous);
            }
            catch
            {
            }

            StatusMessage = "設定無法儲存；請檢查使用者設定資料夾權限。";
            return false;
        }

        _settings = normalized;
        foreach (var card in Providers)
        {
            foreach (var window in card.Windows)
            {
                window.ShowUsedPercent = _settings.ShowUsedPercent;
            }
        }

        OnPropertyChanged(nameof(Settings));
        OnPropertyChanged(nameof(UsageButtonText));
        OnPropertyChanged(nameof(ThemeButtonText));
        ThemeChanged?.Invoke(_settings.Theme);
        ScheduleNextRefresh();
        StatusMessage = "設定已儲存。";
        return true;
    }

    public void SaveFloatingPosition(double left, double top)
    {
        ApplySettings(_settings with { FloatingLeft = left, FloatingTop = top });
    }

    public void TickClock()
    {
        foreach (var card in Providers)
        {
            foreach (var window in card.Windows)
            {
                window.Tick();
            }
        }

        OnPropertyChanged(nameof(RefreshCooldownText));
        if (CanManualRefresh)
        {
            RefreshCommand.RaiseCanExecuteChanged();
        }
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _clockTimer.Stop();
        _refreshTimer.Tick -= RefreshTimer_Tick;
        _clockTimer.Tick -= ClockTimer_Tick;
        _claudeProvider.SnapshotUpdated -= OnClaudeSnapshotUpdated;
        SystemEvents.PowerModeChanged -= PowerModeChanged;
        NetworkChange.NetworkAvailabilityChanged -= NetworkAvailabilityChanged;
        _lifetime.Cancel();
        _lifetime.Dispose();
    }

    private TimeSpan NextRefreshInterval
    {
        get
        {
            var baseInterval = TimeSpan.FromSeconds(_settings.RefreshIntervalSeconds);
            if (_consecutiveFailures == 0)
            {
                return baseInterval;
            }

            var seconds = Math.Min(baseInterval.TotalSeconds * Math.Pow(2, _consecutiveFailures), MaximumRetryInterval.TotalSeconds);
            return TimeSpan.FromSeconds(seconds);
        }
    }

    private void ScheduleNextRefresh()
    {
        if (!_started || _lifetime.IsCancellationRequested)
        {
            return;
        }

        _refreshTimer.Stop();
        _refreshTimer.Interval = NextRefreshInterval;
        _refreshTimer.Start();
    }

    private void CheckLowQuotaAlerts(ProviderSnapshot snapshot)
    {
        if (!_settings.LowAlertsEnabled)
        {
            return;
        }

        foreach (var observation in snapshot.Windows)
        {
            if (observation.Status != QuotaStatus.Fresh ||
                observation.IsDemo ||
                observation.RemainingPercent is not double remaining ||
                remaining > _settings.AlertThresholdPercent)
            {
                continue;
            }

            var key = $"{observation.ProviderId}:{observation.WindowId}:{_settings.AlertThresholdPercent}";
            var resetId = observation.ResetAtUtc?.ToUnixTimeSeconds().ToString() ?? "reset-unknown";
            if (_lastAlertResetByKey.TryGetValue(key, out var alertedReset) && alertedReset == resetId)
            {
                continue;
            }

            _lastAlertResetByKey[key] = resetId;
            var providerName = observation.ProviderId == ProviderId.Claude ? "Claude Code" : "Codex";
            var windowName = observation.WindowId == WindowId.FiveHour ? "5 小時額度" : "每週額度";
            LowQuotaAlert?.Invoke(new QuotaAlert(providerName, windowName, remaining, _settings.AlertThresholdPercent));
        }
    }

    private void ToggleTheme()
    {
        var next = _settings.Theme == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        ApplySettings(_settings with { Theme = next });
    }

    private void ToggleUsageDisplay() => ApplySettings(_settings with { ShowUsedPercent = !_settings.ShowUsedPercent });

    private void OnClaudeSnapshotUpdated(ProviderSnapshot snapshot)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
        {
            return;
        }

        _ = dispatcher.BeginInvoke(new Action(() =>
        {
            if (_cardsByProvider.TryGetValue(ProviderId.Claude, out var card))
            {
                card.Apply(snapshot, _settings.ShowUsedPercent);
            }

            CheckLowQuotaAlerts(snapshot);
        }));
    }

    private async void RefreshTimer_Tick(object? sender, EventArgs e) => await RefreshAsync();

    private void ClockTimer_Tick(object? sender, EventArgs e) => TickClock();

    private void PowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Resume)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.HasShutdownStarted)
            {
                _ = dispatcher.BeginInvoke(new Action(RequestLifecycleRefresh));
            }
        }
    }

    private void NetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        if (e.IsAvailable)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is not null && !dispatcher.HasShutdownStarted)
            {
                _ = dispatcher.BeginInvoke(new Action(RequestLifecycleRefresh));
            }
        }
    }

    private void RequestLifecycleRefresh()
    {
        if (_lifetime.IsCancellationRequested)
        {
            return;
        }

        if (IsRefreshing)
        {
            _refreshAfterCurrent = true;
            return;
        }

        _ = RefreshAsync();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
