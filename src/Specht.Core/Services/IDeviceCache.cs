using Specht.Core.Models;

namespace Specht.Core.Services;

public interface IDeviceCache
{
    IReadOnlyCollection<Device> Snapshot();

    event EventHandler<DeviceChangedEventArgs>? Changed;
}

public enum DeviceChangeKind
{
    Added,
    Updated,
    Removed,
}

public sealed class DeviceChangedEventArgs(DeviceChangeKind kind, Device device) : EventArgs
{
    public DeviceChangeKind Kind { get; } = kind;
    public Device Device { get; } = device;
}
