using System.ComponentModel;
using System.Windows;
using AiQuotaMonitor.App.Services;
using AiQuotaMonitor.App.ViewModels;

namespace AiQuotaMonitor.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SystemTrayService _trayService;
    private readonly bool _startMinimized;
    private FloatingPanelWindow? _floatingPanel;
    private bool _allowExit;

    public MainWindow(bool startMinimized = false)
    {
        InitializeComponent();
        _startMinimized = startMinimized;
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.LowQuotaAlert += _trayServiceAlert;
        _viewModel.SettingsRequested += OpenSettings;
        _viewModel.FloatingPanelRequested += ToggleFloatingPanel;
        _viewModel.MonitoringPauseChanged += MonitoringPauseChanged;
        _viewModel.ThemeChanged += App.ApplyTheme;
        App.ApplyTheme(_viewModel.Settings.Theme);

        _trayService = new SystemTrayService(
            showMainWindow: ShowMainWindow,
            toggleMainWindowVisibility: ToggleMainWindowVisibility,
            isMainWindowVisible: () => IsVisible,
            toggleFloatingPanel: ToggleFloatingPanel,
            toggleMonitoringPause: () => Dispatch(_viewModel.ToggleMonitoringPause),
            refresh: () => Dispatch(() => _ = _viewModel.RefreshAsync(isManual: true)),
            openSettings: () => Dispatch(OpenSettings),
            exit: () => Dispatch(ExitApplication));
        _trayService.SetMonitoringPaused(_viewModel.IsMonitoringPaused);
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (_startMinimized)
        {
            HideMainWindow();
        }

        await _viewModel.StartAsync();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowExit)
        {
            e.Cancel = true;
            HideMainWindow();
            return;
        }

        _viewModel.LowQuotaAlert -= _trayServiceAlert;
        _viewModel.SettingsRequested -= OpenSettings;
        _viewModel.FloatingPanelRequested -= ToggleFloatingPanel;
        _viewModel.MonitoringPauseChanged -= MonitoringPauseChanged;
        _viewModel.ThemeChanged -= App.ApplyTheme;
        _floatingPanel?.Close();
        _trayService.Dispose();
        _viewModel.Dispose();
    }

    private void ShowMainWindow()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatch(ShowMainWindow);
            return;
        }

        if (!IsVisible)
        {
            Show();
        }

        ShowInTaskbar = true;
        WindowState = WindowState.Normal;
        Activate();
    }

    private void HideMainWindow()
    {
        ShowInTaskbar = false;
        Hide();
    }

    private void ToggleMainWindowVisibility()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatch(ToggleMainWindowVisibility);
            return;
        }

        if (IsVisible)
        {
            HideMainWindow();
        }
        else
        {
            ShowMainWindow();
        }
    }

    private void ToggleFloatingPanel()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatch(ToggleFloatingPanel);
            return;
        }

        if (_floatingPanel is { IsVisible: true })
        {
            _floatingPanel.Hide();
            return;
        }

        if (_floatingPanel is null)
        {
            _floatingPanel = new FloatingPanelWindow(_viewModel);
            _floatingPanel.Closed += (_, _) => _floatingPanel = null;
        }

        _floatingPanel.Show();
        _floatingPanel.Activate();
    }

    private void OpenSettings()
    {
        ShowMainWindow();
        var dialog = new SettingsWindow(_viewModel.Settings) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            _viewModel.ApplySettings(dialog.UpdatedSettings);
        }
    }

    private void ExitApplication()
    {
        _allowExit = true;
        Close();
        System.Windows.Application.Current.Shutdown();
    }

    private void _trayServiceAlert(QuotaAlert alert) => _trayService.ShowLowQuotaAlert(alert);

    private void MonitoringPauseChanged(bool isPaused) => _trayService.SetMonitoringPaused(isPaused);

    private void Dispatch(Action action) => _ = Dispatcher.BeginInvoke(action);
}
