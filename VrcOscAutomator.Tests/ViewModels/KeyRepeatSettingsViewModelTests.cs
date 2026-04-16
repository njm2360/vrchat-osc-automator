using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;

namespace VrcOscAutomator.Tests.ViewModels;

public class KeyRepeatSettingsViewModelTests
{
    // ─── デフォルト値 ────────────────────────────────────────────────────

    [Fact]
    public void Default_IsEnabled_True()
    {
        var vm = new KeyRepeatSettingsViewModel();
        vm.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Default_InitialDelayMs_Zero()
    {
        var vm = new KeyRepeatSettingsViewModel();
        vm.InitialDelayMs.Should().Be(0);
    }

    [Fact]
    public void Default_IntervalMs_33()
    {
        var vm = new KeyRepeatSettingsViewModel();
        vm.IntervalMs.Should().Be(33);
    }

    // ─── LoadFromSettings ─────────────────────────────────────────────────

    [Fact]
    public void LoadFromSettings_SetsAllProperties()
    {
        var vm = new KeyRepeatSettingsViewModel();
        var settings = new KeyRepeatSettings { IsEnabled = false, InitialDelayMs = 500, IntervalMs = 100 };

        vm.LoadFromSettings(settings);

        vm.IsEnabled.Should().BeFalse();
        vm.InitialDelayMs.Should().Be(500);
        vm.IntervalMs.Should().Be(100);
    }

    [Fact]
    public void LoadFromSettings_Enabled_SetsIsEnabledTrue()
    {
        var vm = new KeyRepeatSettingsViewModel();
        vm.LoadFromSettings(new KeyRepeatSettings { IsEnabled = true });
        vm.IsEnabled.Should().BeTrue();
    }

    // ─── ToSettings ──────────────────────────────────────────────────────

    [Fact]
    public void ToSettings_ReturnsCorrectValues()
    {
        var vm = new KeyRepeatSettingsViewModel
        {
            IsEnabled = false,
            InitialDelayMs = 300,
            IntervalMs = 50,
        };

        KeyRepeatSettings result = vm.ToSettings();

        result.IsEnabled.Should().BeFalse();
        result.InitialDelayMs.Should().Be(300);
        result.IntervalMs.Should().Be(50);
    }

    // ─── ラウンドトリップ ─────────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(RoundTripData))]
    public void RoundTrip_LoadFromSettings_ToSettings(KeyRepeatSettings original)
    {
        var vm = new KeyRepeatSettingsViewModel();
        vm.LoadFromSettings(original);
        KeyRepeatSettings restored = vm.ToSettings();

        restored.IsEnabled.Should().Be(original.IsEnabled);
        restored.InitialDelayMs.Should().Be(original.InitialDelayMs);
        restored.IntervalMs.Should().Be(original.IntervalMs);
    }

    public static TheoryData<KeyRepeatSettings> RoundTripData => new()
    {
        new KeyRepeatSettings { IsEnabled = true,  InitialDelayMs = 0,   IntervalMs = 33  },
        new KeyRepeatSettings { IsEnabled = false, InitialDelayMs = 0,   IntervalMs = 33  },
        new KeyRepeatSettings { IsEnabled = true,  InitialDelayMs = 500, IntervalMs = 100 },
        new KeyRepeatSettings { IsEnabled = true,  InitialDelayMs = 0,   IntervalMs = 10  },
    };

    // ─── SliderEnabled ────────────────────────────────────────────────────

    [Fact]
    public void SliderEnabled_WhenIsEnabled_True()
    {
        var vm = new KeyRepeatSettingsViewModel { IsEnabled = true };
        vm.SliderEnabled.Should().BeTrue();
    }

    [Fact]
    public void SliderEnabled_WhenNotEnabled_False()
    {
        var vm = new KeyRepeatSettingsViewModel { IsEnabled = false };
        vm.SliderEnabled.Should().BeFalse();
    }

    [Fact]
    public void SliderEnabled_ChangesWithIsEnabled()
    {
        var vm = new KeyRepeatSettingsViewModel { IsEnabled = true };
        vm.IsEnabled = false;
        vm.SliderEnabled.Should().BeFalse();
        vm.IsEnabled = true;
        vm.SliderEnabled.Should().BeTrue();
    }

    // ─── InitialDelaySummary ──────────────────────────────────────────────

    [Fact]
    public void InitialDelaySummary_WhenZero_ShowsNoneMessage()
    {
        var vm = new KeyRepeatSettingsViewModel { InitialDelayMs = 0 };
        vm.InitialDelaySummary.Should().Contain("なし");
    }

    [Fact]
    public void InitialDelaySummary_WhenNonZero_ShowsMsValue()
    {
        var vm = new KeyRepeatSettingsViewModel { InitialDelayMs = 300 };
        vm.InitialDelaySummary.Should().Contain("300");
    }

    // ─── IntervalRatePerSecond ────────────────────────────────────────────

    [Fact]
    public void Default_IntervalRatePerSecond_IsCorrect()
    {
        // デフォルト IntervalMs=33 → 1000/33 = 30
        var vm = new KeyRepeatSettingsViewModel();
        vm.IntervalRatePerSecond.Should().Be(30);
    }

    [Theory]
    [InlineData(10,  100)]  // 10ms → 100回/秒
    [InlineData(50,   20)]  // 50ms → 20回/秒
    [InlineData(100,  10)]  // 100ms → 10回/秒
    [InlineData(200,   5)]  // 200ms → 5回/秒
    public void IntervalRatePerSecond_Get_ReturnsRateForMs(int ms, int expectedRate)
    {
        var vm = new KeyRepeatSettingsViewModel { IntervalMs = ms };
        vm.IntervalRatePerSecond.Should().Be(expectedRate);
    }

    [Theory]
    [InlineData(100, 10)]  // 100回/秒 → 10ms
    [InlineData(20,  50)]  // 20回/秒 → 50ms
    [InlineData(10, 100)]  // 10回/秒 → 100ms
    [InlineData(5,  200)]  // 5回/秒 → 200ms
    public void IntervalRatePerSecond_Set_UpdatesIntervalMs(int rate, int expectedMs)
    {
        var vm = new KeyRepeatSettingsViewModel();
        vm.IntervalRatePerSecond = rate;
        vm.IntervalMs.Should().Be(expectedMs);
    }

    [Fact]
    public void IntervalRatePerSecond_Set_NotifiesIntervalMs()
    {
        var vm = new KeyRepeatSettingsViewModel();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IntervalRatePerSecond = 20;

        changed.Should().Contain(nameof(vm.IntervalMs));
    }

    [Fact]
    public void IntervalMs_Set_NotifiesIntervalRatePerSecond()
    {
        var vm = new KeyRepeatSettingsViewModel();
        var changed = new List<string?>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        vm.IntervalMs = 50;

        changed.Should().Contain(nameof(vm.IntervalRatePerSecond));
    }

    // ─── IntervalSummary ──────────────────────────────────────────────────

    [Fact]
    public void IntervalSummary_ContainsRate()
    {
        // 1000 / 50ms = 20 回/秒
        var vm = new KeyRepeatSettingsViewModel { IntervalMs = 50 };
        vm.IntervalSummary.Should().Contain("20");
    }

    [Fact]
    public void IntervalSummary_ChangesWhenIntervalMsChanges()
    {
        var vm = new KeyRepeatSettingsViewModel { IntervalMs = 33 };
        string first = vm.IntervalSummary;
        vm.IntervalMs = 100;
        vm.IntervalSummary.Should().NotBe(first);
    }
}
