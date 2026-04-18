using Moq;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;
using VrcOscAutomator.Services;
using Xunit;

namespace VrcOscAutomator.Tests.Services;

/// <summary>SequencePlayerService の OSC スロット実行に関するテスト。</summary>
public class OscSlotTests : IDisposable
{
    private readonly Mock<IOscSender> _sender = new(MockBehavior.Loose);
    private readonly Mock<IKeyboardSender> _keyboard = new(MockBehavior.Loose);
    private readonly Mock<IMouseSender> _mouse = new(MockBehavior.Loose);
    private readonly SequencePlayerService _sut;

    public OscSlotTests()
    {
        _sut = new SequencePlayerService(_sender.Object, _keyboard.Object, _mouse.Object);
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
    public async Task PlayAsync_WaitSlot_DoesNotSendOsc()
    {
        var slots = Slots(new WaitSlot(10));

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat(It.IsAny<string>(), It.IsAny<float>()), Times.Never);
        _sender.Verify(s => s.SendInt(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task PlayAsync_RandomWaitSlot_DoesNotSendOsc()
    {
        var slots = Slots(new RandomWaitSlot(0, 10));

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
            new IntSlot("/param/int", 1, 10, false),
            new BoolSlot("/param/bool", true, 10, false),
            new StringSlot("/param/str", "hi", 10, false)
        );

        await _sut.PlayAsync(slots, loop: false, null, CancellationToken.None);

        _sender.Verify(s => s.SendFloat("/param/float", It.IsAny<float>()), Times.Once);
        _sender.Verify(s => s.SendInt("/param/int", It.IsAny<int>()), Times.Once);
        _sender.Verify(s => s.SendBool("/param/bool", It.IsAny<bool>()), Times.Once);
        _sender.Verify(s => s.SendString("/param/str", It.IsAny<string>()), Times.Once);
    }

    // ─── ヘルパー ─────────────────────────────────────────────────────────

    private static IReadOnlyList<SequenceSlot> Slots(params SequenceSlot[] slots) => slots;
}
