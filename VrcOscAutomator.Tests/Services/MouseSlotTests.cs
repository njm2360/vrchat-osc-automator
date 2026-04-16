using FluentAssertions;
using Moq;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;
using VrcOscAutomator.Services;
using Xunit;

namespace VrcOscAutomator.Tests.Services;

/// <summary>SequencePlayerService のマウススロット実行に関するテスト。</summary>
public class MouseSlotTests : IDisposable
{
    private readonly Mock<IOscSender>      _osc      = new(MockBehavior.Loose);
    private readonly Mock<IKeyboardSender> _keyboard = new(MockBehavior.Loose);
    private readonly Mock<IMouseSender>    _mouse    = new(MockBehavior.Loose);
    private readonly SequencePlayerService _sut;

    public MouseSlotTests()
    {
        _sut = new SequencePlayerService(_osc.Object, _keyboard.Object, _mouse.Object);
    }

    public void Dispose() => _sut.Dispose();

    // ─── MouseButtonSlot ─────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_Left_Press_CallsSendMouseButton()
    {
        var slots = Slots(new MouseButtonSlot(MouseButton.Left, KeyAction.Press, DurationMs: 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Press), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_Right_Release_CallsSendMouseButton()
    {
        var slots = Slots(new MouseButtonSlot(MouseButton.Right, KeyAction.Release, DurationMs: 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Right, KeyAction.Release), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_Middle_Press_CallsSendMouseButton()
    {
        var slots = Slots(new MouseButtonSlot(MouseButton.Middle, KeyAction.Press, DurationMs: 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Middle, KeyAction.Press), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_DoesNotCallOscOrKeyboard()
    {
        var slots = Slots(new MouseButtonSlot(MouseButton.Left, KeyAction.Press, DurationMs: 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _osc.Verify(o => o.SendInt(It.IsAny<string>(),   It.IsAny<int>()),   Times.Never);
        _keyboard.Verify(k => k.SendKey(It.IsAny<int>(), It.IsAny<KeyAction>()), Times.Never);
    }

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_PressRelease_BothExecuted()
    {
        var slots = Slots(
            new MouseButtonSlot(MouseButton.Left, KeyAction.Press,   10),
            new MouseButtonSlot(MouseButton.Left, KeyAction.Release,  5)
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Press),   Times.Once);
        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Release),  Times.Once);
    }

    // ─── MouseWheelSlot ──────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_MouseWheelSlot_PositiveClicks_CallsSendMouseWheel()
    {
        var slots = Slots(new MouseWheelSlot(3));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseWheel(3), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseWheelSlot_NegativeClicks_CallsSendMouseWheel()
    {
        var slots = Slots(new MouseWheelSlot(-2));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseWheel(-2), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseWheelSlot_DoesNotCallOscOrKeyboard()
    {
        var slots = Slots(new MouseWheelSlot(1));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _keyboard.Verify(k => k.SendKey(It.IsAny<int>(), It.IsAny<KeyAction>()), Times.Never);
    }

    // ─── MouseMoveSlot ───────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_MouseMoveSlot_Relative_CallsSendMouseMove()
    {
        var slots = Slots(new MouseMoveSlot(100, -50, MouseMoveMode.Relative));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseMove(100, -50, MouseMoveMode.Relative), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseMoveSlot_Absolute_CallsSendMouseMove()
    {
        var slots = Slots(new MouseMoveSlot(1920, 1080, MouseMoveMode.Absolute));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseMove(1920, 1080, MouseMoveMode.Absolute), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseMoveSlot_DoesNotCallOscOrKeyboard()
    {
        var slots = Slots(new MouseMoveSlot(10, 20, MouseMoveMode.Relative));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _keyboard.Verify(k => k.SendKey(It.IsAny<int>(), It.IsAny<KeyAction>()), Times.Never);
    }

    // ─── キャンセル ───────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_CancelledBeforeMouseSlot_DoesNotCallMouseSender()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var slots = Slots(new MouseButtonSlot(MouseButton.Left, KeyAction.Press, 10));

        await _sut.PlayAsync(slots, loop: false, null, cts.Token);

        _mouse.Verify(m => m.SendMouseButton(It.IsAny<MouseButton>(), It.IsAny<KeyAction>()), Times.Never);
    }

    // ─── OscSlot / Keyboard と混在 ────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_OscKeyboardMouseMixed_AllExecuted()
    {
        var slots = Slots(
            new IntSlot("/input/Jump", 1, 10, false),
            new KeySingleSlot(0x20, KeyAction.Press, 10),
            new MouseButtonSlot(MouseButton.Left, KeyAction.Press, 10)
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendInt("/input/Jump", 1),               Times.Once);
        _keyboard.Verify(k => k.SendKey(0x20, KeyAction.Press),     Times.Once);
        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Press), Times.Once);
    }

    // ─── ループブロック内のマウススロット ────────────────────────────────

    [Fact]
    public async Task PlayAsync_MouseSlotInLoopBlock_ExecutedRepeatCountTimes()
    {
        var slots = Slots(
            new LoopBeginSlot(3),
            new MouseButtonSlot(MouseButton.Left, KeyAction.Press, 5),
            new LoopEndSlot()
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Press), Times.Exactly(3));
    }

    // ─── DurationMs の待機 ────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_WithDuration_WaitsBeforeNextSlot()
    {
        var slots = Slots(
            new MouseButtonSlot(MouseButton.Left, KeyAction.Press, DurationMs: 80),
            new MouseButtonSlot(MouseButton.Left, KeyAction.Release, DurationMs: 0)
        );

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        sw.Stop();

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Press),   Times.Once);
        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Release),  Times.Once);
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(70);
    }

    // ─── PressAndRelease ──────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_PressAndRelease_SendsPressAtStart()
    {
        var slots = Slots(new MouseButtonSlot(MouseButton.Left, KeyAction.PressAndRelease, DurationMs: 50));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Press), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_PressAndRelease_SendsReleaseAfterDelay()
    {
        var order = new List<KeyAction>();
        _mouse.Setup(m => m.SendMouseButton(MouseButton.Left, It.IsAny<KeyAction>()))
              .Callback<MouseButton, KeyAction>((_, a) => order.Add(a));

        var slots = Slots(new MouseButtonSlot(MouseButton.Left, KeyAction.PressAndRelease, DurationMs: 50));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        order.Should().Equal(KeyAction.Press, KeyAction.Release);
    }

    [Fact]
    public async Task PlayAsync_MouseButtonSlot_PressAndRelease_ZeroDuration_SendsBoth()
    {
        var slots = Slots(new MouseButtonSlot(MouseButton.Right, KeyAction.PressAndRelease, DurationMs: 0));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Right, KeyAction.Press),   Times.Once);
        _mouse.Verify(m => m.SendMouseButton(MouseButton.Right, KeyAction.Release),  Times.Once);
    }

    [Fact]
    public async Task StopAsync_DuringMousePressAndReleaseHold_SendsRelease()
    {
        var slots = Slots(new MouseButtonSlot(MouseButton.Left, KeyAction.PressAndRelease, DurationMs: 5000));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30); // Press が送信されるのを待つ
        await _sut.StopAsync();
        await play;

        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Press),   Times.Once);
        _mouse.Verify(m => m.SendMouseButton(MouseButton.Left, KeyAction.Release),  Times.Once);
    }

    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static IReadOnlyList<SequenceSlot> Slots(params SequenceSlot[] slots) => slots;
}
