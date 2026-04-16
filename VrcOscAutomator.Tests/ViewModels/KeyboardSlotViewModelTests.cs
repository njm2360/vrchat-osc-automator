using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;
using KeyAction = VrcOscAutomator.Models.KeyAction;

namespace VrcOscAutomator.Tests.ViewModels;

/// <summary>キーボードスロット (KeySinglePreset / KeyTypeStringPreset) の ViewModel テスト。</summary>
public class KeyboardSlotViewModelTests
{
    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static SlotPreset KeySinglePreset    => SlotPreset.All.First(p => p.IsKeyboardSingle);
    private static SlotPreset KeyTypeStrPreset   => SlotPreset.All.First(p => p.IsKeyboardTypeString);
    private static VirtualKeyItem KeyEnter       => VirtualKeyItem.All.First(k => k.Code == 0x0D);
    private static VirtualKeyItem KeyA           => VirtualKeyItem.All.First(k => k.Code == 0x41);

    // ─── SlotPreset フラグ ────────────────────────────────────────────────

    [Fact]
    public void SlotPreset_IsKeyboardSingle_OnlyKeyboardSinglePreset()
    {
        KeySinglePreset.IsKeyboardSingle.Should().BeTrue();
        KeyTypeStrPreset.IsKeyboardSingle.Should().BeFalse();
        SlotPreset.All.First(p => p.IsWait).IsKeyboardSingle.Should().BeFalse();
    }

    [Fact]
    public void SlotPreset_IsKeyboardTypeString_OnlyKeyTypeStringPreset()
    {
        KeyTypeStrPreset.IsKeyboardTypeString.Should().BeTrue();
        KeySinglePreset.IsKeyboardTypeString.Should().BeFalse();
        SlotPreset.All.First(p => p.IsWait).IsKeyboardTypeString.Should().BeFalse();
    }

    // ─── IsKeyboardSingleMode / IsKeyboardTypeStringMode ─────────────────

    [Fact]
    public void IsKeyboardSingleMode_WhenKeyboardSinglePreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.IsKeyboardSingleMode.Should().BeTrue();
        vm.IsKeyboardTypeStringMode.Should().BeFalse();
    }

    [Fact]
    public void IsKeyboardTypeStringMode_WhenKeyTypeStringPreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeyTypeStrPreset };
        vm.IsKeyboardTypeStringMode.Should().BeTrue();
        vm.IsKeyboardSingleMode.Should().BeFalse();
    }

    [Fact]
    public void IsKeyboardSingleMode_WhenOtherPreset_False()
    {
        var vm = new SequenceSlotViewModel();  // デフォルトは WaitPreset
        vm.IsKeyboardSingleMode.Should().BeFalse();
        vm.IsKeyboardTypeStringMode.Should().BeFalse();
    }

    // ─── IsKeyActionPress / IsKeyActionRelease ────────────────────────────

    [Fact]
    public void IsKeyActionPress_Default_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.IsKeyActionPress.Should().BeTrue();
        vm.IsKeyActionRelease.Should().BeFalse();
    }

    [Fact]
    public void IsKeyActionRelease_SetToRelease_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKeyAction = KeyAction.Release,
        };
        vm.IsKeyActionRelease.Should().BeTrue();
        vm.IsKeyActionPress.Should().BeFalse();
    }

    [Fact]
    public void IsKeyActionPress_Setter_UpdatesSelectedKeyAction()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset, SelectedKeyAction = KeyAction.Release };
        vm.IsKeyActionPress = true;
        vm.SelectedKeyAction.Should().Be(KeyAction.Press);
    }

    [Fact]
    public void IsKeyActionRelease_Setter_UpdatesSelectedKeyAction()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.IsKeyActionRelease = true;
        vm.SelectedKeyAction.Should().Be(KeyAction.Release);
    }

    // ─── IsDurationEditable ───────────────────────────────────────────────

    [Fact]
    public void IsDurationEditable_KeySinglePreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.IsDurationEditable.Should().BeTrue();
    }

    [Fact]
    public void IsDurationEditable_KeyTypeStringPreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeyTypeStrPreset };
        vm.IsDurationEditable.Should().BeTrue();
    }

    // ─── ShowResetOption ──────────────────────────────────────────────────

    [Fact]
    public void ShowResetOption_KeySinglePreset_False()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.ShowResetOption.Should().BeFalse();
    }

    [Fact]
    public void ShowResetOption_KeyTypeStringPreset_False()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeyTypeStrPreset };
        vm.ShowResetOption.Should().BeFalse();
    }

    // ─── IsValid ──────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_KeySinglePreset_AlwaysTrue()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_KeyTypeStringPreset_AlwaysTrue()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeyTypeStrPreset };
        vm.IsValid.Should().BeTrue();
    }

    // ─── ParameterSummary ────────────────────────────────────────────────

    [Fact]
    public void ParameterSummary_KeySingle_Press_ShowsKeyNameAndPress()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKey = KeyEnter,
            SelectedKeyAction = KeyAction.Press,
        };
        vm.ParameterSummary.Should().Be("Enter [押す]");
    }

    [Fact]
    public void ParameterSummary_KeySingle_Release_ShowsKeyNameAndRelease()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKey = KeyA,
            SelectedKeyAction = KeyAction.Release,
        };
        vm.ParameterSummary.Should().Be("A [離す]");
    }

    [Fact]
    public void ParameterSummary_KeyTypeString_EmptyText_ShowsUnset()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeyTypeStrPreset, TypeText = "" };
        vm.ParameterSummary.Should().Be("(未入力)");
    }

    [Fact]
    public void ParameterSummary_KeyTypeString_ShortText_ShowsQuoted()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeyTypeStrPreset, TypeText = "hello" };
        vm.ParameterSummary.Should().Be("\"hello\"");
    }

    [Fact]
    public void ParameterSummary_KeyTypeString_AppendNewline_ShowsNewlineMarker()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeyTypeStrPreset,
            TypeText = "hello",
            AppendNewline = true,
        };
        vm.ParameterSummary.Should().Contain("↵");
    }

    [Fact]
    public void ParameterSummary_KeyTypeString_LongText_Truncated()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeyTypeStrPreset,
            TypeText = new string('a', 30),
        };
        vm.ParameterSummary.Should().Contain("…");
        vm.ParameterSummary.Length.Should().BeLessThan(40); // 30文字 + クォート等で爆発しない
    }

    // ─── ToModel ──────────────────────────────────────────────────────────

    [Fact]
    public void ToModel_KeySinglePreset_ReturnsKeySingleSlot()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKey = KeyEnter,
            SelectedKeyAction = KeyAction.Press,
            DurationMs = 200,
        };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<KeySingleSlot>();
        var ks = (KeySingleSlot)model;
        ks.VirtualKey.Should().Be(0x0D);
        ks.Action.Should().Be(KeyAction.Press);
        ks.DurationMs.Should().Be(200);
    }

    [Fact]
    public void ToModel_KeySinglePreset_Release_ReturnsReleaseAction()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKey = KeyA,
            SelectedKeyAction = KeyAction.Release,
            DurationMs = 0,
        };

        var model = (KeySingleSlot)vm.ToModel();
        model.Action.Should().Be(KeyAction.Release);
        model.VirtualKey.Should().Be(0x41);
    }

    [Fact]
    public void ToModel_KeyTypeStringPreset_ReturnsKeyTypeStringSlot()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeyTypeStrPreset,
            TypeText = "hello",
            AppendNewline = true,
            DurationMs = 300,
        };

        SequenceSlot model = vm.ToModel();

        model.Should().BeOfType<KeyTypeStringSlot>();
        var kts = (KeyTypeStringSlot)model;
        kts.Text.Should().Be("hello");
        kts.AppendNewline.Should().BeTrue();
        kts.DurationMs.Should().Be(300);
    }

    [Fact]
    public void ToModel_KeyTypeStringPreset_EmptyText_NoNewline_CorrectDefaults()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeyTypeStrPreset };

        var model = (KeyTypeStringSlot)vm.ToModel();
        model.Text.Should().Be("");
        model.AppendNewline.Should().BeFalse();
        model.DurationMs.Should().Be(500); // ViewModel のデフォルト DurationMs
    }

    // ─── FromModel ────────────────────────────────────────────────────────

    [Fact]
    public void FromModel_KeySingleSlot_SelectsKeySinglePreset()
    {
        var slot = new KeySingleSlot(0x0D, KeyAction.Press, 150);

        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(slot);

        vm.SelectedPreset.IsKeyboardSingle.Should().BeTrue();
        vm.SelectedKey.Code.Should().Be(0x0D);
        vm.SelectedKeyAction.Should().Be(KeyAction.Press);
        vm.DurationMs.Should().Be(150);
    }

    [Fact]
    public void FromModel_KeySingleSlot_UnknownVKey_FallsBackToFirstKey()
    {
        var slot = new KeySingleSlot(0xFF, KeyAction.Release, 0); // 未定義の VK コード

        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(slot);

        vm.SelectedKey.Should().Be(VirtualKeyItem.All[0]);
        vm.SelectedKeyAction.Should().Be(KeyAction.Release);
    }

    [Fact]
    public void FromModel_KeyTypeStringSlot_SelectsKeyTypeStringPreset()
    {
        var slot = new KeyTypeStringSlot("world", AppendNewline: true, DurationMs: 400);

        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(slot);

        vm.SelectedPreset.IsKeyboardTypeString.Should().BeTrue();
        vm.TypeText.Should().Be("world");
        vm.AppendNewline.Should().BeTrue();
        vm.DurationMs.Should().Be(400);
    }

    // ─── IsKeyActionPressAndRelease ───────────────────────────────────────

    [Fact]
    public void IsKeyActionPressAndRelease_Default_False()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.IsKeyActionPressAndRelease.Should().BeFalse();
    }

    [Fact]
    public void IsKeyActionPressAndRelease_SetToPressAndRelease_TrueOthersFalse()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKeyAction = KeyAction.PressAndRelease,
        };
        vm.IsKeyActionPressAndRelease.Should().BeTrue();
        vm.IsKeyActionPress.Should().BeFalse();
        vm.IsKeyActionRelease.Should().BeFalse();
    }

    [Fact]
    public void IsKeyActionPressAndRelease_Setter_UpdatesSelectedKeyAction()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = KeySinglePreset };
        vm.IsKeyActionPressAndRelease = true;
        vm.SelectedKeyAction.Should().Be(KeyAction.PressAndRelease);
    }

    [Fact]
    public void ParameterSummary_KeySingle_PressAndRelease_ShowsLabel()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKey = KeyEnter,
            SelectedKeyAction = KeyAction.PressAndRelease,
        };
        vm.ParameterSummary.Should().Be("Enter [押して離す]");
    }

    [Fact]
    public void ToModel_KeySinglePreset_PressAndRelease_ReturnsPressAndReleaseAction()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = KeySinglePreset,
            SelectedKey = KeyA,
            SelectedKeyAction = KeyAction.PressAndRelease,
            DurationMs = 100,
        };

        var model = (KeySingleSlot)vm.ToModel();
        model.Action.Should().Be(KeyAction.PressAndRelease);
        model.VirtualKey.Should().Be(0x41);
        model.DurationMs.Should().Be(100);
    }

    [Fact]
    public void FromModel_KeySingleSlot_PressAndRelease_RestoresAction()
    {
        var slot = new KeySingleSlot(0x41, KeyAction.PressAndRelease, 200);

        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(slot);

        vm.SelectedKeyAction.Should().Be(KeyAction.PressAndRelease);
        vm.IsKeyActionPressAndRelease.Should().BeTrue();
        vm.DurationMs.Should().Be(200);
    }

    // ─── ToModel / FromModel ラウンドトリップ ─────────────────────────────

    [Theory]
    [MemberData(nameof(KeyboardRoundTripSlots))]
    public void ToModel_FromModel_RoundTrip(SequenceSlot original)
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(original);
        SequenceSlot restored = vm.ToModel();

        restored.Should().Be(original);
    }

    public static TheoryData<SequenceSlot> KeyboardRoundTripSlots => new()
    {
        new KeySingleSlot(0x0D, KeyAction.Press,          200),
        new KeySingleSlot(0x41, KeyAction.Release,           0),
        new KeySingleSlot(0x70, KeyAction.Press,           100), // F1
        new KeySingleSlot(0x41, KeyAction.PressAndRelease, 150),
        new KeyTypeStringSlot("hello", false,  500),
        new KeyTypeStringSlot("world", true,  1000),
        new KeyTypeStringSlot("",      false,    0),
    };

    // ─── VirtualKeyItem ───────────────────────────────────────────────────

    [Fact]
    public void VirtualKeyItem_All_ContainsEnter()
    {
        VirtualKeyItem.All.Should().Contain(k => k.Code == 0x0D && k.Name == "Enter");
    }

    [Fact]
    public void VirtualKeyItem_All_ContainsAllLetters()
    {
        for (int vk = 0x41; vk <= 0x5A; vk++)
            VirtualKeyItem.All.Should().Contain(k => k.Code == vk, $"VK 0x{vk:X2} が一覧に含まれていない");
    }

    [Fact]
    public void VirtualKeyItem_All_ContainsAllDigits()
    {
        for (int vk = 0x30; vk <= 0x39; vk++)
            VirtualKeyItem.All.Should().Contain(k => k.Code == vk, $"VK 0x{vk:X2} が一覧に含まれていない");
    }

    [Fact]
    public void VirtualKeyItem_All_ContainsAllFunctionKeys()
    {
        for (int vk = 0x70; vk <= 0x7B; vk++)
            VirtualKeyItem.All.Should().Contain(k => k.Code == vk, $"F{vk - 0x6F} が一覧に含まれていない");
    }

    [Fact]
    public void VirtualKeyItem_All_NoDuplicateCodes()
    {
        VirtualKeyItem.All
            .GroupBy(k => k.Code)
            .Should().AllSatisfy(g => g.Count().Should().Be(1, $"VK 0x{g.Key:X2} が重複している"));
    }

    [Fact]
    public void VirtualKeyItem_All_DefaultIndex0IsEnter()
    {
        VirtualKeyItem.All[0].Code.Should().Be(0x0D);
    }
}
