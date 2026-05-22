using System.Collections.Concurrent;
using Specht.Core.Models;

namespace Specht.Core.Services;

public sealed class DeviceCache : IDeviceCache
{
    private readonly ConcurrentDictionary<string, Device> _devices = new(StringComparer.OrdinalIgnoreCase);

    public event EventHandler<DeviceChangedEventArgs>? Changed;

    public IReadOnlyCollection<Device> Snapshot() => _devices.Values.ToArray();

    internal void Upsert(Device device)
    {
        var key = device.ServiceInstanceName;
        var isNew = !_devices.ContainsKey(key);
        _devices[key] = device;
        Changed?.Invoke(this, new DeviceChangedEventArgs(
            isNew ? DeviceChangeKind.Added : DeviceChangeKind.Updated, device));
    }

    internal bool Remove(string serviceInstanceName)
    {
        if (_devices.TryRemove(serviceInstanceName, out var device))
        {
            Changed?.Invoke(this, new DeviceChangedEventArgs(DeviceChangeKind.Removed, device));
            return true;
        }
        return false;
    }

    internal void Clear()
    {
        var snapshot = _devices.Values.ToArray();
        _devices.Clear();
        foreach (var d in snapshot)
            Changed?.Invoke(this, new DeviceChangedEventArgs(DeviceChangeKind.Removed, d));
    }
}
