using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using Makaretu.Dns;
using Microsoft.Extensions.Logging;
using Specht.Core.Models;

namespace Specht.Core.Services;

public sealed class DiscoveryService : IDiscoveryService, IDisposable
{
    private readonly DeviceCache _cache;
    private readonly ILogger<DiscoveryService> _log;

    private MulticastService? _mdns;
    private ServiceDiscovery? _sd;
    private CancellationTokenSource? _cts;
    private Task? _requeryTask;
    private Task? _cleanupTask;

    /// <summary>
    /// How long an instance may be silent (no answer received) before it is
    /// removed from the cache. Default 5 minutes — long enough that healthy
    /// devices stay (mDNS announcement intervals are typically much shorter
    /// thanks to our re-query loop), short enough that powered-off devices
    /// fall off without a goodbye packet.
    /// </summary>
    public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, byte> _serviceTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, InstanceBuilder> _instances = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _hostToInstances = new(StringComparer.OrdinalIgnoreCase);

    public DiscoveryService(DeviceCache cache, ILogger<DiscoveryService>? log = null)
    {
        _cache = cache;
        _log = log ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DiscoveryService>.Instance;
    }

    public bool IsRunning => _mdns is not null;

    /// <summary>Number of raw mDNS answers received since Start (resets on Refresh).</summary>
    public long AnswersReceived => Interlocked.Read(ref _answersReceived);
    private long _answersReceived;

    /// <summary>UTC timestamp of last Start/Refresh call.</summary>
    public DateTimeOffset StartedAt { get; private set; } = DateTimeOffset.MinValue;

    public void Start()
    {
        if (IsRunning) return;

        _cts = new CancellationTokenSource();
        _mdns = new MulticastService(FilterInterfaces);
        _sd = new ServiceDiscovery(_mdns);

        _sd.ServiceDiscovered += OnServiceTypeDiscovered;
        _sd.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
        _sd.ServiceInstanceShutdown += OnServiceInstanceShutdown;
        _mdns.AnswerReceived += OnAnswerReceived;

        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;

        Interlocked.Exchange(ref _answersReceived, 0);
        StartedAt = DateTimeOffset.UtcNow;

        _log.LogInformation("Starting mDNS discovery");
        _mdns.Start();
        _sd.QueryAllServices();

        _requeryTask = Task.Run(() => RequeryLoopAsync(_cts.Token));
        _cleanupTask = Task.Run(() => CleanupLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        if (!IsRunning) return;

        _log.LogInformation("Stopping mDNS discovery");
        NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;

        try { _cts?.Cancel(); } catch { /* ignore */ }
        try { _requeryTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        try { _cleanupTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }

        try { _sd?.Dispose(); } catch { /* ignore */ }
        try { _mdns?.Stop(); } catch { /* ignore */ }
        try { _mdns?.Dispose(); } catch { /* ignore */ }
        _sd = null;
        _mdns = null;
        _cts?.Dispose();
        _cts = null;
    }

    public void Refresh()
    {
        _log.LogInformation("Refresh requested");
        _instances.Clear();
        _serviceTypes.Clear();
        _hostToInstances.Clear();
        _cache.Clear();
        Interlocked.Exchange(ref _answersReceived, 0);
        StartedAt = DateTimeOffset.UtcNow;
        if (IsRunning)
        {
            _sd?.QueryAllServices();
        }
    }

    // Virtual NICs we never want to bind to. These all spam mDNS, slow down
    // initial enumeration, or simply have no real LAN behind them.
    private static readonly string[] VirtualNicSubstrings =
    {
        "WFP",                     // Windows Filtering Platform pseudo-NIC
        "QoS Packet Scheduler",    // QoS pseudo-NIC
        "LightWeight Filter",      // LightWeight filter
        "Filter",                  // generic filter adapters
        "Hyper-V",                 // Hyper-V Virtual Ethernet
        "Virtual Ethernet",        // Hyper-V vEthernet
        "vEthernet",               // Hyper-V naming
        "VirtualBox",              // Oracle VirtualBox host-only
        "VMware",                  // VMware vmnet8/vmnet1
        "Docker",                  // Docker Desktop bridges
        "WSL",                     // Windows Subsystem for Linux
        "Loopback Pseudo",         // Windows Loopback driver
        "TAP-Windows",             // OpenVPN TAP adapter
        "Wintun",                  // WireGuard / Wintun adapter
        "Tailscale",               // Tailscale tunnel
        "ZeroTier",                // ZeroTier virtual switch
        "Npcap Loopback",          // Wireshark capture adapter
    };

    private static IEnumerable<NetworkInterface> FilterInterfaces(IEnumerable<NetworkInterface> all) =>
        all.Where(n =>
            n.OperationalStatus == OperationalStatus.Up
            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
            && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel
            && n.NetworkInterfaceType != NetworkInterfaceType.Ppp
            && n.SupportsMulticast
            && !ContainsAny(n.Description, VirtualNicSubstrings)
            && !ContainsAny(n.Name, VirtualNicSubstrings));

    private static bool ContainsAny(string source, string[] needles)
    {
        if (string.IsNullOrEmpty(source)) return false;
        foreach (var n in needles)
            if (source.Contains(n, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        _log.LogInformation("Network address changed, refreshing discovery");
        try { Refresh(); } catch (Exception ex) { _log.LogWarning(ex, "Refresh after network change failed"); }
    }

    private async Task CleanupLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct).ConfigureAwait(false);
                if (ct.IsCancellationRequested) return;

                var cutoff = DateTimeOffset.UtcNow - IdleTimeout;
                foreach (var kv in _instances.ToArray())
                {
                    if (kv.Value.LastSeen < cutoff)
                    {
                        if (_instances.TryRemove(kv.Key, out _))
                        {
                            _log.LogDebug("Stale: {Instance} (last seen {LastSeen})", kv.Key, kv.Value.LastSeen);
                            _cache.Remove(kv.Key);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
        catch (Exception ex)
        {
            _log.LogDebug(ex, "Cleanup loop failed");
        }
    }

    private async Task RequeryLoopAsync(CancellationToken ct)
    {
        // Spike-Finding: initial discovery is sparse; re-query aggressively in first 15s, then back off.
        var schedule = new[] { 2_000, 3_000, 4_000, 6_000, 10_000, 30_000, 60_000 };
        var i = 0;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var delayMs = i < schedule.Length ? schedule[i] : 120_000;
                await Task.Delay(delayMs, ct).ConfigureAwait(false);
                if (ct.IsCancellationRequested) return;

                try
                {
                    _sd?.QueryAllServices();
                    foreach (var st in _serviceTypes.Keys)
                    {
                        _sd?.QueryServiceInstances(st);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogDebug(ex, "Re-query iteration failed");
                }
                i++;
            }
        }
        catch (OperationCanceledException) { /* shutdown */ }
    }

    private void OnServiceTypeDiscovered(object? sender, DomainName name)
    {
        var s = name.ToString();
        if (_serviceTypes.TryAdd(s, 0))
        {
            _log.LogDebug("Service type discovered: {Type}", s);
            try { _sd?.QueryServiceInstances(name); } catch { /* ignore */ }
        }
    }

    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
    {
        var key = e.ServiceInstanceName.ToString();
        var labels = e.ServiceInstanceName.Labels;
        var instanceLabel = labels.Count > 0 ? labels[0] : key;
        var serviceType = labels.Count > 1 ? string.Join(".", labels.Skip(1)) : "";

        var builder = _instances.GetOrAdd(key, _ => new InstanceBuilder(key, instanceLabel, serviceType));
        IngestMessage(builder, e.Message);
        Publish(builder);
    }

    private void OnServiceInstanceShutdown(object? sender, ServiceInstanceShutdownEventArgs e)
    {
        var key = e.ServiceInstanceName.ToString();
        if (_instances.TryRemove(key, out _))
        {
            _log.LogDebug("Goodbye: {Instance}", key);
            _cache.Remove(key);
        }
    }

    private void OnAnswerReceived(object? sender, MessageEventArgs e)
    {
        Interlocked.Increment(ref _answersReceived);
        // Backfill A/AAAA records that arrive after the instance was first discovered.
        foreach (var rec in e.Message.Answers.Concat(e.Message.AdditionalRecords))
        {
            if (rec is ARecord or AAAARecord)
            {
                var host = rec.Name.ToString();
                if (_hostToInstances.TryGetValue(host, out var instanceKeys))
                {
                    foreach (var ik in instanceKeys)
                    {
                        if (_instances.TryGetValue(ik, out var builder))
                        {
                            IngestRecord(builder, rec);
                            Publish(builder);
                        }
                    }
                }
            }
        }
    }

    private void IngestMessage(InstanceBuilder b, Message msg)
    {
        foreach (var rec in msg.Answers.Concat(msg.AdditionalRecords))
            IngestRecord(b, rec);
    }

    private void IngestRecord(InstanceBuilder b, ResourceRecord rec)
    {
        switch (rec)
        {
            case SRVRecord srv:
                b.Port = srv.Port;
                var host = srv.Target.ToString();
                b.Hostname = host;
                _hostToInstances.GetOrAdd(host, _ => new ConcurrentBag<string>()).Add(b.Key);
                break;
            case TXTRecord txt:
                lock (b.TxtLock)
                {
                    foreach (var entry in txt.Strings)
                    {
                        if (string.IsNullOrEmpty(entry)) continue;
                        var idx = entry.IndexOf('=');
                        if (idx > 0) b.Txt[entry[..idx]] = entry[(idx + 1)..];
                        else b.Txt[entry] = "";
                    }
                }
                break;
            case ARecord a:
                lock (b.AddressLock) { if (!b.Addresses.Contains(a.Address)) b.Addresses.Add(a.Address); }
                break;
            case AAAARecord aaaa:
                lock (b.AddressLock) { if (!b.Addresses.Contains(aaaa.Address)) b.Addresses.Add(aaaa.Address); }
                break;
        }
        b.LastSeen = DateTimeOffset.UtcNow;
    }

    private void Publish(InstanceBuilder b)
    {
        IReadOnlyList<IPAddress> addr;
        IReadOnlyDictionary<string, string> txt;
        lock (b.AddressLock) addr = b.Addresses.ToArray();
        lock (b.TxtLock) txt = new Dictionary<string, string>(b.Txt);

        var device = new Device(
            ServiceInstanceName: b.Key,
            DisplayName: b.InstanceLabel,
            Hostname: b.Hostname,
            Addresses: addr,
            Port: b.Port,
            ServiceType: b.ServiceType,
            Txt: txt,
            FirstSeen: b.FirstSeen,
            LastSeen: b.LastSeen,
            Category: ServiceTypeMapping.Categorize(b.ServiceType));

        _cache.Upsert(device);
    }

    public void Dispose() => Stop();

    // ----- Test hooks (InternalsVisibleTo Specht.Core.Tests) -----
    //
    // These bypass the UDP/Multicast path and let tests synthesize discovery
    // events deterministically. They go through the same Publish/Remove path
    // that production uses, so the integration of cache + categorization +
    // record building is fully exercised.

    internal void TestHook_IngestInstance(
        string serviceInstanceName,
        string displayName,
        string serviceType,
        string? hostname,
        ushort? port,
        IReadOnlyList<IPAddress> addresses,
        IReadOnlyDictionary<string, string>? txt = null)
    {
        var builder = _instances.GetOrAdd(serviceInstanceName,
            _ => new InstanceBuilder(serviceInstanceName, displayName, serviceType));
        builder.Hostname = hostname;
        builder.Port = port;
        lock (builder.AddressLock)
            foreach (var addr in addresses)
                if (!builder.Addresses.Contains(addr)) builder.Addresses.Add(addr);
        if (txt is not null)
            lock (builder.TxtLock)
                foreach (var kv in txt) builder.Txt[kv.Key] = kv.Value;
        builder.LastSeen = DateTimeOffset.UtcNow;
        Publish(builder);
    }

    internal void TestHook_RemoveInstance(string serviceInstanceName)
    {
        if (_instances.TryRemove(serviceInstanceName, out _))
            _cache.Remove(serviceInstanceName);
    }

    internal void TestHook_TouchInstance(string serviceInstanceName, DateTimeOffset lastSeen)
    {
        if (_instances.TryGetValue(serviceInstanceName, out var b)) b.LastSeen = lastSeen;
    }

    internal int TestHook_InstanceCount => _instances.Count;

    private sealed class InstanceBuilder(string key, string instanceLabel, string serviceType)
    {
        public string Key { get; } = key;
        public string InstanceLabel { get; } = instanceLabel;
        public string ServiceType { get; } = serviceType;
        public string? Hostname { get; set; }
        public ushort? Port { get; set; }
        public List<IPAddress> Addresses { get; } = new();
        public Dictionary<string, string> Txt { get; } = new();
        public object AddressLock { get; } = new();
        public object TxtLock { get; } = new();
        public DateTimeOffset FirstSeen { get; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastSeen { get; set; } = DateTimeOffset.UtcNow;
    }
}
