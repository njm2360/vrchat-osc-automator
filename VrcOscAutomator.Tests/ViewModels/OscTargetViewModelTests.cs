using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;

namespace VrcOscAutomator.Tests.ViewModels;

public class OscTargetViewModelTests
{
    [Fact]
    public void ToModel_ReturnsModelWithSameValues()
    {
        var vm = new OscTargetViewModel
        {
            IpAddress = "192.168.1.10",
            Port = 9001,
            IsEnabled = false,
        };

        OscTarget model = vm.ToModel();

        model.IpAddress.Should().Be("192.168.1.10");
        model.Port.Should().Be(9001);
        model.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void FromModel_ReturnsViewModelWithSameValues()
    {
        var model = new OscTarget { IpAddress = "10.0.0.1", Port = 1234, IsEnabled = true };

        OscTargetViewModel vm = OscTargetViewModel.FromModel(model);

        vm.IpAddress.Should().Be("10.0.0.1");
        vm.Port.Should().Be(1234);
        vm.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var vm = new OscTargetViewModel();

        vm.IpAddress.Should().Be("127.0.0.1");
        vm.Port.Should().Be(9000);
        vm.IsEnabled.Should().BeTrue();
    }
}
