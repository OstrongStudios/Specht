using System.Net;
using FluentAssertions;
using Specht.Core.Models;
using Specht.Core.Services;
using Xunit;

namespace Specht.Core.Tests;

/// <summary>
/// Mock-responder-style tests: synthesize "discovery events" via the internal
/// TestHook API and verify the full integration of cache + categorization +
/// record building works as expected.
///
/// Per spec §6 the project ships with a small mock responder (~100 LoC) — this
/// is its deterministic, UDP-free incarnation.
/// </summary>
public class DiscoveryServiceIntegrationTests
{
    [Fact]
    public void IngestInstance_PopulatesCacheWithCorrectCategory()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        sut.TestHook_IngestInstance(
            serviceInstanceName: "Apple-TV._airplay._tcp.local",
            displayName: "Apple-TV",
            serviceType: "_airplay._tcp.local",
            hostname: "apple-tv.local",
            port: 7000,
            addresses: new[] { IPAddress.Parse("192.168.1.42") },
            txt: new Dictionary<string, string> { ["model"] = "AppleTV6,2" });

        var devices = cache.Snapshot();
        devices.Should().ContainSingle();

        var d = devices.First();
        d.Category.Should().Be(ServiceCategory.AirPlay);
        d.DisplayName.Should().Be("Apple-TV");
        d.Hostname.Should().Be("apple-tv.local");
        d.Port.Should().Be(7000);
        d.Addresses.Should().ContainSingle()
            .Which.Should().Be(IPAddress.Parse("192.168.1.42"));
        d.Txt.Should().ContainKey("model").WhoseValue.Should().Be("AppleTV6,2");
    }

    [Fact]
    public void IngestInstance_TwiceWithSameKey_UpdatesNotDuplicates()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        sut.TestHook_IngestInstance(
            serviceInstanceName: "Printer._ipp._tcp.local",
            displayName: "Printer",
            serviceType: "_ipp._tcp.local",
            hostname: "printer.local",
            port: 631,
            addresses: new[] { IPAddress.Parse("192.168.1.20") });

        // Second time: same instance now has an IPv6 address too.
        sut.TestHook_IngestInstance(
            serviceInstanceName: "Printer._ipp._tcp.local",
            displayName: "Printer",
            serviceType: "_ipp._tcp.local",
            hostname: "printer.local",
            port: 631,
            addresses: new[] { IPAddress.Parse("fe80::1") });

        var devices = cache.Snapshot();
        devices.Should().ContainSingle();
        devices.First().Addresses.Should().HaveCount(2);
        devices.First().Category.Should().Be(ServiceCategory.Print);
    }

    [Fact]
    public void RemoveInstance_FiresRemovedEvent()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);
        sut.TestHook_IngestInstance(
            serviceInstanceName: "X._smb._tcp.local",
            displayName: "X",
            serviceType: "_smb._tcp.local",
            hostname: "x.local",
            port: 445,
            addresses: new[] { IPAddress.Parse("192.168.1.99") });

        DeviceChangedEventArgs? captured = null;
        cache.Changed += (_, e) =>
        {
            if (e.Kind == DeviceChangeKind.Removed) captured = e;
        };

        sut.TestHook_RemoveInstance("X._smb._tcp.local");

        captured.Should().NotBeNull();
        cache.Snapshot().Should().BeEmpty();
        sut.TestHook_InstanceCount.Should().Be(0);
    }

    [Fact]
    public void IngestInstance_UnknownServiceType_CategorizedAsOther()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        sut.TestHook_IngestInstance(
            serviceInstanceName: "Mystery._never-heard-of._tcp.local",
            displayName: "Mystery",
            serviceType: "_never-heard-of._tcp.local",
            hostname: null,
            port: null,
            addresses: Array.Empty<IPAddress>());

        cache.Snapshot().First().Category.Should().Be(ServiceCategory.Other);
    }

    [Fact]
    public void IngestInstance_RaisesCacheChanged()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);
        DeviceChangedEventArgs? added = null;
        cache.Changed += (_, e) => { if (e.Kind == DeviceChangeKind.Added) added = e; };

        sut.TestHook_IngestInstance(
            serviceInstanceName: "Speaker._raop._tcp.local",
            displayName: "Speaker",
            serviceType: "_raop._tcp.local",
            hostname: "speaker.local",
            port: 7000,
            addresses: new[] { IPAddress.Parse("192.168.1.11") });

        added.Should().NotBeNull();
        added!.Device.Category.Should().Be(ServiceCategory.AirPlay);
    }

    [Fact]
    public void IngestInstance_MergesTxtAcrossMultipleCalls()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        sut.TestHook_IngestInstance(
            serviceInstanceName: "Z._airplay._tcp.local",
            displayName: "Z", serviceType: "_airplay._tcp.local",
            hostname: "z.local", port: 7000,
            addresses: new[] { IPAddress.Parse("10.0.0.1") },
            txt: new Dictionary<string, string> { ["key1"] = "value1" });

        sut.TestHook_IngestInstance(
            serviceInstanceName: "Z._airplay._tcp.local",
            displayName: "Z", serviceType: "_airplay._tcp.local",
            hostname: "z.local", port: 7000,
            addresses: new[] { IPAddress.Parse("10.0.0.1") },
            txt: new Dictionary<string, string> { ["key2"] = "value2" });

        var device = cache.Snapshot().Single();
        device.Txt.Should().ContainKey("key1");
        device.Txt.Should().ContainKey("key2");
    }
}
