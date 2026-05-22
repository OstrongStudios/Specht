using System.IO;
using H.NotifyIcon;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using Specht.App.Services;
using Specht.App.ViewModels;
using Specht.Core.Services;

namespace Specht.App;

public partial class App : Application
{
    public static DeviceCache Cache { get; } = new();
    public static DiscoveryService Discovery { get; } = new(Cache);
    public static SettingsService Settings { get; } = new();
    public static MainViewModel ViewModel { get; } = new(Cache, Discovery);
    public static IntPtr MainWindowHwnd { get; private set; } = IntPtr.Zero;

    private MainWindow? _mainWindow;
    private TaskbarIcon? _trayIcon;
    private ToastService? _toasts;
    private PowerWatchdog? _watchdog;
    private DispatcherQueue? _uiDispatcher;
    private bool _exiting;

    public App()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"Unhandled: {e.Exception}");
            e.Handled = true;
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _uiDispatcher = DispatcherQueue.GetForCurrentThread();

        AppInstance.GetCurrent().Activated += OnAppActivated;

        // Persistent main window — created once, hidden/shown on demand.
        // Hide-instead-of-close avoids the WinUI 3 last-window auto-shutdown.
        _mainWindow = new MainWindow();
        _mainWindow.HideRequested += (_, _) => HideMainWindow();
        MainWindowHwnd = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
        _mainWindow.Activate();        // initialise then immediately hide
        _mainWindow.AppWindow.Hide();

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = Strings.Get("AppTooltip"),
            NoLeftClickDelay = true,
            ContextMenuMode = ContextMenuMode.PopupMenu,
        };

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            try { _trayIcon.IconSource = new BitmapImage(new Uri(iconPath)); }
            catch { /* ignore icon errors */ }
        }

        // Single-click and double-click both toggle. Two toggles in quick
        // succession (a double-click) return to the original visibility state.
        var toggleCmd = new RelayCommand(ToggleMainWindow);
        _trayIcon.LeftClickCommand = toggleCmd;
        _trayIcon.DoubleClickCommand = toggleCmd;

        BuildTrayContextMenu();

        _trayIcon.ForceCreate();

        _toasts = new ToastService(Cache, Settings);
        _toasts.Start();

        Discovery.Start();

        _watchdog = new PowerWatchdog(Discovery);
        _watchdog.Start();

        AutostartService.SetEnabled(Settings.Current.Autostart);
    }

    private void BuildTrayContextMenu()
    {
        // In PopupMenu (native Win32) mode XAML Click events do NOT fire.
        // Each item must carry a Command instead.
        var menu = new MenuFlyout();
        menu.Items.Add(MakeItem(Strings.Get("TrayOpen"), ShowMainWindow));
        menu.Items.Add(MakeItem(Strings.Get("TrayRefresh"), () => Discovery.Refresh()));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MakeItem(Strings.Get("TraySettings"), () => NavigateExtra(typeof(Pages.SettingsPage))));
        menu.Items.Add(MakeItem(Strings.Get("TrayAbout"), () => NavigateExtra(typeof(Pages.AboutPage))));
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(MakeItem(Strings.Get("TrayExit"), RequestExit));
        if (_trayIcon is not null) _trayIcon.ContextFlyout = menu;
    }

    private void OnAppActivated(object? sender, AppActivationArguments e)
    {
        _uiDispatcher?.TryEnqueue(ShowMainWindow);
    }

    public void ToggleMainWindow()
    {
        if (_mainWindow is null) return;
        if (_mainWindow.AppWindow.IsVisible) HideMainWindow();
        else ShowMainWindow();
    }

    public void ShowMainWindow()
    {
        if (_mainWindow is null) return;
        try
        {
            _mainWindow.RepositionAtTray();
            _mainWindow.AppWindow.Show(activateWindow: true);
            _mainWindow.Activate();
            _mainWindow.RefreshBackdrop();
        }
        catch { /* ignore */ }
    }

    public void HideMainWindow()
    {
        if (_mainWindow is null) return;
        try { _mainWindow.AppWindow.Hide(); } catch { /* ignore */ }
    }

    private void NavigateExtra(Type pageType)
    {
        ShowMainWindow();
        _mainWindow?.NavigateTo(pageType);
    }

    public void RequestExit()
    {
        if (_exiting) return;
        _exiting = true;
        try { _watchdog?.Dispose(); } catch { }
        try { _toasts?.Dispose(); } catch { }
        try { Discovery.Stop(); } catch { }
        try { _trayIcon?.Dispose(); } catch { }
        try { _mainWindow?.Close(); } catch { }
        Exit();
    }

    /// <summary>
    /// Schedules a relaunch of the current executable after a short delay, then exits.
    /// The delay lets the AppInstance single-instance key from this process be released
    /// before the new instance registers; otherwise the new launch would redirect into
    /// our dying instance.
    /// </summary>
    public void RestartApp()
    {
        var exe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exe)) return;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                // 3-second delay then launch (start "" detaches it from the cmd shell)
                Arguments = $"/c timeout /t 3 /nobreak >nul & start \"\" \"{exe}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
            };
            System.Diagnostics.Process.Start(psi);
        }
        catch
        {
            // ignore; user can manually relaunch
        }
        RequestExit();
    }

    private static MenuFlyoutItem MakeItem(string label, Action action) =>
        new() { Text = label, Command = new RelayCommand(action) };

    private sealed class RelayCommand(Action execute) : System.Windows.Input.ICommand
    {
        private readonly Action _execute = execute;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
    }
}
