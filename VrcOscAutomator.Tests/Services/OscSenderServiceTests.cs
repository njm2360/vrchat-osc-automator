using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.Services;
using Xunit;

namespace VrcOscAutomator.Tests.Services;

public class OscSenderServiceTests
{
    [Fact]
    public void SetTargets_WithInvalidIp_DoesNotThrow()
    {
        using var sut = new OscSenderService();

        Action act = () => sut.SetTargets([
            new OscTarget { IpAddress = "not-an-ip", Port = 9000, IsEnabled = true },
        ]);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetTargets_WithMixedValidAndInvalidIps_DoesNotThrow()
    {
        using var sut = new OscSenderService();

        Action act = () => sut.SetTargets([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000, IsEnabled = true },
            new OscTarget { IpAddress = "999.999.999.999", Port = 9001, IsEnabled = true },
            new OscTarget { IpAddress = "", Port = 9002, IsEnabled = true },
        ]);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetTargets_AllValid_DoesNotThrow()
    {
        using var sut = new OscSenderService();

        Action act = () => sut.SetTargets([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000, IsEnabled = true },
            new OscTarget { IpAddress = "::1", Port = 9001, IsEnabled = true },
        ]);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(65536)]
    [InlineData(999999)]
    public void SetTargets_WithOutOfRangePort_DoesNotThrow(int port)
    {
        using var sut = new OscSenderService();

        Action act = () => sut.SetTargets([
            new OscTarget { IpAddress = "127.0.0.1", Port = port, IsEnabled = true },
        ]);

        act.Should().NotThrow();
    }

    [Fact]
    public void SetTargets_InvalidButDisabled_IsIgnored_DoesNotThrow()
    {
        using var sut = new OscSenderService();

        Action act = () => sut.SetTargets([
            new OscTarget { IpAddress = "garbage", Port = 9000, IsEnabled = false },
        ]);

        act.Should().NotThrow();
    }
}
