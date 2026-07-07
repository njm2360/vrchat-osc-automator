using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;

namespace VrcOscAutomator.Tests.ViewModels;

public class SendTargetsViewModelTests
{
    private readonly SendTargetsViewModel _sut = new();

    // ─── AddTarget ────────────────────────────────────────────────────────

    [Fact]
    public void AddTarget_AppendsNewTargetWithDefaults()
    {
        _sut.AddTargetCommand.Execute(null);

        _sut.Targets.Should().ContainSingle();
        _sut.Targets[0].IpAddress.Should().Be("127.0.0.1");
        _sut.Targets[0].Port.Should().Be(9000);
        _sut.Targets[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void AddTarget_MultipleTimes_AddsEach()
    {
        _sut.AddTargetCommand.Execute(null);
        _sut.AddTargetCommand.Execute(null);

        _sut.Targets.Should().HaveCount(2);
    }

    // ─── RemoveTarget ─────────────────────────────────────────────────────

    [Fact]
    public void RemoveTarget_CanExecute_FalseWhenNothingSelected()
    {
        _sut.AddTargetCommand.Execute(null);

        _sut.RemoveTargetCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void RemoveTarget_CanExecute_TrueWhenTargetSelected()
    {
        _sut.AddTargetCommand.Execute(null);
        _sut.SelectedTarget = _sut.Targets[0];

        _sut.RemoveTargetCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void RemoveTarget_RemovesSelectedTarget()
    {
        _sut.AddTargetCommand.Execute(null);
        _sut.AddTargetCommand.Execute(null);
        OscTargetViewModel first = _sut.Targets[0];
        _sut.SelectedTarget = first;

        _sut.RemoveTargetCommand.Execute(null);

        _sut.Targets.Should().NotContain(first);
        _sut.Targets.Should().HaveCount(1);
    }

    // ─── LoadFromModels / ToModels ────────────────────────────────────────

    [Fact]
    public void LoadFromModels_ReplacesExistingTargets()
    {
        _sut.AddTargetCommand.Execute(null); // 既存の1件

        var models = new[]
        {
            new OscTarget { IpAddress = "10.0.0.1", Port = 8000, IsEnabled = true },
            new OscTarget { IpAddress = "10.0.0.2", Port = 8001, IsEnabled = false },
        };

        _sut.LoadFromModels(models);

        _sut.Targets.Should().HaveCount(2);
        _sut.Targets[0].IpAddress.Should().Be("10.0.0.1");
        _sut.Targets[1].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void ToModels_ReturnsModelsMatchingTargets()
    {
        var models = new[]
        {
            new OscTarget { IpAddress = "192.168.1.1", Port = 9000, IsEnabled = true },
            new OscTarget { IpAddress = "192.168.1.2", Port = 9001, IsEnabled = false },
        };
        _sut.LoadFromModels(models);

        List<OscTarget> result = _sut.ToModels();

        result.Should().HaveCount(2);
        result[0].IpAddress.Should().Be("192.168.1.1");
        result[1].Port.Should().Be(9001);
        result[1].IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void LoadFromModels_Empty_ClearsTargets()
    {
        _sut.AddTargetCommand.Execute(null);

        _sut.LoadFromModels([]);

        _sut.Targets.Should().BeEmpty();
    }

    // ─── GetDuplicateError ────────────────────────────────────────────────

    [Fact]
    public void GetDuplicateError_NoDuplicates_ReturnsNull()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
            new OscTarget { IpAddress = "127.0.0.1", Port = 9001 },
        ]);

        _sut.GetDuplicateError().Should().BeNull();
    }

    [Fact]
    public void GetDuplicateError_SameIpAndPort_ReturnsErrorMessage()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
        ]);

        string? error = _sut.GetDuplicateError();

        error.Should().NotBeNull();
        error.Should().Contain("127.0.0.1:9000");
    }

    [Fact]
    public void GetDuplicateError_MultipleDuplicatePairs_ReportsAll()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
            new OscTarget { IpAddress = "10.0.0.1",  Port = 8000 },
            new OscTarget { IpAddress = "10.0.0.1",  Port = 8000 },
        ]);

        string? error = _sut.GetDuplicateError();

        error.Should().Contain("127.0.0.1:9000");
        error.Should().Contain("10.0.0.1:8000");
    }

    [Fact]
    public void GetDuplicateError_SameIpDifferentPort_ReturnsNull()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
            new OscTarget { IpAddress = "127.0.0.1", Port = 9001 },
            new OscTarget { IpAddress = "127.0.0.1", Port = 9002 },
        ]);

        _sut.GetDuplicateError().Should().BeNull();
    }

    [Fact]
    public void GetDuplicateError_IpAddressWithTrailingSpace_TreatedAsDuplicate()
    {
        // GetDuplicateError は IpAddress.Trim() で比較するため、前後スペースは無視される
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "127.0.0.1 ", Port = 9000 },
            new OscTarget { IpAddress = "127.0.0.1",  Port = 9000 },
        ]);

        _sut.GetDuplicateError().Should().NotBeNull();
    }

    [Fact]
    public void GetDuplicateError_EmptyList_ReturnsNull()
    {
        _sut.GetDuplicateError().Should().BeNull();
    }

    // ─── GetInvalidIpError ────────────────────────────────────────────────

    [Fact]
    public void GetInvalidIpError_AllValidIpv4_ReturnsNull()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
            new OscTarget { IpAddress = "192.168.1.100", Port = 9001 },
        ]);

        _sut.GetInvalidIpError().Should().BeNull();
    }

    [Fact]
    public void GetInvalidIpError_ValidIpv6_ReturnsNull()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "::1", Port = 9000 },
            new OscTarget { IpAddress = "fe80::1", Port = 9001 },
        ]);

        _sut.GetInvalidIpError().Should().BeNull();
    }

    [Fact]
    public void GetInvalidIpError_MalformedIp_ReturnsErrorMessage()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "not-an-ip", Port = 9000 },
        ]);

        string? error = _sut.GetInvalidIpError();

        error.Should().NotBeNull();
        error.Should().Contain("not-an-ip");
    }

    [Fact]
    public void GetInvalidIpError_OutOfRangeOctet_ReturnsErrorMessage()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "999.1.1.1", Port = 9000 },
        ]);

        _sut.GetInvalidIpError().Should().NotBeNull();
    }

    [Fact]
    public void GetInvalidIpError_EmptyAddress_ReturnsErrorMessage()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "", Port = 9000 },
        ]);

        string? error = _sut.GetInvalidIpError();

        error.Should().NotBeNull();
        error.Should().Contain("(空欄)");
    }

    [Fact]
    public void GetInvalidIpError_ReportsAllInvalidRows()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = "127.0.0.1", Port = 9000 },
            new OscTarget { IpAddress = "bad1", Port = 9001 },
            new OscTarget { IpAddress = "bad2", Port = 9002 },
        ]);

        string? error = _sut.GetInvalidIpError();

        error.Should().Contain("bad1");
        error.Should().Contain("bad2");
    }

    [Fact]
    public void GetInvalidIpError_TrimsSurroundingWhitespace()
    {
        _sut.LoadFromModels([
            new OscTarget { IpAddress = " 127.0.0.1 ", Port = 9000 },
        ]);

        _sut.GetInvalidIpError().Should().BeNull();
    }

    [Fact]
    public void GetInvalidIpError_EmptyList_ReturnsNull()
    {
        _sut.GetInvalidIpError().Should().BeNull();
    }
}
