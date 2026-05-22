using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using Specht.Core.Services;

namespace Specht.App.Services;

public sealed class ToastService : IDisposable
{
    private readonly DeviceCache _cache;
    private readonly ISettingsService _settings;
    private readonly HashSet<string> _announced = new(StringComparer.OrdinalIgnoreCase);
    private readonly DateTime _startupGrace = DateTime.UtcNow.AddSeconds(3); // suppress initial discovery burst
    private bool _registered;

    public ToastService(DeviceCache cache, ISettingsService settings)
    {
        _cache = cache;
        _settings = settings;
    }

    public void Start()
    {
        try
        {
            AppNotificationManager.Default.Register();
            _registered = true;
        }
        catch
        {
            _registered = false;
        }
        _cache.Changed += OnCacheChanged;
    }

    public void Stop()
    {
        _cache.Changed -= OnCacheChanged;
        if (_registered)
        {
            try { AppNotificationManager.Default.Unregister(); } catch { /* ignore */ }
            _registered = false;
        }
    }

    private void OnCacheChanged(object? sender, DeviceChangedEventArgs e)
    {
        if (e.Kind != DeviceChangeKind.Added) return;
        if (!_settings.Current.ToastOnNewDevice) return;
        if (DateTime.UtcNow < _startupGrace) return;
        if (!_announced.Add(e.Device.ServiceInstanceName)) return;
        if (!_registered) return;

        try
        {
            var subtitle = ShortServiceType(e.Device.ServiceType);
            var notification = new AppNotificationBuilder()
                .AddText(Strings.Get("ToastNewDeviceTitle"))
                .AddText($"{e.Device.DisplayName}")
                .AddText($"{e.Device.Hostname ?? "?"} · {subtitle}")
                .BuildNotification();
            AppNotificationManager.Default.Show(notification);
        }
        catch
        {
            // ignore toast errors
        }
    }

    private static string ShortServiceType(string type)
    {
        var t = type.TrimEnd('.');
        if (t.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            t = t[..^".local".Length];
        return t;
    }

    public void Dispose() => Stop();
}
