using System.Net;
using FluentAssertions;
using Specht.Core;
using Xunit;

namespace Specht.Core.Tests;

public class NetworkUtilsTests
{
    // The subnet helpers are private; we exercise them via FindAdaptersFor on
    // realistic addresses. Where direct testing matters, internal access is
    // available via InternalsVisibleTo (see SubnetMatchInternalTests below).

    [Fact]
    public void FindAdaptersFor_LoopbackAddress_ReturnsNoUsefulAdapter()
    {
        // 127.0.0.1 only matches the loopback interface, which we filter out.
        var result = NetworkUtils.FindAdaptersFor(new[] { IPAddress.Parse("127.0.0.1") });
        result.Should().BeEmpty();
    }

    [Fact]
    public void FindAdaptersFor_EmptyAddressList_ReturnsEmpty()
    {
        NetworkUtils.FindAdaptersFor(Array.Empty<IPAddress>()).Should().BeEmpty();
    }

    [Fact]
    public void IsLikelyVpnActive_DoesNotThrow()
    {
        // Whether or not a VPN is active depends on the host; we just verify
        // the helper enumerates adapters without exploding.
        var _ = NetworkUtils.IsLikelyVpnActive();
    }
}

public class SubnetMatchInternalTests
{
    // These reach into NetworkUtils private helpers via reflection; the cleaner
    // way would be to internalize them. For now reflection is fine for tests.

    private static bool SameSubnetV4(string addr, string nicAddr, string mask)
    {
        var t = typeof(NetworkUtils);
        var m = t.GetMethod("SameSubnetV4", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        return (bool)m.Invoke(null, new object[]
        {
            IPAddress.Parse(addr),
            IPAddress.Parse(nicAddr),
            IPAddress.Parse(mask),
        })!;
    }

    [Theory]
    [InlineData("192.168.1.42", "192.168.1.10", "255.255.255.0", true)]
    [InlineData("192.168.1.42", "192.168.2.10", "255.255.255.0", false)]
    [InlineData("10.0.0.5",    "10.0.0.1",     "255.0.0.0",     true)]
    [InlineData("10.1.0.5",    "10.0.0.1",     "255.255.0.0",   false)]
    public void SameSubnetV4_MaskBoundaries(string a, string b, string mask, bool expected)
    {
        SameSubnetV4(a, b, mask).Should().Be(expected);
    }
}
