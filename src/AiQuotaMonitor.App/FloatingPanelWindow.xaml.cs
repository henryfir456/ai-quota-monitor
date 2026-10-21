using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AiQuotaMonitor.App.ViewModels;

namespace AiQuotaMonitor.App;

public partial class FloatingPanelWindow : Window
{
    private readonly MainViewModel _viewModel;

    public FloatingPanelWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        var left = _viewModel.Settings.FloatingLeft ?? workArea.Right - Width - 20;
        var top = _viewModel.Settings.FloatingTop ?? workArea.Top + 20;
        if (left + Width < SystemParameters.VirtualScreenLeft ||
            left > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth ||
            top + Height < SystemParameters.VirtualScreenTop ||
            top > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
        {
            left = workArea.Right - Width - 20;
            top = workArea.Top + 20;
        }

        Left = left;
        Top = top;
    }

    private void Window_Closing(object? sender, CancelEventArgs e) => _viewModel.SaveFloatingPosition(Left, Top);

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
            _viewModel.SaveFloatingPosition(Left, Top);
        }
    }

    private void ShowMainWindow_Click(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Application.Current.MainWindow is MainWindow mainWindow)
        {
            if (!mainWindow.IsVisible)
            {
                mainWindow.Show();
            }

            mainWindow.ShowInTaskbar = true;
            mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }
    }
}
