using System.Net;
using System.Text.Json;
using FluentAssertions;
using Specht.Core.Models;
using Specht.Core.Services;
using Xunit;

namespace Specht.Core.Tests;

public class ExportServiceTests
{
    private static Device MakeDevice(string display = "Apple TV", string? hostname = "Apple-TV.local") =>
        new(
            ServiceInstanceName: $"{display}._airplay._tcp.local",
            DisplayName: display,
            Hostname: hostname,
            Addresses: new[] { IPAddress.Parse("192.168.1.42"), IPAddress.Parse("fe80::1") },
            Port: 7000,
            ServiceType: "_airplay._tcp.local",
            Txt: new Dictionary<string, string> { ["model"] = "AppleTV6,2", ["features"] = "0x4A7FFFF7" },
            FirstSeen: new DateTimeOffset(2026, 5, 22, 14, 0, 0, TimeSpan.Zero),
            LastSeen: new DateTimeOffset(2026, 5, 22, 14, 31, 0, TimeSpan.Zero),
            Category: ServiceCategory.AirPlay);

    [Fact]
    public void ToCsv_StartsWithBomAndHeader()
    {
        var sut = new ExportService();

        var csv = sut.ToCsv(new[] { MakeDevice() });

        csv.Should().StartWith("﻿Anzeigename;Hostname;IPv4;IPv6;Port;ServiceTyp;TXT");
    }

    [Fact]
    public void ToCsv_RowContainsAllFields()
    {
        var sut = new ExportService();

        var csv = sut.ToCsv(new[] { MakeDevice() });

        csv.Should().Contain("Apple TV;");
        csv.Should().Contain("Apple-TV.local;");
        csv.Should().Contain("192.168.1.42;");
        csv.Should().Contain("7000;");
        csv.Should().Contain("_airplay._tcp.local;");
        csv.Should().Contain("model=AppleTV6,2");
    }

    [Fact]
    public void ToCsv_QuotesFieldsContainingSeparator()
    {
        var sut = new ExportService();
        var device = MakeDevice(display: "Wohnzimmer; Apple TV");

        var csv = sut.ToCsv(new[] { device });

        csv.Should().Contain("\"Wohnzimmer; Apple TV\"");
    }

    [Fact]
    public void ToJson_MatchesSpecSchema()
    {
        var sut = new ExportService();

        var json = sut.ToJson(new[] { MakeDevice() });
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("exportedAt").GetString().Should().NotBeNullOrEmpty();
        root.GetProperty("deviceCount").GetInt32().Should().Be(1);

        var devices = root.GetProperty("devices");
        devices.GetArrayLength().Should().Be(1);
        var d = devices[0];
        d.GetProperty("displayName").GetString().Should().Be("Apple TV");
        d.GetProperty("hostname").GetString().Should().Be("Apple-TV.local");
        d.GetProperty("addresses").GetProperty("ipv4").GetArrayLength().Should().Be(1);
        d.GetProperty("addresses").GetProperty("ipv6").GetArrayLength().Should().Be(1);
        d.GetProperty("port").GetInt32().Should().Be(7000);
        d.GetProperty("serviceType").GetString().Should().Be("_airplay._tcp.local");
        d.GetProperty("txt").GetProperty("model").GetString().Should().Be("AppleTV6,2");
    }

    [Fact]
    public void ToJson_HandlesNullHostname()
    {
        var sut = new ExportService();
        var device = MakeDevice(hostname: null);

        var json = sut.ToJson(new[] { device });
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("devices")[0]
            .GetProperty("hostname").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void ToJson_EmptyList_HasZeroDeviceCount()
    {
        var sut = new ExportService();

        var json = sut.ToJson(Array.Empty<Device>());
        using var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("deviceCount").GetInt32().Should().Be(0);
        doc.RootElement.GetProperty("devices").GetArrayLength().Should().Be(0);
    }
}
