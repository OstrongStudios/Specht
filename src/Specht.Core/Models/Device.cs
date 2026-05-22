using System.Net;

namespace Specht.Core.Models;

public sealed record Device(
    string ServiceInstanceName,
    string DisplayName,
    string? Hostname,
    IReadOnlyList<IPAddress> Addresses,
    ushort? Port,
    string ServiceType,
    IReadOnlyDictionary<string, string> Txt,
    DateTimeOffset FirstSeen,
    DateTimeOffset LastSeen,
    ServiceCategory Category)
{
    public IEnumerable<IPAddress> IPv4 =>
        Addresses.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

    public IEnumerable<IPAddress> IPv6 =>
        Addresses.Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6);

    public bool IsResolved => Port is not null && Addresses.Count > 0;
}
