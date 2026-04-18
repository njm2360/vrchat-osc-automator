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
    private readonly Mock<IOscSender> _osc = new(MockBehavior.Loose);
    private readonly Mock<IKeyboardSender> _keyboard = new(MockBehavior.Loose);
    private readonly Mock<IMouseSender> _mouse = new(MockBehavior.Loose);
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
            new KeySingleSlot(0x41, KeyAction.Press, 10),
            new KeySingleSlot(0x41, KeyAction.Release, 5)
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Release), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_KeySingleSlot_DoesNotCallOsc()
    {
        var slots = Slots(new KeySingleSlot(0x20, KeyAction.Press, 10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _osc.Verify(o => o.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _osc.Verify(o => o.SendInt(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _osc.Verify(o => o.SendBool(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
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

        _keyboard.Verify(k => k.TypeString("hi"), Times.Once);
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
        _osc.Verify(o => o.SendInt(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
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
            new IntSlot("/input/Jump", 1, 10, false, TransitionMode.None),
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

    // ─── キーリピート ─────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_KeySingleSlot_Press_WithDuration_SendsKeyDownMultipleTimes()
    {
        // Press + 十分な DurationMs → バックグラウンドリピートで KEYDOWN が複数回送信されること
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 200));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        // 200ms / 33ms ≒ 6 回以上の KEYDOWN が期待される
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.AtLeast(3));
    }

    [Fact]
    public async Task PlayAsync_KeySingleSlot_Press_ZeroDuration_SendsKeyDownOnce()
    {
        // DurationMs=0 のとき、リピートが発火する前にシーケンスが終わるので初回の1回のみ
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 0));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
    }

    // ─── 暴走防止: 停止・一時停止でキー解放 ──────────────────────────────

    [Fact]
    public async Task StopAsync_WhileKeyIsHeld_ReleasesKey()
    {
        // Press → Wait(長い) → Release の途中で Stop → Press 済みキーが解放されること
        var slots = Slots(
            new KeySingleSlot(0x41, KeyAction.Press, 0),
            new WaitSlot(5000),
            new KeySingleSlot(0x41, KeyAction.Release, 0)
        );

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(5); // リピート間隔(33ms)より十分短い時間で止める
        await _sut.StopAsync();
        await play;

        // Stop 前にリピートが発火しないことを保証するためではなく
        // Release が必ず送信されることを確認する
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.AtLeastOnce);
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Release), Times.AtLeastOnce);
    }

    [Fact]
    public async Task PauseAsync_WhileKeyIsHeld_ReleasesKey()
    {
        var slots = Slots(
            new KeySingleSlot(0x41, KeyAction.Press, 0),
            new WaitSlot(5000),
            new KeySingleSlot(0x41, KeyAction.Release, 0)
        );

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.PauseAsync();
        await Task.Delay(20); // Pause が反映されるのを待つ

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Release), Times.AtLeastOnce);

        await _sut.StopAsync();
        await play;
    }

    [Fact]
    public async Task ResumeAsync_AfterPauseWithHeldKey_RepressesKey()
    {
        var slots = Slots(
            new KeySingleSlot(0x41, KeyAction.Press, 0),
            new WaitSlot(5000),
            new KeySingleSlot(0x41, KeyAction.Release, 0)
        );

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.PauseAsync();
        await Task.Delay(20);

        // Pause 時点で Release が 1 回送られていること
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Release), Times.AtLeastOnce);

        await _sut.ResumeAsync();
        await Task.Delay(20); // Resume 後の再押下が反映されるのを待つ

        // Resume 後に Press が再送されること（初回 + 再押下 = 2 回以上）
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.AtLeast(2));

        await _sut.StopAsync();
        await play;
    }

    // ─── PressAndRelease ──────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_KeySingleSlot_PressAndRelease_SendsPressAtStart()
    {
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.PressAndRelease, DurationMs: 50));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_KeySingleSlot_PressAndRelease_SendsReleaseAfterDelay()
    {
        var order = new List<KeyAction>();
        _keyboard.Setup(k => k.SendKey(0x41, It.IsAny<KeyAction>()))
                 .Callback<int, KeyAction>((_, a) => order.Add(a));

        var slots = Slots(new KeySingleSlot(0x41, KeyAction.PressAndRelease, DurationMs: 50));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        order.Should().Equal(KeyAction.Press, KeyAction.Release);
    }

    [Fact]
    public async Task PlayAsync_KeySingleSlot_PressAndRelease_ZeroDuration_SendsBoth()
    {
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.PressAndRelease, DurationMs: 0));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Release), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DuringPressAndReleaseHold_SendsRelease()
    {
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.PressAndRelease, DurationMs: 5000));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30); // Press が送信されるのを待つ
        await _sut.StopAsync();
        await play;

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Release), Times.Once);
    }

    // ─── SetKeyRepeatSettings ─────────────────────────────────────────────

    [Fact]
    public async Task SetKeyRepeatSettings_Disabled_Press_NeverRepeats()
    {
        _sut.SetKeyRepeatSettings(new KeyRepeatSettings { IsEnabled = false });
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 200));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        // リピート無効 → 初回の1回のみ
        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
    }

    [Fact]
    public async Task SetKeyRepeatSettings_InitialDelayLongerThanDuration_NeverRepeats()
    {
        // 初回遅延がスロット継続時間より長い → リピートが発火する前にスロットが終わる
        _sut.SetKeyRepeatSettings(new KeyRepeatSettings { IsEnabled = true, InitialDelayMs = 500, IntervalMs = 33 });
        var slots = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 50));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.Once);
    }

    [Fact]
    public async Task SetKeyRepeatSettings_SlowInterval_FewerRepeatsThanDefault()
    {
        // 低速インターバル(100ms) vs デフォルト(33ms) で同じ時間内のリピート回数が少ないこと
        var slotsDefault = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 300));
        var slotsSlow = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 300));

        // デフォルト設定で実行
        int defaultCount = 0;
        _keyboard.Setup(k => k.SendKey(0x41, KeyAction.Press)).Callback(() => defaultCount++);
        await _sut.PlayAsync(slotsDefault, loop: false, null, CancellationToken.None);
        _keyboard.Invocations.Clear();

        // 低速インターバルに変更して再実行
        _sut.SetKeyRepeatSettings(new KeyRepeatSettings { IsEnabled = true, InitialDelayMs = 0, IntervalMs = 100 });
        int slowCount = 0;
        _keyboard.Setup(k => k.SendKey(0x41, KeyAction.Press)).Callback(() => slowCount++);
        await _sut.PlayAsync(slotsSlow, loop: false, null, CancellationToken.None);

        slowCount.Should().BeLessThan(defaultCount);
    }

    [Fact]
    public async Task SetKeyRepeatSettings_ReEnable_RepeatsAgain()
    {
        // 一度無効にして再度有効にすると再びリピートすること
        _sut.SetKeyRepeatSettings(new KeyRepeatSettings { IsEnabled = false });
        _sut.SetKeyRepeatSettings(new KeyRepeatSettings { IsEnabled = true, InitialDelayMs = 0, IntervalMs = 33 });

        var slots = Slots(new KeySingleSlot(0x41, KeyAction.Press, DurationMs: 200));
        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _keyboard.Verify(k => k.SendKey(0x41, KeyAction.Press), Times.AtLeast(3));
    }

    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static IReadOnlyList<SequenceSlot> Slots(params SequenceSlot[] slots) => slots;
}
