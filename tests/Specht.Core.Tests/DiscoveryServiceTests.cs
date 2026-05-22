using FluentAssertions;
using Specht.Core.Services;
using Xunit;

namespace Specht.Core.Tests;

public class DiscoveryServiceTests
{
    [Fact]
    public void Lifecycle_StartStop_DoesNotThrow()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        sut.IsRunning.Should().BeFalse();
        sut.Start();
        sut.IsRunning.Should().BeTrue();
        sut.AnswersReceived.Should().BeGreaterOrEqualTo(0);
        sut.StartedAt.Should().BeAfter(DateTimeOffset.MinValue);

        sut.Stop();
        sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void Refresh_ResetsCounters()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        sut.Start();
        try
        {
            sut.Refresh();
            sut.AnswersReceived.Should().Be(0);
        }
        finally
        {
            sut.Stop();
        }
    }

    [Fact]
    public void Start_CalledTwice_IsIdempotent()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        sut.Start();
        try
        {
            sut.Start(); // second call should be a no-op, not throw
            sut.IsRunning.Should().BeTrue();
        }
        finally
        {
            sut.Stop();
        }
    }

    [Fact]
    public void Stop_OnFreshInstance_IsIdempotent()
    {
        var cache = new DeviceCache();
        var sut = new DiscoveryService(cache);

        // Stopping before Start should not throw.
        sut.Stop();
        sut.IsRunning.Should().BeFalse();
    }
}
