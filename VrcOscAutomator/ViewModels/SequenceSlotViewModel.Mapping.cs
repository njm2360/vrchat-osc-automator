using System.Diagnostics;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel
{
    // VM => SequenceSlot
    public SequenceSlot ToModel() => SelectedPreset switch
    {
        LoopBeginPreset => new LoopBeginSlot(RepeatCount),
        LoopEndPreset => new LoopEndSlot(),
        WaitPreset => new WaitSlot(DurationMs),
        RandomWaitPreset => new RandomWaitSlot(RandomWaitMinMs, RandomWaitMaxMs),
        BreakpointPreset => new BreakpointSlot(),
        KeySinglePreset => new KeySingleSlot(SelectedKey.Code, SelectedKeyAction, DurationMs),
        KeyTypeStringPreset => new KeyTypeStringSlot(TypeText, AppendNewline, DurationMs),
        MouseButtonPreset => new MouseButtonSlot(SelectedMouseButton, SelectedMouseButtonAction, DurationMs),
        MouseWheelPreset => new MouseWheelSlot(WheelClicks, DurationMs),
        MouseMovePreset => new MouseMoveSlot(MouseMoveX, MouseMoveY, SelectedMouseMoveMode, DurationMs),
        BuiltinPreset bp => OscSlot(bp.Address, bp.ValueType),
        CustomPreset => OscSlot(CustomAddress, CustomValueType),
        _ => throw new UnreachableException(),
    };

    // OSCスロットの組み立てヘルパー
    private SequenceSlot OscSlot(string address, OscValueType vt) => vt switch
    {
        OscValueType.Int => new IntSlot(address, IntValue, DurationMs, ResetOnComplete,
            TransitionMode,
            TransitionMode != TransitionMode.None ? IntTransitionFrom : null,
            TransitionMode != TransitionMode.None ? IntTransitionTo : null),
        OscValueType.Bool => new BoolSlot(address, BoolValue, DurationMs, ResetOnComplete),
        OscValueType.String => new StringSlot(address, StringValue, DurationMs, ResetOnComplete),
        _ => new FloatSlot(address, FloatValue, DurationMs, ResetOnComplete,
            TransitionMode,
            TransitionMode != TransitionMode.None ? FloatTransitionFrom : null,
            TransitionMode != TransitionMode.None ? FloatTransitionTo : null),
    };

    // SequenceSlot → VM
    public static SequenceSlotViewModel FromModel(SequenceSlot slot) => slot switch
    {
        LoopBeginSlot lb => new() { SelectedPreset = SlotPreset.All.OfType<LoopBeginPreset>().First(), RepeatCount = lb.RepeatCount },
        LoopEndSlot => new() { SelectedPreset = SlotPreset.All.OfType<LoopEndPreset>().First() },
        WaitSlot w => new() { SelectedPreset = SlotPreset.All.OfType<WaitPreset>().First(), DurationMs = w.DurationMs },
        RandomWaitSlot rw => new()
        {
            SelectedPreset = SlotPreset.All.OfType<RandomWaitPreset>().First(),
            RandomWaitMinMs = rw.MinMs,
            RandomWaitMaxMs = rw.MaxMs,
        },
        BreakpointSlot => new() { SelectedPreset = SlotPreset.All.OfType<BreakpointPreset>().First() },
        KeySingleSlot ks => new()
        {
            SelectedPreset = SlotPreset.All.OfType<KeySinglePreset>().First(),
            SelectedKey = VirtualKeyItem.All.FirstOrDefault(k => k.Code == ks.VirtualKey) ?? VirtualKeyItem.All[0],
            SelectedKeyAction = ks.Action,
            DurationMs = ks.DurationMs,
        },
        KeyTypeStringSlot kts => new()
        {
            SelectedPreset = SlotPreset.All.OfType<KeyTypeStringPreset>().First(),
            TypeText = kts.Text,
            AppendNewline = kts.AppendNewline,
            DurationMs = kts.DurationMs,
        },
        MouseButtonSlot mb => new()
        {
            SelectedPreset = SlotPreset.All.OfType<MouseButtonPreset>().First(),
            SelectedMouseButton = mb.Button,
            SelectedMouseButtonAction = mb.Action,
            DurationMs = mb.DurationMs,
        },
        MouseWheelSlot mw => new()
        {
            SelectedPreset = SlotPreset.All.OfType<MouseWheelPreset>().First(),
            WheelClicks = mw.Clicks,
            DurationMs = mw.DurationMs,
        },
        MouseMoveSlot mm => new()
        {
            SelectedPreset = SlotPreset.All.OfType<MouseMovePreset>().First(),
            MouseMoveX = mm.X,
            MouseMoveY = mm.Y,
            SelectedMouseMoveMode = mm.Mode,
            DurationMs = mm.DurationMs,
        },
        FloatSlot f => BuildOscVm(f.Address, OscValueType.Float, floatVal: f.Value, durationMs: f.DurationMs, resetOnComplete: f.ResetOnComplete,
            transitionMode: f.TransitionMode, floatFromVal: f.TransitionFromValue ?? 0f, floatToVal: f.TransitionToValue ?? 1f),
        IntSlot n => BuildOscVm(n.Address, OscValueType.Int, intVal: n.Value, durationMs: n.DurationMs, resetOnComplete: n.ResetOnComplete,
            transitionMode: n.TransitionMode, intFromVal: n.TransitionFromValue ?? 0, intToVal: n.TransitionToValue ?? 1),
        BoolSlot b => BuildOscVm(b.Address, OscValueType.Bool, boolVal: b.Value, durationMs: b.DurationMs, resetOnComplete: b.ResetOnComplete),
        StringSlot s => BuildOscVm(s.Address, OscValueType.String, strVal: s.Value, durationMs: s.DurationMs, resetOnComplete: s.ResetOnComplete),
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };

    // OSC系VMの組み立てヘルパー
    // アドレスが既知のプリセットに一致すればBuiltinPreset、それ以外はCustomPreset として復元する
    private static SequenceSlotViewModel BuildOscVm(
        string address, OscValueType vt,
        float floatVal = 0f, int intVal = 0, bool boolVal = false,
        string? strVal = null, int durationMs = 500, bool resetOnComplete = true,
        TransitionMode transitionMode = TransitionMode.None,
        float floatFromVal = 0f, float floatToVal = 1f,
        int intFromVal = 0, int intToVal = 1)
    {
        SlotPreset preset = SlotPreset.All.FirstOrDefault(p => p is BuiltinPreset bp && bp.Address == address)
                         ?? SlotPreset.All.First(p => p is CustomPreset);
        return new()
        {
            SelectedPreset = preset,
            FloatValue = floatVal,
            IntValue = intVal,
            BoolValue = boolVal,
            StringValue = preset is CustomPreset ? (strVal ?? string.Empty) : string.Empty,
            CustomValueType = preset is CustomPreset ? vt : OscValueType.Float,
            CustomAddress = preset is CustomPreset ? address : string.Empty,
            DurationMs = durationMs,
            ResetOnComplete = resetOnComplete,
            TransitionMode = preset is CustomPreset ? transitionMode : TransitionMode.None,
            FloatTransitionFrom = floatFromVal,
            FloatTransitionTo = floatToVal,
            IntTransitionFrom = intFromVal,
            IntTransitionTo = intToVal,
        };
    }
}
