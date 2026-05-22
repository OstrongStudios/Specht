using FluentAssertions;
using Specht.Core;
using Specht.Core.Models;
using Xunit;

namespace Specht.Core.Tests;

public class ServiceTypeMappingTests
{
    [Theory]
    [InlineData("_airplay._tcp", ServiceCategory.AirPlay)]
    [InlineData("_raop._tcp", ServiceCategory.AirPlay)]
    [InlineData("_googlecast._tcp", ServiceCategory.Cast)]
    [InlineData("_ipp._tcp", ServiceCategory.Print)]
    [InlineData("_printer._tcp", ServiceCategory.Print)]
    [InlineData("_hap._tcp", ServiceCategory.HomeKit)]
    [InlineData("_matter._tcp", ServiceCategory.HomeKit)]
    [InlineData("_smb._tcp", ServiceCategory.FileShare)]
    [InlineData("_ssh._tcp", ServiceCategory.RemoteControl)]
    [InlineData("_esphomelib._tcp", ServiceCategory.IoT)]
    [InlineData("_workstation._tcp", ServiceCategory.Other)]
    public void Categorize_KnownServiceType_ReturnsExpectedCategory(string serviceType, ServiceCategory expected)
    {
        ServiceTypeMapping.Categorize(serviceType).Should().Be(expected);
    }

    [Theory]
    [InlineData("_airplay._tcp.local")]
    [InlineData("_airplay._tcp.local.")]
    public void Categorize_TolerantOfLocalSuffixAndTrailingDot(string input)
    {
        ServiceTypeMapping.Categorize(input).Should().Be(ServiceCategory.AirPlay);
    }

    [Theory]
    [InlineData("_AIRPLAY._TCP")]
    [InlineData("_AirPlay._Tcp")]
    public void Categorize_IsCaseInsensitive(string input)
    {
        ServiceTypeMapping.Categorize(input).Should().Be(ServiceCategory.AirPlay);
    }

    [Fact]
    public void Categorize_UnknownServiceType_FallsBackToOther()
    {
        ServiceTypeMapping.Categorize("_madeup._tcp").Should().Be(ServiceCategory.Other);
    }

    [Fact]
    public void All_ContainsManyEntries()
    {
        // The JSON resource ships with at least the major categories.
        ServiceTypeMapping.All.Should().HaveCountGreaterThan(20);
    }
}
