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
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Vertical",
            Value = 0.5f,
            ValueType = OscValueType.Float,
            DurationMs = 10,
            ResetOnComplete = false,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat("/input/Vertical", 0.5f), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_IntSlot_SendsInt()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Jump",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 10,
            ResetOnComplete = false,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_BoolSlot_SendsBool()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Voice",
            Value = 1f,
            ValueType = OscValueType.Bool,
            DurationMs = 10,
            ResetOnComplete = false,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendBool("/input/Voice", true), Times.Once);
    }

    [Fact]
    public async Task PlayAsync_StringSlot_SendsString()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/custom/param",
            StringValue = "hello",
            ValueType = OscValueType.String,
            DurationMs = 10,
            ResetOnComplete = false,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendString("/custom/param", "hello"), Times.Once);
    }

    // ─── 待機スロット（Address = null）────────────────────────────────────

    [Fact]
    public async Task PlayAsync_WaitSlot_DoesNotSend()
    {
        var slots = Slots(new SequenceSlot { Address = null, DurationMs = 10 });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _sender.Verify(s => s.SendInt(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    // ─── ResetOnComplete ──────────────────────────────────────────────────

    [Fact]
    public async Task ResetOnComplete_Float_SendsZero()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/param/float",
            Value = 0.8f,
            ValueType = OscValueType.Float,
            DurationMs = 10,
            ResetOnComplete = true,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat("/param/float", 0f), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_Int_SendsZero()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/param/int",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 10,
            ResetOnComplete = true,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendInt("/param/int", 0), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_Bool_SendsFalse()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/param/bool",
            Value = 1f,
            ValueType = OscValueType.Bool,
            DurationMs = 10,
            ResetOnComplete = true,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendBool("/param/bool", false), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_String_SendsEmpty()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/param/str",
            StringValue = "hello",
            ValueType = OscValueType.String,
            DurationMs = 10,
            ResetOnComplete = true,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendString("/param/str", string.Empty), Times.Once);
    }

    [Fact]
    public async Task ResetOnComplete_False_NeverSendsReset()
    {
        var slots = Slots(
            new SequenceSlot { Address = "/param/float", Value = 0.8f, ValueType = OscValueType.Float, DurationMs = 10, ResetOnComplete = false },
            new SequenceSlot { Address = "/param/int", Value = 1f, ValueType = OscValueType.Int, DurationMs = 10, ResetOnComplete = false },
            new SequenceSlot { Address = "/param/bool", Value = 1f, ValueType = OscValueType.Bool, DurationMs = 10, ResetOnComplete = false },
            new SequenceSlot { Address = "/param/str", StringValue = "hi", ValueType = OscValueType.String, DurationMs = 10, ResetOnComplete = false }
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
            new SequenceSlot { SlotType = SlotType.LoopBegin, RepeatCount = 3 },
            new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int, DurationMs = 5, ResetOnComplete = false },
            new SequenceSlot { SlotType = SlotType.LoopEnd }
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
            new SequenceSlot { SlotType = SlotType.LoopBegin, RepeatCount = 2 },
            new SequenceSlot { SlotType = SlotType.LoopBegin, RepeatCount = 3 },
            new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int, DurationMs = 5, ResetOnComplete = false },
            new SequenceSlot { SlotType = SlotType.LoopEnd },
            new SequenceSlot { SlotType = SlotType.LoopEnd }
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendInt("/input/Jump", 1), Times.Exactly(6));
    }

    // ─── Progress 通知 ───────────────────────────────────────────────────

    [Fact]
    public async Task PlayAsync_ReportsSlotIndexThenMinusOne()
    {
        var slots = Slots(
            new SequenceSlot { Address = "/input/Jump", Value = 1f, ValueType = OscValueType.Int, DurationMs = 5, ResetOnComplete = false },
            new SequenceSlot { Address = "/input/Voice", Value = 1f, ValueType = OscValueType.Int, DurationMs = 5, ResetOnComplete = false }
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

        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Jump",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 1000,
            ResetOnComplete = false,
        });

        await _sut.PlayAsync(slots, loop: false, null, cts.Token);

        _sender.Verify(s => s.SendInt(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task StopAsync_DuringPlay_StopsPlayback()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Vertical",
            Value = 1f,
            ValueType = OscValueType.Float,
            DurationMs = 5000,   // 長い遅延
            ResetOnComplete = false,
        });

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30); // PlayAsync が開始するのを待つ
        await _sut.StopAsync();
        await play;

        _sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task StopAsync_ResetOnComplete_False_DoesNotSendReset()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Vertical",
            Value = 1f,
            ValueType = OscValueType.Float,
            DurationMs = 5000,
            ResetOnComplete = false,
        });

        Task play = _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);
        await Task.Delay(30);
        await _sut.StopAsync();
        await play;

        _sender.Verify(s => s.SendFloat("/input/Vertical", 0f), Times.Never);
    }

    [Fact]
    public async Task StopAsync_ResetOnComplete_True_SendsReset()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Vertical",
            Value = 1f,
            ValueType = OscValueType.Float,
            DurationMs = 5000,
            ResetOnComplete = true,
        });

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
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Jump",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 50,
            ResetOnComplete = false,
        });

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
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Jump",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 5000,
            ResetOnComplete = true,
        });

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
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Jump",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 5000,
            ResetOnComplete = false,
        });

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
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Jump",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 5000,
            ResetOnComplete = false,
        });

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

    // ─── IsPlaying ───────────────────────────────────────────────────────

    [Fact]
    public void IsPlaying_BeforePlay_IsFalse()
    {
        _sut.IsPlaying.Should().BeFalse();
    }

    [Fact]
    public async Task IsPlaying_AfterPlayCompletes_IsFalse()
    {
        var slots = Slots(new SequenceSlot
        {
            Address = "/input/Jump",
            Value = 1f,
            ValueType = OscValueType.Int,
            DurationMs = 10,
            ResetOnComplete = false,
        });

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sut.IsPlaying.Should().BeFalse();
    }

    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static IReadOnlyList<SequenceSlot> Slots(params SequenceSlot[] slots) => slots;
}
