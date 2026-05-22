using System.Net;
using FluentAssertions;
using Specht.Core.Models;
using Specht.Core.Services;
using Xunit;

namespace Specht.Core.Tests;

public class DeviceCacheTests
{
    private static Device MakeDevice(string key = "test._airplay._tcp.local", string display = "Test")
    {
        return new Device(
            ServiceInstanceName: key,
            DisplayName: display,
            Hostname: "test.local",
            Addresses: new[] { IPAddress.Parse("192.168.1.50") },
            Port: 7000,
            ServiceType: "_airplay._tcp.local",
            Txt: new Dictionary<string, string> { ["v"] = "1" },
            FirstSeen: DateTimeOffset.UtcNow,
            LastSeen: DateTimeOffset.UtcNow,
            Category: ServiceCategory.AirPlay);
    }

    [Fact]
    public void Upsert_NewDevice_RaisesAddedAndAppearsInSnapshot()
    {
        var cache = new DeviceCache();
        DeviceChangedEventArgs? captured = null;
        cache.Changed += (_, e) => captured = e;

        var device = MakeDevice();
        cache.Upsert(device);

        cache.Snapshot().Should().ContainSingle().Which.ServiceInstanceName.Should().Be(device.ServiceInstanceName);
        captured.Should().NotBeNull();
        captured!.Kind.Should().Be(DeviceChangeKind.Added);
    }

    [Fact]
    public void Upsert_ExistingDevice_RaisesUpdated()
    {
        var cache = new DeviceCache();
        cache.Upsert(MakeDevice());
        DeviceChangedEventArgs? captured = null;
        cache.Changed += (_, e) => captured = e;

        cache.Upsert(MakeDevice(display: "Test (Updated)"));

        cache.Snapshot().Should().ContainSingle().Which.DisplayName.Should().Be("Test (Updated)");
        captured!.Kind.Should().Be(DeviceChangeKind.Updated);
    }

    [Fact]
    public void Remove_KnownKey_RaisesRemovedAndClearsEntry()
    {
        var cache = new DeviceCache();
        cache.Upsert(MakeDevice());
        DeviceChangedEventArgs? captured = null;
        cache.Changed += (_, e) => captured = e;

        var removed = cache.Remove("test._airplay._tcp.local");

        removed.Should().BeTrue();
        cache.Snapshot().Should().BeEmpty();
        captured!.Kind.Should().Be(DeviceChangeKind.Removed);
    }

    [Fact]
    public void Remove_UnknownKey_ReturnsFalse()
    {
        var cache = new DeviceCache();
        cache.Remove("not.there").Should().BeFalse();
    }

    [Fact]
    public void Clear_RaisesRemovedForEachEntry()
    {
        var cache = new DeviceCache();
        cache.Upsert(MakeDevice("a._airplay._tcp.local"));
        cache.Upsert(MakeDevice("b._airplay._tcp.local"));
        var removedCount = 0;
        cache.Changed += (_, e) => { if (e.Kind == DeviceChangeKind.Removed) removedCount++; };

        cache.Clear();

        cache.Snapshot().Should().BeEmpty();
        removedCount.Should().Be(2);
    }

    [Fact]
    public void Snapshot_ReturnsIndependentCopy()
    {
        var cache = new DeviceCache();
        cache.Upsert(MakeDevice());

        var first = cache.Snapshot();
        cache.Clear();
        var second = cache.Snapshot();

        first.Should().HaveCount(1);
        second.Should().BeEmpty();
    }
}
