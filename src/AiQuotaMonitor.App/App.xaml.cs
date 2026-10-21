using System.Threading;
using System.Windows;

namespace AiQuotaMonitor.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Local\\AiQuotaMonitor.v0.1";
    private const string FocusEventName = "Local\\AiQuotaMonitor.Focus.v0.1";
    private Mutex? _instanceMutex;
    private EventWaitHandle? _focusEvent;
    private RegisteredWaitHandle? _focusWait;
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        _ownsMutex = createdNew;
        if (!createdNew)
        {
            try
            {
                using var existingEvent = EventWaitHandle.OpenExisting(FocusEventName);
                existingEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
            }

            Shutdown();
            return;
        }

        _focusEvent = new EventWaitHandle(false, EventResetMode.AutoReset, FocusEventName);
        base.OnStartup(e);
        var startMinimized = e.Args.Any(argument => string.Equals(argument, "--minimized", StringComparison.OrdinalIgnoreCase));
        var mainWindow = new MainWindow(startMinimized);
        MainWindow = mainWindow;
        mainWindow.Show();
        _focusWait = ThreadPool.RegisterWaitForSingleObject(
            _focusEvent,
            (state, timedOut) => { _ = Dispatcher.BeginInvoke(new Action(FocusMainWindow)); },
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _focusWait?.Unregister(null);
        _focusEvent?.Dispose();
        if (_ownsMutex)
        {
            try
            {
                _instanceMutex?.ReleaseMutex();
            }
            catch (ApplicationException)
            {
            }
        }

        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    public static void ApplyTheme(Services.ThemeMode theme)
    {
        var current = System.Windows.Application.Current;
        if (current is null)
        {
            return;
        }

        var source = new Uri(theme == Services.ThemeMode.Dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        var replacement = new ResourceDictionary { Source = source };
        var dictionaries = current.Resources.MergedDictionaries;
        if (dictionaries.Count == 0)
        {
            dictionaries.Add(replacement);
        }
        else
        {
            dictionaries[0] = replacement;
        }
    }

    private void FocusMainWindow()
    {
        if (MainWindow is not AiQuotaMonitor.App.MainWindow window)
        {
            return;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }
}
