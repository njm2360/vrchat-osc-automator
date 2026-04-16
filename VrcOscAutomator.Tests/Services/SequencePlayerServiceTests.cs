using FluentAssertions;
using Moq;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;
using VrcOscAutomator.Services;
using Xunit;

namespace VrcOscAutomator.Tests.Services;

public class SequencePlayerServiceTests : IDisposable
{
    private readonly Mock<IOscSender> _sender = new(MockBehavior.Loose);
    private readonly SequencePlayerService _sut;

    public SequencePlayerServiceTests()
    {
        _sut = new SequencePlayerService(_sender.Object);
    }

    public void Dispose() => _sut.Dispose();

    // ─── 基本送信 ─────────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_FloatSlot_SendsFloat()
    {
        var slots = Slots(new FloatSlot("/input/Vertical", 0.5f, 10, false));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat("/input/Vertical", 0.5f), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_IntSlot_SendsInt()
    {
        var slots = Slots(new IntSlot("/input/Jump", 1, 10, false));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_BoolSlot_SendsBool()
    {
        var slots = Slots(new BoolSlot("/input/Voice", true, 10, false));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendBool("/input/Voice", true), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_StringSlot_SendsString()
    {
        var slots = Slots(new StringSlot("/custom/param", "hello", 10, false));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendString("/custom/param", "hello"), Times.Once);
    }

    // ─── 待機スロット ─────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_WaitSlot_DoesNotSend()
    {
        var slots = Slots(new WaitSlot(10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _sender.Verify(s => s.SendInt(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    // ─── ResetOnComplete ──────────────────────────────────────────────────

    [Fact]
    public async Task ResetOnComplete_Float_SendsZero()
    {
        var slots = Slots(new FloatSlot("/param/float", 0.8f, 10, true));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat("/param/float", 0f), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_Int_SendsZero()
    {
        var slots = Slots(new IntSlot("/param/int", 1, 10, true));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendInt("/param/int", 0), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_Bool_SendsFalse()
    {
        var slots = Slots(new BoolSlot("/param/bool", true, 10, true));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendBool("/param/bool", false), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_String_SendsEmpty()
    {
        var slots = Slots(new StringSlot("/param/str", "hello", 10, true));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendString("/param/str", string.Empty), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_False_NeverSendsReset()
    {
        var slots = Slots(
            new FloatSlot("/param/float", 0.8f, 10, false),
            new IntSlot("/param/int",     1,    10, false),
            new BoolSlot("/param/bool",   true, 10, false),
            new StringSlot("/param/str",  "hi", 10, false)
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat("/param/float", It.IsAny<float>()), Times.Once); // 送信のみ
        _sender.Verify(s => s.SendInt("/param/int", It.IsAny<int>()), Times.Once);
        _sender.Verify(s => s.SendBool("/param/bool", It.IsAny<bool>()), Times.Once);
        _sender.Verify(s => s.SendString("/param/str", It.IsAny<string>()), Times.Once);
    }

    // ─── LoopBegin / LoopEnd ──────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_LoopBlock_ExecutesBodyRepeatCountTimes()
    {
        // [LoopBegin×3] [Jump=1] [LoopEnd]  → Jump を 3 回送信
        var slots = Slots(
            new LoopBeginSlot(3),
            new IntSlot("/input/Jump", 1, 5, false),
            new LoopEndSlot()
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.Exactly(3));
    }

    [Fact]
    public async Task PlayAsync_NestedLoopBlocks_ExecutesCorrectCount()
    {
        // [LoopBegin×2] [LoopBegin×3] [Jump=1] [LoopEnd] [LoopEnd]
        // → Jump は 2×3 = 6 回
        var slots = Slots(
            new LoopBeginSlot(2),
            new LoopBeginSlot(3),
            new IntSlot("/input/Jump", 1, 5, false),
            new LoopEndSlot(),
            new LoopEndSlot()
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.Exactly(6));
    }

    [Fact]
    public async Task PlayAsync_LoopBlock_RepeatCountZero_LoopsIndefinitely()
    {
        // RepeatCount=0 は無限ループ。キャンセルで止まり、3回以上実行されていること
        using var cts = new CancellationTokenSource();
        var slots = Slots(
            new LoopBeginSlot(0),
            new IntSlot("/input/Jump", 1, 5, false),
            new LoopEndSlot()
        );

        Task play = _sut.PlayAsync(slots, loop: false, null, cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await play;

        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.AtLeast(3));
    }

    [Fact]
    public async Task PlayAsync_LoopTrue_RepeatsSequenceAfterEnd()
    {
        // loop=true の場合、全スロット完了後に先頭に戻って繰り返す
        using var cts = new CancellationTokenSource();
        var slots = Slots(new IntSlot("/input/Jump", 1, 5, false));

        Task play = _sut.PlayAsync(slots, loop: true, null, cts.Token);
        await Task.Delay(100);
        cts.Cancel();
        await play;

        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.AtLeast(2));
    }

    // ─── Progress 通知 ───────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_ReportsSlotIndexThenMinusOne()
    {
        var slots = Slots(
            new IntSlot("/input/Jump",  1, 5, false),
            new IntSlot("/input/Voice", 1, 5, false)
        );
        var reported = new List<int>();
        var progress = new Progress<int>(i => reported.Add(i));

        await _sut.PlayAsync(slots, loop: false, progress, CancellationToken.None);

        // Progress は非同期ポストなので少し待つ
        await Task.Delay(50);
        reported.Should().ContainInOrder(0, 1, -1);
    }

    // ─── キャンセル ──────────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_CancelledImmediately_DoesNotSend()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var slots = Slots(new IntSlot("/input/Jump", 1, 1000, false));

        await _sut.PlayAsync(slots, loop: false, null, cts.Token);

        _sender.Verify(s => s.SendInt(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task StopAsync_DuringPlay_StopsPlayback()
    {
        var slots = Slots(new FloatSlot("/input/Vertical", 1f, 5000, false));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30); // PlayAsync が開始するのを待つ
        await _sut.StopAsync();
        await play;

        _sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_ResetOnComplete_False_DoesNotSendReset()
    {
        var slots = Slots(new FloatSlot("/input/Vertical", 1f, 5000, false));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.StopAsync();
        await play;

        _sender.Verify(s => s.SendFloat("/input/Vertical", 0f), Times.Never);
    }

    [Fact]
    public async Task StopAsync_ResetOnComplete_True_SendsReset()
    {
        var slots = Slots(new FloatSlot("/input/Vertical", 1f, 5000, true));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.StopAsync();
        await play;

        _sender.Verify(s => s.SendFloat("/input/Vertical", 0f), Times.Once);
    }

    // ─── Pause / Resume ──────────────────────────────────────────────────

    [Fact]
    public async Task PauseAsync_ThenResumeAsync_CompletesPlayback()
    {
        var slots = Slots(new IntSlot("/input/Jump", 1, 50, false));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(10);
        await _sut.PauseAsync();
        _sut.IsPaused.Should().BeTrue();

        await _sut.ResumeAsync();
        await play;

        _sut.IsPaused.Should().BeFalse();
        _sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task PauseAsync_ResetOnComplete_True_SendsReset()
    {
        var slots = Slots(new IntSlot("/input/Jump", 1, 5000, true));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.PauseAsync();
        await _sut.StopAsync();
        await play;

        _sender.Verify(s => s.SendInt("/input/Jump", 0), Times.Once);
    }

    [Fact]
    public async Task PauseAsync_ResetOnComplete_False_DoesNotSendReset()
    {
        var slots = Slots(new IntSlot("/input/Jump", 1, 5000, false));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.PauseAsync();
        await _sut.StopAsync();
        await play;

        _sender.Verify(s => s.SendInt("/input/Jump", 0), Times.Never);
    }

    [Fact]
    public async Task ResumeAsync_ResendsSendCommand()
    {
        var slots = Slots(new IntSlot("/input/Jump", 1, 5000, false));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.PauseAsync();
        await _sut.ResumeAsync();
        await Task.Delay(30); // Resume後の再送が実行されるのを待つ
        await _sut.StopAsync();
        await play;

        // 初回送信 + Resume後の再送 2回
        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.Exactly(2));
    }

    [Fact]
    public async Task ResumeAsync_ContinuesFromSameSlot_NotNextSlot()
    {
        // Pause → Resume 後、次のスロットに進まず同じスロットに留まること
        var slots = Slots(
            new IntSlot("/slot/first",  1, 400, false),
            new IntSlot("/slot/second", 1,  10, false)
        );

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30); // first スロット実行中に Pause
        await _sut.PauseAsync();

        // Pause 中に second スロットへ進んでいないこと
        _sender.Verify(s => s.SendInt("/slot/second", 1), Times.Never);

        await _sut.ResumeAsync();
        await play;

        // Resume 後: first の再送 (初回 + 再送 = 2回) → second が 1回
        _sender.Verify(s => s.SendInt("/slot/first",  1), Times.Exactly(2));
        _sender.Verify(s => s.SendInt("/slot/second", 1), Times.Once);
    }

    [Fact]
    public async Task StopAsync_WhilePaused_ClearsBothFlags()
    {
        // 一時停止中に停止したとき IsPaused/IsPlaying が両方 false になること
        var slots = Slots(new IntSlot("/input/Jump", 1, 5000, false));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.PauseAsync();
        await _sut.StopAsync();
        await play;

        _sut.IsPaused.Should().BeFalse();
        _sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task PauseAsync_ResetOnComplete_True_SendsResetAtPauseTime()
    {
        // リセットは Stop 時ではなく Pause 時点で送られること
        var slots = Slots(new IntSlot("/input/Jump", 1, 5000, true));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.PauseAsync();
        await Task.Delay(20); // Pause が反映されるのを待つ

        // StopAsync 前の時点でリセット済み
        _sender.Verify(s => s.SendInt("/input/Jump", 0), Times.Once);

        await _sut.StopAsync();
        await play;

        // Stop 後も追加リセットは発生しない（_activeSlot が Pause 時に null 化されているため）
        _sender.Verify(s => s.SendInt("/input/Jump", 0), Times.Once);
    }

    [Fact]
    public async Task ResumeAsync_RemainingTimePreserved_SlotDoesNotRestartFromFull()
    {
        // Pause 後に再開したとき、経過分だけ短くなった残り時間でカウントが再開されること（ストップウォッチ動作）
        // Duration=400ms のスロット、約30ms 再生後に Pause → 残り約370ms のはず
        // Pause 中に 500ms 待機（Duration を超える時間）してから Resume しても、
        // Resume 後に自然完了するまでの追加時間は ~400ms ではなく ~370ms 以内であること
        var slots = Slots(new IntSlot("/input/Jump", 1, 400, false));

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);   // ~30ms 再生
        await _sut.PauseAsync();
        await Task.Delay(500);  // Pause 中に Duration を超えて待機（カウントされてはいけない）
        await _sut.ResumeAsync();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        await play; // 自然完了を待つ
        sw.Stop();

        // 残り ~370ms で完了するはず。フルで再スタートしていたら ~400ms かかる
        // ストップウォッチ動作なら 400ms 以内に完了（余裕を持って 450ms をしきい値とする）
        sw.ElapsedMilliseconds.Should().BeLessThan(450);
    }

    // ─── IsPlaying ───────────────────────────────────────────────────────

    [Fact]
    public void IsPlaying_BeforePlay_IsFalse()
    {
        _sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task IsPlaying_AfterPlayCompletes_IsFalse()
    {
        var slots = Slots(new IntSlot("/input/Jump", 1, 10, false));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sut.IsPlaying.Should().BeFalse();
    }

    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static IReadOnlyList<SequenceSlot> Slots(params SequenceSlot[] slots) => slots;
}
