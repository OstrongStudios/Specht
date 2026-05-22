using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Specht.Core;

public static class NetworkUtils
{
    private static readonly string[] VpnDescriptionPatterns =
    {
        "VPN",
        "TAP-Windows",
        "Wintun",
        "WireGuard",
        "OpenVPN",
        "Cisco AnyConnect",
        "FortiClient",
        "GlobalProtect",
        "Tailscale",
        "ZeroTier",
        "NordVPN",
        "ExpressVPN",
    };

    /// <summary>
    /// Heuristically detects whether a VPN-like tunnel adapter is currently
    /// up. mDNS multicast often doesn't traverse VPN tunnels, so this is the
    /// signal for showing a "VPN may block local discovery" hint.
    /// </summary>
    /// <summary>
    /// Finds the names of local network adapters that could plausibly reach the
    /// given remote address (same IPv4 subnet, or same IPv6 link/zone).
    /// Used to label which interface a discovered device is reachable on.
    /// </summary>
    public static IReadOnlyList<string> FindAdaptersFor(IEnumerable<IPAddress> remoteAddresses)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                            && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .ToArray();

            foreach (var remote in remoteAddresses)
            {
                foreach (var nic in nics)
                {
                    var props = nic.GetIPProperties();
                    foreach (var ua in props.UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != remote.AddressFamily) continue;
                        if (remote.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (ua.IPv4Mask is null) continue;
                            if (SameSubnetV4(remote, ua.Address, ua.IPv4Mask))
                                result.Add(nic.Name);
                        }
                        else if (remote.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            // IPv6 link-local: same scope id implies same link
                            if (remote.IsIPv6LinkLocal && ua.Address.IsIPv6LinkLocal)
                                result.Add(nic.Name);
                            else if (SameSubnetV6(remote, ua.Address, ua.PrefixLength))
                                result.Add(nic.Name);
                        }
                    }
                }
            }
        }
        catch
        {
            // ignore — fall back to empty list
        }
        return result.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool SameSubnetV4(IPAddress a, IPAddress b, IPAddress mask)
    {
        var ab = a.GetAddressBytes();
        var bb = b.GetAddressBytes();
        var mb = mask.GetAddressBytes();
        if (ab.Length != 4 || bb.Length != 4 || mb.Length != 4) return false;
        for (var i = 0; i < 4; i++)
            if ((ab[i] & mb[i]) != (bb[i] & mb[i])) return false;
        return true;
    }

    private static bool SameSubnetV6(IPAddress a, IPAddress b, int prefixLength)
    {
        if (prefixLength <= 0 || prefixLength > 128) return false;
        var ab = a.GetAddressBytes();
        var bb = b.GetAddressBytes();
        if (ab.Length != 16 || bb.Length != 16) return false;
        var fullBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;
        for (var i = 0; i < fullBytes; i++)
            if (ab[i] != bb[i]) return false;
        if (remainingBits == 0) return true;
        var mask = (byte)(0xFF << (8 - remainingBits));
        return (ab[fullBytes] & mask) == (bb[fullBytes] & mask);
    }

    public static bool IsLikelyVpnActive()
    {
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Tunnel ||
                    nic.NetworkInterfaceType == NetworkInterfaceType.Ppp)
                    return true;
                foreach (var pattern in VpnDescriptionPatterns)
                {
                    if (nic.Description.Contains(pattern, StringComparison.OrdinalIgnoreCase) ||
                        nic.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }
        catch
        {
            // ignore — fall back to "no VPN"
        }
        return false;
    }
}
