using System.Globalization;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Specht.Core.Services;
using Windows.Globalization;
using WinRT;

namespace Specht.App;

/// <summary>
/// Custom entry point that enforces single-instance behaviour via AppInstance.
/// A second launch redirects the activation to the running instance and exits.
/// </summary>
public static class Program
{
    private const string InstanceKey = "specht-mdns-spotter-singleton-v1";

    [STAThread]
    private static int Main(string[] args)
    {
        ComWrappersSupport.InitializeComWrappers();

        ApplyLanguageOverride();

        if (DecideRedirection())
        {
            // Another instance is running; we already redirected activation. Exit.
            return 0;
        }

        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(
                DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
        return 0;
    }

    private static void ApplyLanguageOverride()
    {
        try
        {
            var settings = new SettingsService();
            var lang = settings.Current.Language switch
            {
                "de" => "de-DE",
                "en" => "en-US",
                _    => string.Empty,
            };
            if (string.IsNullOrEmpty(lang)) return;

            // Unpackaged WinUI 3 has notoriously brittle language switching. We hit
            // it from every angle we know — each call is best-effort, none of them
            // is documented to be authoritative for XAML x:Uid in 2026.
            try { ApplicationLanguages.PrimaryLanguageOverride = lang; } catch { }
            try
            {
                var ci = new CultureInfo(lang);
                CultureInfo.DefaultThreadCurrentCulture = ci;
                CultureInfo.DefaultThreadCurrentUICulture = ci;
                CultureInfo.CurrentCulture = ci;
                CultureInfo.CurrentUICulture = ci;
            }
            catch { }
            try
            {
                Windows.ApplicationModel.Resources.Core.ResourceManager.Current
                    .DefaultContext.Languages = new List<string> { lang };
            }
            catch { }
        }
        catch
        {
            // ignore; fall back to system language
        }
    }

    private static bool DecideRedirection()
    {
        var thisInstance = AppInstance.GetCurrent();
        var keyInstance = AppInstance.FindOrRegisterForKey(InstanceKey);

        if (keyInstance.IsCurrent) return false;

        try
        {
            var activationArgs = thisInstance.GetActivatedEventArgs();
            keyInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
        }
        catch
        {
            // Even if redirect fails, don't start a second instance.
        }
        return true;
    }
}
