using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;

namespace VrcOscAutomator.Tests.ViewModels;

public class SequenceSlotViewModelTests
{
    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static BuiltinPreset FloatPreset => SlotPreset.All.OfType<BuiltinPreset>().First(p => p.IsBuiltinFloat);
    private static SlotPreset IntPreset => SlotPreset.All.First(p => p.IsBuiltinInt);
    private static SlotPreset CustomPreset => SlotPreset.All.First(p => p.IsCustom);
    private static SlotPreset WaitPreset => SlotPreset.All.First(p => p.IsWait);
    private static SlotPreset LoopBeginPreset => SlotPreset.All.First(p => p.IsLoopBegin);
    private static SlotPreset LoopEndPreset => SlotPreset.All.First(p => p.IsLoopEnd);
    private static SlotPreset BreakpointPreset => SlotPreset.All.First(p => p.IsBreakpoint);
    private static SlotPreset RandomWaitPreset => SlotPreset.All.First(p => p.IsRandomWait);

    // ─── IsValid ──────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_NonCustomPreset_AlwaysTrue()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = FloatPreset };
        vm.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_CustomPreset_AddressStartsWithSlash_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/my/param",
        };
        vm.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_CustomPreset_AddressEmpty_False()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "",
        };
        vm.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_CustomPreset_AddressNoSlash_False()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "param",
        };
        vm.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_CustomPreset_AddressOnlySlash_False()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/",
        };
        vm.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("/my param")]   // スペース
    [InlineData("/my#param")]   // #
    [InlineData("/my*param")]   // *
    [InlineData("/my,param")]   // ,
    [InlineData("/my?param")]   // ?
    [InlineData("/my[param")]   // [
    [InlineData("/my]param")]   // ]
    [InlineData("/my{param")]   // {
    [InlineData("/my}param")]   // }
    public void IsValid_CustomPreset_AddressContainsForbiddenChar_False(string address)
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = address,
        };
        vm.IsValid.Should().BeFalse();
    }

    // ─── IsValid (RandomWait) ────────────────────────────────────────────

    [Fact]
    public void IsValid_RandomWait_MinLessThanMax_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = RandomWaitPreset,
            RandomWaitMinMs = 300,
            RandomWaitMaxMs = 1000,
        };
        vm.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_RandomWait_MinEqualsMax_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = RandomWaitPreset,
            RandomWaitMinMs = 500,
            RandomWaitMaxMs = 500,
        };
        vm.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_RandomWait_MinGreaterThanMax_False()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = RandomWaitPreset,
            RandomWaitMinMs = 1000,
            RandomWaitMaxMs = 300,
        };
        vm.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_RandomWait_NegativeMin_False()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = RandomWaitPreset,
            RandomWaitMinMs = -1,
            RandomWaitMaxMs = 1000,
        };
        vm.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_RandomWait_ChangingMaxRaisesIsValidNotification()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = RandomWaitPreset,
            RandomWaitMinMs = 300,
            RandomWaitMaxMs = 1000,
        };
        bool raised = false;
        vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(vm.IsValid)) raised = true; };

        vm.RandomWaitMaxMs = 100; // Min > Max にして無効化

        raised.Should().BeTrue();
        vm.IsValid.Should().BeFalse();
    }

    // ─── 値プロパティの独立性 ─────────────────────────────────────────────

    [Fact]
    public void ValueProperties_AreIndependent()
    {
        var vm = new SequenceSlotViewModel
        {
            FloatValue = 0.75f,
            IntValue = 1,
            BoolValue = true,
        };

        vm.FloatValue.Should().Be(0.75f);
        vm.IntValue.Should().Be(1);
        vm.BoolValue.Should().BeTrue();
    }

    [Fact]
    public void CustomValueType_ChangingType_DoesNotAffectOtherValues()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            FloatValue = 0.75f,
            IntValue = 2,
        };

        vm.CustomValueType = OscValueType.Int;
        vm.FloatValue.Should().Be(0.75f);
        vm.IntValue.Should().Be(2);
    }

    // ─── モードフラグ ─────────────────────────────────────────────────────

    [Fact]
    public void IsFloatMode_FloatMovementPreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = FloatPreset };
        vm.IsFloatMode.Should().BeTrue();
        vm.IsIntMode.Should().BeFalse();
        vm.IsBoolMode.Should().BeFalse();
        vm.IsStringMode.Should().BeFalse();
    }

    [Fact]
    public void IsIntMode_IntMovementPreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = IntPreset };
        vm.IsIntMode.Should().BeTrue();
        vm.IsFloatMode.Should().BeFalse();
    }

    [Fact]
    public void IsFloatMode_CustomWithFloat_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomValueType = OscValueType.Float,
        };
        vm.IsFloatMode.Should().BeTrue();
    }

    [Fact]
    public void IsIntMode_CustomWithInt_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomValueType = OscValueType.Int,
        };
        vm.IsIntMode.Should().BeTrue();
        vm.IsFloatMode.Should().BeFalse();
    }

    [Fact]
    public void IsBoolMode_CustomWithBool_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomValueType = OscValueType.Bool,
        };
        vm.IsBoolMode.Should().BeTrue();
    }

    [Fact]
    public void IsStringMode_CustomWithString_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomValueType = OscValueType.String,
        };
        vm.IsStringMode.Should().BeTrue();
    }

    // ─── ShowResetOption / IsDurationEditable ────────────────────────────

    [Fact]
    public void ShowResetOption_FloatMovementPreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = FloatPreset };
        vm.ShowResetOption.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(NoResetOptionPresets))]
    public void ShowResetOption_SpecialPresets_False(SlotPreset preset)
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = preset };
        vm.ShowResetOption.Should().BeFalse();
    }

    public static TheoryData<SlotPreset> NoResetOptionPresets => new()
    {
        SlotPreset.All.First(p => p.IsWait),
        SlotPreset.All.First(p => p.IsLoopBegin),
        SlotPreset.All.First(p => p.IsLoopEnd),
        SlotPreset.All.First(p => p.IsCustom),
    };

    [Theory]
    [MemberData(nameof(LoopMarkerPresets))]
    public void IsDurationEditable_LoopMarkers_False(SlotPreset preset)
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = preset };
        vm.IsDurationEditable.Should().BeFalse();
    }

    [Fact]
    public void IsDurationEditable_NormalPreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = FloatPreset };
        vm.IsDurationEditable.Should().BeTrue();
    }

    public static TheoryData<SlotPreset> LoopMarkerPresets => new()
    {
        SlotPreset.All.First(p => p.IsLoopBegin),
        SlotPreset.All.First(p => p.IsLoopEnd),
    };

    [Fact]
    public void IsDurationEditable_Breakpoint_False()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = BreakpointPreset };
        vm.IsDurationEditable.Should().BeFalse();
    }

    // ─── ParameterSummary ────────────────────────────────────────────────

    [Fact]
    public void ParameterSummary_LoopBegin_ShowsRepeatCount()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = LoopBeginPreset, RepeatCount = 4 };
        vm.ParameterSummary.Should().Be("x 4 回");
    }

    [Fact]
    public void ParameterSummary_LoopEnd_ShowsDash()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = LoopEndPreset };
        vm.ParameterSummary.Should().Be("—");
    }

    [Fact]
    public void ParameterSummary_Wait_ShowsDash()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = WaitPreset };
        vm.ParameterSummary.Should().Be("—");
    }

    [Fact]
    public void ParameterSummary_Breakpoint_ShowsPauseIcon()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = BreakpointPreset };
        vm.ParameterSummary.Should().Be("—");
    }

    [Fact]
    public void ParameterSummary_IntPreset_ValueOne_ShowsOn()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = IntPreset, IntValue = 1 };
        vm.ParameterSummary.Should().Be("1 (ON)");
    }

    [Fact]
    public void ParameterSummary_IntPreset_ValueZero_ShowsOff()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = IntPreset, IntValue = 0 };
        vm.ParameterSummary.Should().Be("0 (OFF)");
    }

    [Fact]
    public void ParameterSummary_FloatPreset_ShowsFormattedValue()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = FloatPreset, FloatValue = 0.75f };
        vm.ParameterSummary.Should().Be("0.75");
    }

    [Fact]
    public void ParameterSummary_CustomWithAddress_ShowsAddressAndType()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/my/param",
            CustomValueType = OscValueType.Float,
            FloatValue = 0.5f,
        };
        vm.ParameterSummary.Should().Contain("/my/param").And.Contain("Float");
    }

    [Theory]
    [InlineData(0, "0")]
    [InlineData(1, "1")]
    [InlineData(2, "2")]
    public void ParameterSummary_CustomInt_ShowsNumber_NotOnOff(int value, string expected)
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/my/int",
            CustomValueType = OscValueType.Int,
            IntValue = value,
        };
        vm.ParameterSummary.Should().Contain(expected);
        vm.ParameterSummary.Should().NotContain("ON").And.NotContain("OFF");
    }

    [Theory]
    [InlineData(false, "false")]
    [InlineData(true, "true")]
    public void ParameterSummary_CustomBool_ShowsTrueOrFalse(bool value, string expected)
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/my/bool",
            CustomValueType = OscValueType.Bool,
            BoolValue = value,
        };
        vm.ParameterSummary.Should().Contain(expected);
    }

    [Fact]
    public void ParameterSummary_CustomString_ShowsQuotedValue()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/my/str",
            CustomValueType = OscValueType.String,
            StringValue = "hello",
        };
        vm.ParameterSummary.Should().Contain("\"hello\"");
    }

    [Fact]
    public void ParameterSummary_CustomNoAddress_ShowsUnset()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "",
        };
        vm.ParameterSummary.Should().Contain("アドレス未設定");
    }

    // ─── ToModel ──────────────────────────────────────────────────────────

    [Fact]
    public void ToModel_LoopBeginPreset_ReturnsLoopBeginSlot()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = LoopBeginPreset, RepeatCount = 3 };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<LoopBeginSlot>();
        ((LoopBeginSlot)model).RepeatCount.Should().Be(3);
    }

    [Fact]
    public void ToModel_LoopEndPreset_ReturnsLoopEndSlot()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = LoopEndPreset };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<LoopEndSlot>();
    }

    [Fact]
    public void ToModel_BreakpointPreset_ReturnsBreakpointSlot()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = BreakpointPreset };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<BreakpointSlot>();
    }

    [Fact]
    public void ToModel_WaitPreset_ReturnsWaitSlot()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = WaitPreset, DurationMs = 800 };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<WaitSlot>();
        ((WaitSlot)model).DurationMs.Should().Be(800);
    }

    [Fact]
    public void ToModel_FloatPreset_UsesPresetAddress()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = FloatPreset,
            FloatValue = 0.8f,
            DurationMs = 300,
            ResetOnComplete = false,
        };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<FloatSlot>();
        var f = (FloatSlot)model;
        f.Address.Should().Be(FloatPreset.Address);
        f.Value.Should().Be(0.8f);
        f.DurationMs.Should().Be(300);
        f.ResetOnComplete.Should().BeFalse();
    }

    [Fact]
    public void ToModel_CustomStringPreset_UsesCustomAddressAndStringValue()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/my/str",
            CustomValueType = OscValueType.String,
            StringValue = "hello",
        };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<StringSlot>();
        var s = (StringSlot)model;
        s.Address.Should().Be("/my/str");
        s.Value.Should().Be("hello");
    }

    [Fact]
    public void ToModel_CustomIntPreset_ReturnsIntSlot()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = CustomPreset,
            CustomAddress = "/my/int",
            CustomValueType = OscValueType.Int,
            IntValue = 1,
        };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<IntSlot>();
        ((IntSlot)model).Value.Should().Be(1);
    }

    // ─── FromModel ────────────────────────────────────────────────────────

    [Fact]
    public void FromModel_LoopBeginSlot_SelectsLoopBeginPreset()
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(new LoopBeginSlot(5));

        vm.SelectedPreset.IsLoopBegin.Should().BeTrue();
        vm.RepeatCount.Should().Be(5);
    }

    [Fact]
    public void FromModel_LoopEndSlot_SelectsLoopEndPreset()
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(new LoopEndSlot());

        vm.SelectedPreset.IsLoopEnd.Should().BeTrue();
    }

    [Fact]
    public void FromModel_BreakpointSlot_SelectsBreakpointPreset()
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(new BreakpointSlot());

        vm.SelectedPreset.IsBreakpoint.Should().BeTrue();
    }

    [Fact]
    public void FromModel_WaitSlot_SelectsWaitPreset()
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(new WaitSlot(500));

        vm.SelectedPreset.IsWait.Should().BeTrue();
        vm.DurationMs.Should().Be(500);
    }

    [Fact]
    public void FromModel_KnownAddress_SelectsMatchingPreset()
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(
            new FloatSlot("/input/Vertical", 0.5f, 500, true, TransitionMode.None));

        vm.SelectedPreset.Should().BeOfType<BuiltinPreset>()
            .Which.Address.Should().Be("/input/Vertical");
        vm.FloatValue.Should().Be(0.5f);
    }

    [Fact]
    public void FromModel_UnknownAddress_SelectsCustomPreset()
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(
            new IntSlot("/custom/unknown", 1, 500, true, TransitionMode.None));

        vm.SelectedPreset.IsCustom.Should().BeTrue();
        vm.CustomAddress.Should().Be("/custom/unknown");
        vm.CustomValueType.Should().Be(OscValueType.Int);
        vm.IntValue.Should().Be(1);
    }

    // ─── ToModel / FromModel ラウンドトリップ ─────────────────────────────

    [Theory]
    [MemberData(nameof(RoundTripSlots))]
    public void ToModel_FromModel_RoundTrip(SequenceSlot original)
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(original);
        SequenceSlot restored = vm.ToModel();

        // レコードの値等価性で比較（LoopEndSlot のようにメンバーがない型にも対応）
        restored.Should().Be(original);
    }

    public static TheoryData<SequenceSlot> RoundTripSlots => new()
    {
        new LoopBeginSlot(4),
        new LoopBeginSlot(0), // RepeatCount=0 は無限ループ
        new LoopEndSlot(),
        new WaitSlot(1000),
        new FloatSlot("/input/Vertical", 0.5f, 300, true,  TransitionMode.None),
        new IntSlot("/input/Jump",       1,    200, false, TransitionMode.None),
        new BoolSlot("/custom/x",        false, 100, true),
        new StringSlot("/custom/s",      "hi",  50,  true),
    };
}
