using FluentAssertions;
using Moq;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;
using VrcOscAutomator.Services;
using Xunit;

namespace VrcOscAutomator.Tests.Services;

/// <summary>SequencePlayerService のキーボードスロット実行に関するテスト。</summary>
public class KeyboardSlotTests : IDisposable
{
    private readonly Mock<IOscSender>      _osc      = new(MockBehavior.Loose);
    private readonly Mock<IKeyboardSender> _keyboard = new(MockBehavior.Loose);
    private readonly Mock<IMouseSender>    _mouse    = new(MockBehavior.Loose);
    private readonly SequencePlayerService _sut;

    public KeyboardSlotTests()
    {
        _sut = new SequencePlayerService(_osc.Object, _keyboard.Object, _mouse.Object);
    }

    public void Dispose() => _sut.Dispose();

    // ─── KeySingleSlot ────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_KeySingleSlot_Press_CallsSendKey_Press()
    {
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_KeySingleSlot_Release_CallsSendKey_Release()
    {
        var slots = Slots(new KeySingleSlot(0x0D, KeyAction.Release, DurationMs: 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x0D, KeyAction.Release), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_MultipleKeySlots_EachCalledOnce()
    {
        var slots = Slots(
            new KeySingleSlot(0x41, KeyAction.Press,   10),
            new KeySingleSlot(0x41, KeyAction.Release,  5)
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press),   Times.Once);
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Release),  Times.Once);
    }

    [Fact]
    public async Task PlayAsync_KeySingleSlot_DoesNotCallOsc()
    {
        var slots = Slots(new KeySingleSlot(0x20, KeyAction.Press, 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _osc.Verify(o => o.SendInt(It.IsAny<string>(),   It.IsAny<int>()),   Times.Never);
        _osc.Verify(o => o.SendBool(It.IsAny<string>(),  It.IsAny<bool>()), Times.Never);
    }

    // ─── KeyTypeStringSlot ────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_KeyTypeStringSlot_CallsTypeString()
    {
        var slots = Slots(new KeyTypeStringSlot("hello", DurationMs: 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.TypeString("hello"), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_KeyTypeStringSlot_WithDuration_NextSlotExecutedAfterDelay()
    {
        // 文字入力後に DurationMs 分だけ待機してから次スロットへ進むこと
        var slots = Slots(
            new KeyTypeStringSlot("hi", DurationMs: 80),
            new KeySingleSlot(0x0D, KeyAction.Press, 0)
        );

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        sw.Stop();

        _keyboard.Verify(k => k.TypeString("hi"),             Times.Once);
        _keyboard.Verify(k => k.SendKey(0x0D, KeyAction.Press), Times.Once);
        sw.ElapsedMilliseconds.Should().BeGreaterThanOrEqualTo(70); // 80ms 待機のゆとり確認
    }

    [Fact]
    public async Task PlayAsync_KeyTypeStringSlot_AppendNewline_True_AppendsLf()
    {
        var slots = Slots(new KeyTypeStringSlot("hello", AppendNewline: true));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.TypeString("hello\n"), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_KeyTypeStringSlot_AppendNewline_False_NoLf()
    {
        var slots = Slots(new KeyTypeStringSlot("hello", AppendNewline: false));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.TypeString("hello"), Times.Once);
        _keyboard.Verify(k => k.TypeString(It.Is<string>(s => s.Contains('\n'))), Times.Never);
    }

    [Fact]
    public async Task PlayAsync_KeyTypeStringSlot_EmptyText_StillCallsTypeString()
    {
        var slots = Slots(new KeyTypeStringSlot(""));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.TypeString(""), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_KeyTypeStringSlot_DoesNotCallOsc()
    {
        var slots = Slots(new KeyTypeStringSlot("test"));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _osc.Verify(o => o.SendInt(It.IsAny<string>(),   It.IsAny<int>()),   Times.Never);
    }

    // ─── キャンセル / 停止 ────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_CancelledBeforeKeySlot_DoesNotCallSendKey()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var slots = Slots(new KeySingleSlot(0x41, KeyAction.Press, 10));

        await _sut.PlayAsync(slots, loop: false, null, cts.Token);

        _keyboard.Verify(k => k.SendKey(It.IsAny<int>(), It.IsAny<KeyAction>()), Times.Never);
    }

    // ─── OscSlot と混在 ──────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_OscAndKeySlotsMixed_BothExecuted()
    {
        var slots = Slots(
            new IntSlot("/input/Jump", 1, 10, false),
            new KeySingleSlot(0x20, KeyAction.Press, 10)
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendInt("/input/Jump", 1), Times.Once);
        _keyboard.Verify(k => k.SendKey(0x20, KeyAction.Press), Times.Once);
    }

    // ─── ループブロック内のキーボードスロット ────────────────────────────

    [Fact]
    public async Task PlayAsync_KeySlotInLoopBlock_ExecutedRepeatCountTimes()
    {
        var slots = Slots(
            new LoopBeginSlot(3),
            new KeySingleSlot(0x0D, KeyAction.Press, 5),
            new LoopEndSlot()
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x0D, KeyAction.Press), Times.Exactly(3));
    }

    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static IReadOnlyList<SequenceSlot> Slots(params SequenceSlot[] slots) => slots;
}
