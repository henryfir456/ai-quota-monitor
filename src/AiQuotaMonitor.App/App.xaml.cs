using System.Threading;
using System.IO;
using System.Text.Json;
using System.Windows.Markup;
using System.Windows;
using System.Windows.Threading;

namespace AiQuotaMonitor.App;

public partial class App : System.Windows.Application
{
    private const string MutexName = "Local\\AiQuotaMonitor.v0.1";
    private const string FocusEventName = "Local\\AiQuotaMonitor.Focus.v0.1";
    private Mutex? _instanceMutex;
    private EventWaitHandle? _focusEvent;
    private RegisteredWaitHandle? _focusWait;
    private bool _ownsMutex;
    private bool _allowInjectedSmokeFailureExit;
    private string? _smokeExceptionFile;

    protected override void OnStartup(StartupEventArgs e)
    {
        var smokeTest = HasArgument(e.Args, "--smoke-test");
        var forceSmokeFailure = HasArgument(e.Args, "--smoke-test-force-failure");
        var readyFile = GetArgumentValue(e.Args, "--smoke-ready-file");
        var exceptionFile = GetArgumentValue(e.Args, "--smoke-exception-file");
        var settingsFile = GetArgumentValue(e.Args, "--smoke-settings-file");
        if (smokeTest)
        {
            if (string.IsNullOrWhiteSpace(readyFile) || string.IsNullOrWhiteSpace(exceptionFile) || string.IsNullOrWhiteSpace(settingsFile))
            {
                Environment.ExitCode = 64;
                Shutdown(64);
                return;
            }

            _smokeExceptionFile = exceptionFile;
            _allowInjectedSmokeFailureExit = forceSmokeFailure;
            DispatcherUnhandledException += SmokeTestDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += SmokeTestDomainUnhandledException;
        }

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
        var mainWindow = new MainWindow(
            startMinimized,
            smokeTest,
            readyFile,
            smokeTest ? settingsFile : null);
        MainWindow = mainWindow;
        mainWindow.Show();
        if (smokeTest && forceSmokeFailure)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                throw new XamlParseException("Injected startup failure for the Windows release-gate negative control.")));
        }

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

    private static bool HasArgument(IEnumerable<string> arguments, string name) =>
        arguments.Any(argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));

    private static string? GetArgumentValue(IReadOnlyList<string> arguments, string name)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return arguments[index + 1];
            }
        }

        return null;
    }

    private void SmokeTestDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteSmokeException(e.Exception);
        Environment.ExitCode = 70;
        if (_allowInjectedSmokeFailureExit && e.Exception is XamlParseException)
        {
            e.Handled = true;
            Shutdown(70);
        }
    }

    private void SmokeTestDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            WriteSmokeException(exception);
        }

        Environment.ExitCode = 71;
    }

    private void WriteSmokeException(Exception exception)
    {
        if (string.IsNullOrWhiteSpace(_smokeExceptionFile))
        {
            return;
        }

        try
        {
            var sanitized = JsonSerializer.Serialize(new
            {
                ExceptionType = exception.GetType().FullName,
                ProcessId = Environment.ProcessId,
                ObservedAtUtc = DateTimeOffset.UtcNow
            });
            File.WriteAllText(_smokeExceptionFile, sanitized);
        }
        catch
        {
            // Preserve the original exception and nonzero smoke exit even if evidence storage fails.
        }
    }
}
