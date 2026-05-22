using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using Makaretu.Dns;

var sw = System.Diagnostics.Stopwatch.StartNew();
void Log(string msg) => Console.WriteLine($"[{sw.Elapsed:mm\\:ss\\.fff}] {msg}");

Log("=== Specht Spike - Makaretu.Dns.Multicast.New 0.38.0 on .NET 9 ===");
Log("");
Log("Aktive Netzwerk-Interfaces:");
foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()
             .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
{
    var props = nic.GetIPProperties();
    var v4 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.Address;
    var v6 = props.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6 && !a.Address.IsIPv6LinkLocal)?.Address;
    Log($"  - {nic.Name} ({nic.NetworkInterfaceType}): v4={v4}, v6={v6}");
}
Log("");

var seenServiceTypes = new ConcurrentDictionary<string, byte>();
var seenInstances = new ConcurrentDictionary<string, InstanceInfo>();
var goodbyes = new ConcurrentBag<string>();
int answerCount = 0;

using var mdns = new MulticastService();
using var sd = new ServiceDiscovery(mdns);

mdns.NetworkInterfaceDiscovered += (s, e) =>
{
    foreach (var nic in e.NetworkInterfaces)
        Log($"NIC erkannt von Makaretu: {nic.Name}");
};

sd.ServiceDiscovered += (s, serviceName) =>
{
    if (seenServiceTypes.TryAdd(serviceName.ToString(), 0))
    {
        Log($"  Service-Typ entdeckt: {serviceName}");
        sd.QueryServiceInstances(serviceName);
    }
};

sd.ServiceInstanceDiscovered += (s, e) =>
{
    var key = e.ServiceInstanceName.ToString();
    var info = seenInstances.GetOrAdd(key, k => new InstanceInfo(
        ServiceType: string.Join(".", e.ServiceInstanceName.Labels.Skip(1)),
        Instance: e.ServiceInstanceName.Labels[0]));
    info.LastSeen = DateTime.UtcNow;
    Log($"  + Instanz: {key}");

    foreach (var rec in e.Message.AdditionalRecords.Concat(e.Message.Answers))
    {
        switch (rec)
        {
            case SRVRecord srv:
                info.Port = srv.Port;
                info.Hostname = srv.Target.ToString();
                break;
            case TXTRecord txt:
                foreach (var entry in txt.Strings)
                {
                    var idx = entry.IndexOf('=');
                    if (idx > 0) info.Txt[entry[..idx]] = entry[(idx + 1)..];
                    else info.Txt[entry] = "";
                }
                break;
            case ARecord a:
                if (!info.Addresses.Contains(a.Address)) info.Addresses.Add(a.Address);
                break;
            case AAAARecord aaaa:
                if (!info.Addresses.Contains(aaaa.Address)) info.Addresses.Add(aaaa.Address);
                break;
        }
    }
};

sd.ServiceInstanceShutdown += (s, e) =>
{
    goodbyes.Add(e.ServiceInstanceName.ToString());
    Log($"  - Goodbye: {e.ServiceInstanceName}");
};

mdns.AnswerReceived += (s, e) => Interlocked.Increment(ref answerCount);

Log("MulticastService starten...");
mdns.Start();
Log("QueryAllServices() - PTR-Abfrage auf _services._dns-sd._udp.local");
sd.QueryAllServices();

const int scanSeconds = 20;
Log($"Scanne {scanSeconds} Sekunden...");
Log("");

await Task.Delay(TimeSpan.FromSeconds(scanSeconds));

Log("");
Log("=== Zusammenfassung ===");
Log($"Service-Typen entdeckt: {seenServiceTypes.Count}");
Log($"Instanzen entdeckt:     {seenInstances.Count}");
Log($"Goodbyes empfangen:     {goodbyes.Count}");
Log($"mDNS-Antworten total:   {answerCount}");
Log("");

if (seenInstances.IsEmpty)
{
    Log("KEINE Instanzen gefunden. Moegliche Ursachen:");
    Log("  - Firewall blockiert UDP 5353");
    Log("  - Kein mDNS-Geraet im Netz aktiv");
    Log("  - Falsches Interface gewaehlt");
}
else
{
    Log("Details pro Instanz:");
    foreach (var kv in seenInstances.OrderBy(k => k.Key))
    {
        var i = kv.Value;
        Log($"  [{i.ServiceType}] {i.Instance}");
        Log($"    Host:  {i.Hostname ?? "(unresolved)"}");
        Log($"    Port:  {i.Port?.ToString() ?? "(unresolved)"}");
        Log($"    Addr:  {(i.Addresses.Count == 0 ? "(none)" : string.Join(", ", i.Addresses))}");
        if (i.Txt.Count > 0)
        {
            var preview = string.Join(", ", i.Txt.Take(4).Select(t => $"{t.Key}={Truncate(t.Value, 30)}"));
            Log($"    TXT:   {preview}{(i.Txt.Count > 4 ? $" (+{i.Txt.Count - 4} more)" : "")}");
        }
    }
}

var unresolved = seenInstances.Values.Count(i => i.Addresses.Count == 0 || i.Port == null);
if (unresolved > 0)
    Log($"WARNUNG: {unresolved} Instanz(en) ohne Adresse/Port - Resolve unvollstaendig.");

var withV6 = seenInstances.Values.Count(i => i.Addresses.Any(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6));
Log($"IPv6-Coverage: {withV6}/{seenInstances.Count} Instanzen haben mind. eine v6-Adresse.");

static string Truncate(string s, int n) => s.Length <= n ? s : s[..n] + "...";

record InstanceInfo(string ServiceType, string Instance)
{
    public List<IPAddress> Addresses { get; } = new();
    public ushort? Port { get; set; }
    public string? Hostname { get; set; }
    public Dictionary<string, string> Txt { get; } = new();
    public DateTime FirstSeen { get; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}
