using System.Drawing;
using System.Windows;
using AiQuotaMonitor.App.ViewModels;

namespace AiQuotaMonitor.App.Services;

public sealed class SystemTrayService : IDisposable
{
    private readonly System.Windows.Forms.NotifyIcon _icon;
    private readonly Action _showMainWindow;
    private readonly Action _toggleFloatingPanel;
    private readonly Action _refresh;
    private readonly Action _openSettings;
    private readonly Action _exit;

    public SystemTrayService(
        Action showMainWindow,
        Action toggleFloatingPanel,
        Action refresh,
        Action openSettings,
        Action exit)
    {
        _showMainWindow = showMainWindow;
        _toggleFloatingPanel = toggleFloatingPanel;
        _refresh = refresh;
        _openSettings = openSettings;
        _exit = exit;

        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("開啟主視窗", null, (_, _) => _showMainWindow());
        menu.Items.Add("顯示／隱藏精簡面板", null, (_, _) => _toggleFloatingPanel());
        menu.Items.Add("立即刷新", null, (_, _) => _refresh());
        menu.Items.Add("設定", null, (_, _) => _openSettings());
        menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
        menu.Items.Add("結束程式", null, (_, _) => _exit());

        _icon = new System.Windows.Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "AI 額度監控器",
            ContextMenuStrip = menu,
            Visible = true
        };
        _icon.DoubleClick += Icon_DoubleClick;
    }

    public void ShowLowQuotaAlert(QuotaAlert alert)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.HasShutdownStarted)
        {
            return;
        }

        _ = dispatcher.BeginInvoke(new Action(() =>
        {
            _icon.BalloonTipTitle = $"{alert.ProviderName} 額度偏低";
            _icon.BalloonTipText = $"{alert.WindowName}剩餘 {alert.RemainingPercent:0.#}%（提醒門檻 {alert.ThresholdPercent}%）";
            _icon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
            _icon.ShowBalloonTip(5000);
        }));
    }

    public void Dispose()
    {
        _icon.DoubleClick -= Icon_DoubleClick;
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }

    private void Icon_DoubleClick(object? sender, EventArgs e) => _showMainWindow();
}
