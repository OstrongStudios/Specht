using System.IO;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Specht.App.Pages;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;

namespace Specht.App;

public sealed partial class MainWindow : Window
{
    private const int DropdownWidth = 420;
    private const int DropdownHeight = 640;
    private const int EdgeMargin = 12;

    /// <summary>Raised when the window should be hidden (deactivation).</summary>
    public event EventHandler? HideRequested;

    public MainWindow()
    {
        InitializeComponent();
        ConfigureWindow();
        ApplyTheme();
        ApplyLanguage();
        App.Settings.Changed += OnSettingsChanged;
        Activated += OnActivated;
        Closed += (_, _) => App.Settings.Changed -= OnSettingsChanged;
        RootFrame.Navigate(typeof(MainPage));
        RootFrame.KeyDown += OnRootKeyDown;
    }

    private void OnRootKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != VirtualKey.Escape) return;
        if (RootFrame.CanGoBack)
        {
            RootFrame.GoBack();
            e.Handled = true;
        }
        else
        {
            ((App)Microsoft.UI.Xaml.Application.Current).HideMainWindow();
            e.Handled = true;
        }
    }

    private void ApplyLanguage()
    {
        var pref = App.Settings.Current.Language;
        var lang = pref switch
        {
            "de" => "de-DE",
            "en" => "en-US",
            _    => string.Empty,
        };
        if (string.IsNullOrEmpty(lang)) return;
        try { RootFrame.Language = lang; } catch { /* ignore */ }
    }

    public void NavigateTo(Type pageType) => RootFrame.Navigate(pageType);

    /// <summary>
    /// Forces a re-attachment of the SystemBackdrop. Workaround for a WinUI 3
    /// quirk where Mica loses its compositor binding after AppWindow.Hide()/Show().
    /// Visible symptom: depending on the activation path (tray click vs flyout)
    /// the window renders gray instead of the proper Mica blend.
    /// </summary>
    public void RefreshBackdrop()
    {
        try
        {
            var backdrop = SystemBackdrop;
            if (backdrop is null) return;
            SystemBackdrop = null;
            SystemBackdrop = backdrop;
        }
        catch { /* ignore */ }
    }

    /// <summary>Re-position the window over the tray each time we show it.</summary>
    public void RepositionAtTray()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        var workArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest).WorkArea;
        var scale = GetDpiScale(hwnd);
        var widthPx = (int)(DropdownWidth * scale);
        var heightPx = (int)(DropdownHeight * scale);
        var marginPx = (int)(EdgeMargin * scale);

        var x = workArea.X + workArea.Width - widthPx - marginPx;
        var y = workArea.Y + workArea.Height - heightPx - marginPx;

        appWindow.MoveAndResize(new RectInt32(x, y, widthPx, heightPx));
    }

    private void ConfigureWindow()
    {
        Title = "Specht";

        var hwnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        appWindow.Title = "Specht";

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            try { appWindow.SetIcon(iconPath); } catch { /* ignore icon errors */ }
        }

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        RepositionAtTray();
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        // Hide on focus loss instead of closing — closing would terminate the app.
        if (args.WindowActivationState == WindowActivationState.Deactivated)
        {
            HideRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnSettingsChanged(object? sender, Specht.Core.Services.AppSettings settings)
        => DispatcherQueue.TryEnqueue(ApplyTheme);

    private void ApplyTheme()
    {
        var theme = App.Settings.Current.Theme;
        if (RootFrame is FrameworkElement root)
        {
            root.RequestedTheme = theme switch
            {
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default,
            };
        }
    }

    private static double GetDpiScale(IntPtr hwnd)
    {
        var dpi = NativeMethods.GetDpiForWindow(hwnd);
        return dpi == 0 ? 1.0 : dpi / 96.0;
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int GetDpiForWindow(IntPtr hwnd);
    }
}
