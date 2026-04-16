using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;
using KeyAction = VrcOscAutomator.Models.KeyAction;
using MouseButton = VrcOscAutomator.Models.MouseButton;
using MouseMoveMode = VrcOscAutomator.Models.MouseMoveMode;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel : ObservableObject
{
    [GeneratedRegex(@"^(/[^ #*,?/\[\]{}]+)+$")]
    private static partial Regex OscAddressRegex();

    public static IReadOnlyList<SlotPreset> AvailablePresets => SlotPreset.All;
    public static IReadOnlyList<OscValueType> AvailableValueTypes => [OscValueType.Float, OscValueType.Int, OscValueType.Bool, OscValueType.String];
    public static IReadOnlyList<VirtualKeyItem> AvailableKeys => VirtualKeyItem.All;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowResetOption))]
    [NotifyPropertyChangedFor(nameof(IsDurationEditable))]
    [NotifyPropertyChangedFor(nameof(IsFloatMode))]
    [NotifyPropertyChangedFor(nameof(IsIntMode))]
    [NotifyPropertyChangedFor(nameof(IsBoolMode))]
    [NotifyPropertyChangedFor(nameof(IsStringMode))]
    [NotifyPropertyChangedFor(nameof(IsKeyboardSingleMode))]
    [NotifyPropertyChangedFor(nameof(IsKeyboardTypeStringMode))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonMode))]
    [NotifyPropertyChangedFor(nameof(IsMouseWheelMode))]
    [NotifyPropertyChangedFor(nameof(IsMouseMoveMode))]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(Value))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial SlotPreset SelectedPreset { get; set; } = SlotPreset.All[0];


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsFloatMode))]
    [NotifyPropertyChangedFor(nameof(IsIntMode))]
    [NotifyPropertyChangedFor(nameof(IsBoolMode))]
    [NotifyPropertyChangedFor(nameof(IsStringMode))]
    public partial OscValueType CustomValueType { get; set; } = OscValueType.Float;


    partial void OnCustomValueTypeChanged(OscValueType value) => Value = 0f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(BoolValue))]
    public partial float Value { get; set; }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string CustomAddress { get; set; } = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial string StringValue { get; set; } = "";

    [ObservableProperty]
    public partial int DurationMs { get; set; } = 500;

    [ObservableProperty]
    public partial bool ResetOnComplete { get; set; } = true;

    [ObservableProperty]
    public partial bool IsCurrentSlot { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int RepeatCount { get; set; } = 2;

    // ── キーボード (単押し) ────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    private VirtualKeyItem _selectedKey = VirtualKeyItem.All[0];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsKeyActionPress))]
    [NotifyPropertyChangedFor(nameof(IsKeyActionRelease))]
    private KeyAction _selectedKeyAction = KeyAction.Press;

    /// <summary>RadioButton バインディング用。</summary>
    public bool IsKeyActionPress
    {
        get => SelectedKeyAction == KeyAction.Press;
        set { if (value) SelectedKeyAction = KeyAction.Press; }
    }

    public bool IsKeyActionRelease
    {
        get => SelectedKeyAction == KeyAction.Release;
        set { if (value) SelectedKeyAction = KeyAction.Release; }
    }

    // ── キーボード (文字入力) ─────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    private string _typeText = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    private bool _appendNewline;

    // ── マウスボタン ──────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonLeft))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonRight))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonMiddle))]
    private MouseButton _selectedMouseButton = MouseButton.Left;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonActionPress))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonActionRelease))]
    private KeyAction _selectedMouseButtonAction = KeyAction.Press;

    public bool IsMouseButtonActionPress
    {
        get => SelectedMouseButtonAction == KeyAction.Press;
        set { if (value) SelectedMouseButtonAction = KeyAction.Press; }
    }

    public bool IsMouseButtonActionRelease
    {
        get => SelectedMouseButtonAction == KeyAction.Release;
        set { if (value) SelectedMouseButtonAction = KeyAction.Release; }
    }

    public bool IsMouseButtonLeft { get => SelectedMouseButton == MouseButton.Left; set { if (value) SelectedMouseButton = MouseButton.Left; } }
    public bool IsMouseButtonRight { get => SelectedMouseButton == MouseButton.Right; set { if (value) SelectedMouseButton = MouseButton.Right; } }
    public bool IsMouseButtonMiddle { get => SelectedMouseButton == MouseButton.Middle; set { if (value) SelectedMouseButton = MouseButton.Middle; } }

    // ── マウスホイール ────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    private int _wheelClicks = 1;

    // ── マウス移動 ────────────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    private int _mouseMoveX;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    private int _mouseMoveY;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsMouseMoveRelative))]
    [NotifyPropertyChangedFor(nameof(IsMouseMoveAbsolute))]
    private MouseMoveMode _selectedMouseMoveMode = MouseMoveMode.Relative;

    public bool IsMouseMoveRelative
    {
        get => SelectedMouseMoveMode == MouseMoveMode.Relative;
        set { if (value) SelectedMouseMoveMode = MouseMoveMode.Relative; }
    }

    public bool IsMouseMoveAbsolute
    {
        get => SelectedMouseMoveMode == MouseMoveMode.Absolute;
        set { if (value) SelectedMouseMoveMode = MouseMoveMode.Absolute; }
    }

    /// <summary>Bool 型の値を bool として読み書きするビュー用プロパティ。</summary>
    public bool BoolValue
    {
        get => Value != 0f;
        set => Value = value ? 1f : 0f;
    }

    public bool IsValid => SelectedPreset is not CustomPreset || OscAddressRegex().IsMatch(CustomAddress);

    public bool ShowResetOption => SelectedPreset is BuiltinPreset;

    public bool IsDurationEditable => SelectedPreset is not (LoopBeginPreset or LoopEndPreset or BreakpointPreset);

    public bool IsKeyboardSingleMode => SelectedPreset is KeySinglePreset;
    public bool IsKeyboardTypeStringMode => SelectedPreset is KeyTypeStringPreset;
    public bool IsMouseButtonMode => SelectedPreset is MouseButtonPreset;
    public bool IsMouseWheelMode => SelectedPreset is MouseWheelPreset;
    public bool IsMouseMoveMode => SelectedPreset is MouseMovePreset;

    private OscValueType? EffectiveValueType => SelectedPreset switch
    {
        BuiltinPreset { ValueType: var vt } => vt,
        CustomPreset => CustomValueType,
        _ => null,
    };

    public bool IsFloatMode => EffectiveValueType == OscValueType.Float;
    public bool IsIntMode => EffectiveValueType == OscValueType.Int;
    public bool IsBoolMode => EffectiveValueType == OscValueType.Bool;
    public bool IsStringMode => EffectiveValueType == OscValueType.String;

    public string ParameterSummary => SelectedPreset switch
    {
        LoopBeginPreset => RepeatCount == 0 ? "エンドレス" : $"x {RepeatCount} 回",
        LoopEndPreset => "—",
        WaitPreset => "—",
        BreakpointPreset => "—",
        KeySinglePreset => $"{SelectedKey.Name} [{(SelectedKeyAction == KeyAction.Press ? "押す" : "離す")}]",
        KeyTypeStringPreset => TypeText.Length == 0
            ? "(未入力)"
            : $"\"{Truncate(TypeText, 20)}\"{(AppendNewline ? " ↵" : "")}",
        MouseButtonPreset => $"{MouseButtonLabel(SelectedMouseButton)} [{(SelectedMouseButtonAction == KeyAction.Press ? "押す" : "離す")}]",
        MouseWheelPreset => WheelClicks > 0 ? $"↑ {WheelClicks} クリック" : WheelClicks < 0 ? $"↓ {-WheelClicks} クリック" : "0",
        MouseMovePreset => SelectedMouseMoveMode == MouseMoveMode.Relative
            ? $"相対 (Δ{MouseMoveX:+#;-#;0}, Δ{MouseMoveY:+#;-#;0})"
            : $"絶対 ({MouseMoveX}, {MouseMoveY})",
        CustomPreset => CustomAddress.Length > 0
                               ? $"{CustomAddress} [{CustomValueType}] = {ValueSummary}"
                               : $"(アドレス未設定) [{CustomValueType}] = {ValueSummary}",
        BuiltinPreset { ValueType: OscValueType.Int } => (int)Value == 1 ? "1 (ON)" : "0 (OFF)",
        _ => $"{Value:F2}",
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private static string MouseButtonLabel(MouseButton b) => b switch
    {
        MouseButton.Left => "左ボタン",
        MouseButton.Right => "右ボタン",
        MouseButton.Middle => "中ボタン",
        _ => "?",
    };

    private string ValueSummary => CustomValueType switch
    {
        OscValueType.Int => $"{(int)Value}",
        OscValueType.Bool => Value != 0f ? "true" : "false",
        OscValueType.String => $"\"{StringValue}\"",
        _ => $"{Value:F3}",
    };

    public SequenceSlot ToModel() => SelectedPreset switch
    {
        LoopBeginPreset => new LoopBeginSlot(RepeatCount),
        LoopEndPreset => new LoopEndSlot(),
        WaitPreset => new WaitSlot(DurationMs),
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

    private SequenceSlot OscSlot(string address, OscValueType vt) => vt switch
    {
        OscValueType.Int => new IntSlot(address, (int)Value, DurationMs, ResetOnComplete),
        OscValueType.Bool => new BoolSlot(address, Value != 0f, DurationMs, ResetOnComplete),
        OscValueType.String => new StringSlot(address, StringValue, DurationMs, ResetOnComplete),
        _ => new FloatSlot(address, Value, DurationMs, ResetOnComplete),
    };

    public static SequenceSlotViewModel FromModel(SequenceSlot slot) => slot switch
    {
        LoopBeginSlot lb => new() { SelectedPreset = SlotPreset.All.OfType<LoopBeginPreset>().First(), RepeatCount = lb.RepeatCount },
        LoopEndSlot => new() { SelectedPreset = SlotPreset.All.OfType<LoopEndPreset>().First() },
        WaitSlot w => new() { SelectedPreset = SlotPreset.All.OfType<WaitPreset>().First(), DurationMs = w.DurationMs },
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
        FloatSlot f => BuildOscVm(f.Address, OscValueType.Float, f.Value, null, f.DurationMs, f.ResetOnComplete),
        IntSlot n => BuildOscVm(n.Address, OscValueType.Int, n.Value, null, n.DurationMs, n.ResetOnComplete),
        BoolSlot b => BuildOscVm(b.Address, OscValueType.Bool, b.Value ? 1f : 0f, null, b.DurationMs, b.ResetOnComplete),
        StringSlot s => BuildOscVm(s.Address, OscValueType.String, 0f, s.Value, s.DurationMs, s.ResetOnComplete),
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };

    private static SequenceSlotViewModel BuildOscVm(
        string address, OscValueType vt, float floatVal,
        string? strVal, int durationMs, bool resetOnComplete)
    {
        SlotPreset preset = SlotPreset.All.FirstOrDefault(p => p is BuiltinPreset bp && bp.Address == address)
                         ?? SlotPreset.All.First(p => p is CustomPreset);
        return new()
        {
            SelectedPreset = preset,
            Value = floatVal,
            StringValue = preset is CustomPreset ? (strVal ?? string.Empty) : string.Empty,
            CustomValueType = preset is CustomPreset ? vt : OscValueType.Float,
            CustomAddress = preset is CustomPreset ? address : string.Empty,
            DurationMs = durationMs,
            ResetOnComplete = resetOnComplete,
        };
    }
}
