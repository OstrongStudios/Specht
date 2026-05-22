using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Specht.App.Services;
using Specht.Core;
using Specht.Core.Models;
using Specht.Core.Services;
using Windows.ApplicationModel.DataTransfer;

namespace Specht.App.ViewModels;

public sealed record TxtEntry(string Key, string Value, bool IsBinary)
{
    public Visibility BinaryIndicator => IsBinary ? Visibility.Visible : Visibility.Collapsed;
}

public sealed partial class DeviceDetailViewModel : ObservableObject, IDisposable
{
    private readonly DeviceCache _cache;
    private readonly string _serviceInstanceName;
    private readonly DispatcherQueue _dispatcher;
    private Device? _device;

    [ObservableProperty]
    private string _displayName = "";

    [ObservableProperty]
    private string _categoryLabel = "";

    [ObservableProperty]
    private string _categoryGlyph = "";

    [ObservableProperty]
    private string _hostnameText = Strings.Get("ValueUnknown");

    [ObservableProperty]
    private string _serviceTypeText = "";

    [ObservableProperty]
    private string _instanceNameText = "";

    [ObservableProperty]
    private string _iPv4Text = Strings.Get("ValueNone");

    [ObservableProperty]
    private string _iPv6Text = Strings.Get("ValueNone");

    [ObservableProperty]
    private string _portText = Strings.Get("ValueUnknown");

    [ObservableProperty]
    private string _sourceAdaptersText = Strings.Get("ValueUnknown");

    [ObservableProperty]
    private string _firstSeenText = "";

    [ObservableProperty]
    private string _lastSeenText = "";

    [ObservableProperty]
    private Visibility _noTxtVisibility = Visibility.Collapsed;

    public ObservableCollection<TxtEntry> TxtEntries { get; } = new();

    public DeviceDetailViewModel(DeviceCache cache, string serviceInstanceName)
    {
        _cache = cache;
        _serviceInstanceName = serviceInstanceName;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        _cache.Changed += OnCacheChanged;
        Refresh();
    }

    private void OnCacheChanged(object? sender, DeviceChangedEventArgs e)
    {
        if (!string.Equals(e.Device.ServiceInstanceName, _serviceInstanceName, StringComparison.OrdinalIgnoreCase))
            return;
        _dispatcher.TryEnqueue(Refresh);
    }

    private void Refresh()
    {
        _device = _cache.Snapshot().FirstOrDefault(d =>
            string.Equals(d.ServiceInstanceName, _serviceInstanceName, StringComparison.OrdinalIgnoreCase));
        if (_device is null) return;

        DisplayName = _device.DisplayName;
        CategoryLabel = _device.Category.ToString();
        CategoryGlyph = new DeviceViewModel(_device).CategoryGlyph;
        HostnameText = _device.Hostname ?? Strings.Get("ValueUnknown");
        ServiceTypeText = _device.ServiceType;
        InstanceNameText = _device.ServiceInstanceName;

        var v4 = _device.IPv4.Select(a => a.ToString()).ToArray();
        IPv4Text = v4.Length == 0 ? "(keine)" : string.Join(", ", v4);

        var v6 = _device.IPv6.Select(a => a.ToString()).ToArray();
        IPv6Text = v6.Length == 0 ? "(keine)" : string.Join(", ", v6);

        PortText = _device.Port?.ToString() ?? Strings.Get("ValueUnknown");

        var adapters = NetworkUtils.FindAdaptersFor(_device.Addresses);
        SourceAdaptersText = adapters.Count == 0 ? Strings.Get("ValueUnknown") : string.Join(", ", adapters);

        FirstSeenText = _device.FirstSeen.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        LastSeenText = _device.LastSeen.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        TxtEntries.Clear();
        foreach (var kv in _device.Txt.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
        {
            var (display, isBinary) = FormatTxtValue(kv.Value);
            TxtEntries.Add(new TxtEntry(kv.Key, display, isBinary));
        }
        NoTxtVisibility = TxtEntries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    [RelayCommand]
    private void CopyIPv4() => CopyToClipboard(IPv4Text);

    [RelayCommand]
    private void CopyIPv6() => CopyToClipboard(IPv6Text);

    [RelayCommand]
    private void CopyPort() => CopyToClipboard(PortText);

    [RelayCommand]
    private void CopyAsJson()
    {
        if (_device is null) return;
        var json = JsonSerializer.Serialize(new
        {
            displayName = _device.DisplayName,
            hostname = _device.Hostname,
            addresses = new
            {
                ipv4 = _device.IPv4.Select(a => a.ToString()).ToArray(),
                ipv6 = _device.IPv6.Select(a => a.ToString()).ToArray(),
            },
            port = _device.Port,
            serviceType = _device.ServiceType,
            txt = _device.Txt,
            firstSeen = _device.FirstSeen,
            lastSeen = _device.LastSeen,
        }, new JsonSerializerOptions { WriteIndented = true });
        CopyToClipboard(json);
    }

    /// <summary>
    /// Detects whether a TXT value contains bytes that aren't safely printable
    /// and falls back to a hex representation (spec §9). Returns the value to
    /// show plus a flag the UI uses to render a warning indicator.
    /// </summary>
    private static (string display, bool isBinary) FormatTxtValue(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return (raw, false);
        var isBinary = false;
        foreach (var c in raw)
        {
            // Printable ASCII range plus common whitespace are fine. U+FFFD
            // (replacement char from broken UTF-8) and control chars trip the flag.
            if (c == '\t' || c == '\n' || c == '\r') continue;
            if (c >= 0x20 && c <= 0x7E) continue;
            if (c >= 0xA0 && c < 0xD800) continue; // printable extended Unicode
            if (c > 0xE000 && c < 0xFFFD) continue;
            isBinary = true;
            break;
        }
        if (!isBinary) return (raw, false);
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        return ("0x" + Convert.ToHexString(bytes), true);
    }

    private static void CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    public void Dispose()
    {
        _cache.Changed -= OnCacheChanged;
    }
}
