using FluentAssertions;
using VrcOscAutomator.Models;
using VrcOscAutomator.ViewModels;
using Xunit;
using KeyAction = VrcOscAutomator.Models.KeyAction;
using MouseButton = VrcOscAutomator.Models.MouseButton;

namespace VrcOscAutomator.Tests.ViewModels;

/// <summary>マウスボタンスロット (MouseButtonPreset) の ViewModel テスト。</summary>
public class MouseButtonSlotViewModelTests
{
    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static SlotPreset MouseButtonPreset => SlotPreset.All.First(p => p.IsMouseButton);

    // ─── IsMouseButtonMode ────────────────────────────────────────────────

    [Fact]
    public void IsMouseButtonMode_WhenMouseButtonPreset_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = MouseButtonPreset };
        vm.IsMouseButtonMode.Should().BeTrue();
    }

    [Fact]
    public void IsMouseButtonMode_WhenOtherPreset_False()
    {
        var vm = new SequenceSlotViewModel(); // デフォルトは WaitPreset
        vm.IsMouseButtonMode.Should().BeFalse();
    }

    // ─── IsMouseButtonAction (Press / Release) ────────────────────────────

    [Fact]
    public void IsMouseButtonActionPress_Default_True()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = MouseButtonPreset };
        vm.IsMouseButtonActionPress.Should().BeTrue();
        vm.IsMouseButtonActionRelease.Should().BeFalse();
        vm.IsMouseButtonActionPressAndRelease.Should().BeFalse();
    }

    [Fact]
    public void IsMouseButtonActionRelease_SetToRelease_True()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButtonAction = KeyAction.Release,
        };
        vm.IsMouseButtonActionRelease.Should().BeTrue();
        vm.IsMouseButtonActionPress.Should().BeFalse();
        vm.IsMouseButtonActionPressAndRelease.Should().BeFalse();
    }

    [Fact]
    public void IsMouseButtonActionPress_Setter_UpdatesSelectedMouseButtonAction()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButtonAction = KeyAction.Release,
        };
        vm.IsMouseButtonActionPress = true;
        vm.SelectedMouseButtonAction.Should().Be(KeyAction.Press);
    }

    [Fact]
    public void IsMouseButtonActionRelease_Setter_UpdatesSelectedMouseButtonAction()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = MouseButtonPreset };
        vm.IsMouseButtonActionRelease = true;
        vm.SelectedMouseButtonAction.Should().Be(KeyAction.Release);
    }

    // ─── IsMouseButtonActionPressAndRelease ───────────────────────────────

    [Fact]
    public void IsMouseButtonActionPressAndRelease_Default_False()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = MouseButtonPreset };
        vm.IsMouseButtonActionPressAndRelease.Should().BeFalse();
    }

    [Fact]
    public void IsMouseButtonActionPressAndRelease_SetToPressAndRelease_TrueOthersFalse()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButtonAction = KeyAction.PressAndRelease,
        };
        vm.IsMouseButtonActionPressAndRelease.Should().BeTrue();
        vm.IsMouseButtonActionPress.Should().BeFalse();
        vm.IsMouseButtonActionRelease.Should().BeFalse();
    }

    [Fact]
    public void IsMouseButtonActionPressAndRelease_Setter_UpdatesSelectedMouseButtonAction()
    {
        var vm = new SequenceSlotViewModel { SelectedPreset = MouseButtonPreset };
        vm.IsMouseButtonActionPressAndRelease = true;
        vm.SelectedMouseButtonAction.Should().Be(KeyAction.PressAndRelease);
    }

    // ─── ParameterSummary ────────────────────────────────────────────────

    [Fact]
    public void ParameterSummary_MouseButton_Press_ShowsButtonAndPress()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButton = MouseButton.Left,
            SelectedMouseButtonAction = KeyAction.Press,
        };
        vm.ParameterSummary.Should().Be("左ボタン [押す]");
    }

    [Fact]
    public void ParameterSummary_MouseButton_Release_ShowsButtonAndRelease()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButton = MouseButton.Right,
            SelectedMouseButtonAction = KeyAction.Release,
        };
        vm.ParameterSummary.Should().Be("右ボタン [離す]");
    }

    [Fact]
    public void ParameterSummary_MouseButton_PressAndRelease_ShowsLabel()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButton = MouseButton.Left,
            SelectedMouseButtonAction = KeyAction.PressAndRelease,
        };
        vm.ParameterSummary.Should().Be("左ボタン [押して離す]");
    }

    // ─── ToModel ──────────────────────────────────────────────────────────

    [Fact]
    public void ToModel_MouseButtonPreset_Press_ReturnsMouseButtonSlot()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButton = MouseButton.Left,
            SelectedMouseButtonAction = KeyAction.Press,
            DurationMs = 100,
        };

        var model = (MouseButtonSlot)vm.ToModel();
        model.Button.Should().Be(MouseButton.Left);
        model.Action.Should().Be(KeyAction.Press);
        model.DurationMs.Should().Be(100);
    }

    [Fact]
    public void ToModel_MouseButtonPreset_PressAndRelease_ReturnsPressAndReleaseAction()
    {
        var vm = new SequenceSlotViewModel
        {
            SelectedPreset = MouseButtonPreset,
            SelectedMouseButton = MouseButton.Right,
            SelectedMouseButtonAction = KeyAction.PressAndRelease,
            DurationMs = 200,
        };

        var model = (MouseButtonSlot)vm.ToModel();
        model.Button.Should().Be(MouseButton.Right);
        model.Action.Should().Be(KeyAction.PressAndRelease);
        model.DurationMs.Should().Be(200);
    }

    // ─── FromModel ────────────────────────────────────────────────────────

    [Fact]
    public void FromModel_MouseButtonSlot_Press_RestoresProperties()
    {
        var slot = new MouseButtonSlot(MouseButton.Middle, KeyAction.Press, 50);

        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(slot);

        vm.SelectedPreset.IsMouseButton.Should().BeTrue();
        vm.SelectedMouseButton.Should().Be(MouseButton.Middle);
        vm.SelectedMouseButtonAction.Should().Be(KeyAction.Press);
        vm.DurationMs.Should().Be(50);
    }

    [Fact]
    public void FromModel_MouseButtonSlot_PressAndRelease_RestoresAction()
    {
        var slot = new MouseButtonSlot(MouseButton.Left, KeyAction.PressAndRelease, 300);

        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(slot);

        vm.SelectedMouseButtonAction.Should().Be(KeyAction.PressAndRelease);
        vm.IsMouseButtonActionPressAndRelease.Should().BeTrue();
        vm.DurationMs.Should().Be(300);
    }

    // ─── ToModel / FromModel ラウンドトリップ ─────────────────────────────

    [Theory]
    [MemberData(nameof(MouseButtonRoundTripSlots))]
    public void ToModel_FromModel_RoundTrip(SequenceSlot original)
    {
        SequenceSlotViewModel vm = SequenceSlotViewModel.FromModel(original);
        SequenceSlot restored = vm.ToModel();

        restored.Should().Be(original);
    }

    public static TheoryData<SequenceSlot> MouseButtonRoundTripSlots => new()
    {
        new MouseButtonSlot(MouseButton.Left,   KeyAction.Press,          100),
        new MouseButtonSlot(MouseButton.Right,  KeyAction.Release,          0),
        new MouseButtonSlot(MouseButton.Middle, KeyAction.Press,           50),
        new MouseButtonSlot(MouseButton.Left,   KeyAction.PressAndRelease, 200),
        new MouseButtonSlot(MouseButton.Right,  KeyAction.PressAndRelease,   0),
    };
}
