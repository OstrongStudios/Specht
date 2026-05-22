using Microsoft.Win32;
using Specht.Core.Services;

namespace Specht.App.Services;

/// <summary>
/// Restarts discovery after the system wakes from sleep.
/// Spec §F-DISC-08 / §5 Stabilität (Sleep/Wake-Watchdog).
/// </summary>
public sealed class PowerWatchdog : IDisposable
{
    private readonly DiscoveryService _discovery;
    private bool _hooked;

    public PowerWatchdog(DiscoveryService discovery)
    {
        _discovery = discovery;
    }

    public void Start()
    {
        if (_hooked) return;
        try
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _hooked = true;
        }
        catch
        {
            _hooked = false;
        }
    }

    public void Stop()
    {
        if (!_hooked) return;
        try { SystemEvents.PowerModeChanged -= OnPowerModeChanged; } catch { /* ignore */ }
        _hooked = false;
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume) return;
        try
        {
            _discovery.Stop();
            _discovery.Start();
        }
        catch
        {
            // ignore restart errors
        }
    }

    public void Dispose() => Stop();
}
