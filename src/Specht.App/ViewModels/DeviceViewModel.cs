using CommunityToolkit.Mvvm.ComponentModel;
using Specht.Core.Models;

namespace Specht.App.ViewModels;

public sealed partial class DeviceViewModel(Device device) : ObservableObject
{
    public Device Device { get; private set; } = device;

    public void UpdateDevice(Device next) => Device = next;

    public string DisplayName => Device.DisplayName;

    public string Subtitle =>
        $"{Device.Hostname ?? "?"} · {ShortServiceType(Device.ServiceType)}";

    public string CategoryLabel => Device.Category.ToString();

    // Glyphs from Segoe Fluent Icons (PUA range E000-F8FF).
    public string CategoryGlyph => Device.Category switch
    {
        ServiceCategory.AirPlay       => "", // Volume
        ServiceCategory.Cast          => "", // CastDevice
        ServiceCategory.Audio         => "", // MusicNote
        ServiceCategory.Print         => "", // Print
        ServiceCategory.HomeKit       => "", // Home
        ServiceCategory.FileShare     => "", // Folder
        ServiceCategory.RemoteControl => "", // Devices
        ServiceCategory.IoT           => "", // generic device
        _                             => "",
    };

    [ObservableProperty]
    private bool _isNew;

    [ObservableProperty]
    private bool _isLeaving;

    private static string ShortServiceType(string type)
    {
        var t = type.TrimEnd('.');
        if (t.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            t = t[..^".local".Length];
        return t;
    }

    public bool Matches(string search)
    {
        if (string.IsNullOrWhiteSpace(search)) return true;
        var s = search.Trim();
        return Device.DisplayName.Contains(s, StringComparison.OrdinalIgnoreCase)
               || (Device.Hostname?.Contains(s, StringComparison.OrdinalIgnoreCase) ?? false)
               || Device.ServiceType.Contains(s, StringComparison.OrdinalIgnoreCase)
               || Device.Addresses.Any(a => a.ToString().Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}
