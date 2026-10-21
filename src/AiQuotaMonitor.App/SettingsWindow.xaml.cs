using System.Windows;
using AiQuotaMonitor.App.Services;

namespace AiQuotaMonitor.App;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        _settings = settings.Normalize();
        RefreshSecondsBox.Text = _settings.RefreshIntervalSeconds.ToString();
        LowAlertsCheckBox.IsChecked = _settings.LowAlertsEnabled;
        AlertThresholdBox.ItemsSource = new[] { 10, 20 };
        AlertThresholdBox.SelectedItem = _settings.AlertThresholdPercent;
        StartWithWindowsCheckBox.IsChecked = _settings.StartWithWindows;
    }

    public AppSettings UpdatedSettings { get; private set; } = new();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(RefreshSecondsBox.Text, out var refreshSeconds) || refreshSeconds is < 60 or > 1800)
        {
            System.Windows.MessageBox.Show(this, "刷新間隔必須是 60 到 1800 秒。", "設定錯誤", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            RefreshSecondsBox.Focus();
            return;
        }

        UpdatedSettings = _settings with
        {
            RefreshIntervalSeconds = refreshSeconds,
            LowAlertsEnabled = LowAlertsCheckBox.IsChecked == true,
            AlertThresholdPercent = AlertThresholdBox.SelectedItem is int threshold ? threshold : 20,
            StartWithWindows = StartWithWindowsCheckBox.IsChecked == true
        };
        DialogResult = true;
    }
}
