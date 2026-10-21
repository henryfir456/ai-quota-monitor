using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using AiQuotaMonitor.App.Services;
using AiQuotaMonitor.App.ViewModels;

namespace AiQuotaMonitor.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly SystemTrayService _trayService;
    private readonly bool _startMinimized;
    private readonly bool _smokeTest;
    private readonly string? _smokeReadyFile;
    private FloatingPanelWindow? _floatingPanel;
    private bool _allowExit;

    public MainWindow(bool startMinimized = false, bool smokeTest = false, string? smokeReadyFile = null, string? smokeSettingsPath = null)
    {
        InitializeComponent();
        _startMinimized = startMinimized;
        _smokeTest = smokeTest;
        _smokeReadyFile = smokeReadyFile;
        _viewModel = new MainViewModel(smokeSettingsPath is null ? null : new SettingsStore(smokeSettingsPath));
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

        if (_smokeTest)
        {
            await Dispatcher.InvokeAsync(WriteSmokeReadiness, DispatcherPriority.ApplicationIdle);
            return;
        }

        await _viewModel.StartAsync();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_allowExit && !_smokeTest)
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
        if (_smokeTest)
        {
            System.Windows.Application.Current.Shutdown();
        }
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

    private void WriteSmokeReadiness()
    {
        UpdateLayout();
        var controls = EnumerateVisualChildren(this).ToArray();
        var progressBars = controls.OfType<System.Windows.Controls.ProgressBar>().ToArray();
        var bindingModes = progressBars
            .Select(progressBar => BindingOperations.GetBinding(progressBar, RangeBase.ValueProperty)?.Mode.ToString())
            .ToArray();
        var providerNames = controls.OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .Where(text => text is "Claude Code" or "Codex")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(text => text, StringComparer.Ordinal)
            .ToArray();

        if (string.IsNullOrWhiteSpace(_smokeReadyFile) ||
            !IsVisible ||
            Title != "AI 額度監控器" ||
            providerNames.Length != 2 ||
            progressBars.Length != 4 ||
            bindingModes.Any(mode => mode != BindingMode.OneWay.ToString()))
        {
            throw new InvalidOperationException("Smoke UI did not initialize the expected provider cards and read-only quota progress bindings.");
        }

        var readiness = JsonSerializer.Serialize(new
        {
            Ready = true,
            ProcessId = Environment.ProcessId,
            WindowTitle = Title,
            MainWindowVisible = IsVisible,
            ProviderCardCount = providerNames.Length,
            ProviderNames = providerNames,
            QuotaProgressBarCount = progressBars.Length,
            ProgressValueBindingModes = bindingModes,
            CheckedAtUtc = DateTimeOffset.UtcNow
        });
        var temporaryPath = _smokeReadyFile + ".tmp";
        File.WriteAllText(temporaryPath, readiness);
        File.Move(temporaryPath, _smokeReadyFile, overwrite: true);
    }

    private static IEnumerable<DependencyObject> EnumerateVisualChildren(DependencyObject parent)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var index = 0; index < childCount; index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            yield return child;
            foreach (var descendant in EnumerateVisualChildren(child))
            {
                yield return descendant;
            }
        }
    }
}
