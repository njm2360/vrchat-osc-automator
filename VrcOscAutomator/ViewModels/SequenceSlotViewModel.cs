using System.Diagnostics;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;
using KeyAction = VrcOscAutomator.Models.KeyAction;

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
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(Value))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private SlotPreset _selectedPreset = SlotPreset.All[0];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsFloatMode))]
    [NotifyPropertyChangedFor(nameof(IsIntMode))]
    [NotifyPropertyChangedFor(nameof(IsBoolMode))]
    [NotifyPropertyChangedFor(nameof(IsStringMode))]
    private OscValueType _customValueType = OscValueType.Float;

    partial void OnCustomValueTypeChanged(OscValueType value) => Value = 0f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(BoolValue))]
    private float _value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    private string _customAddress = string.Empty;

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

    /// <summary>Bool 型の値を bool として読み書きするビュー用プロパティ。</summary>
    public bool BoolValue
    {
        get => Value != 0f;
        set => Value = value ? 1f : 0f;
    }

    public bool IsValid => SelectedPreset is not CustomPreset || OscAddressRegex().IsMatch(CustomAddress);

    public bool ShowResetOption => SelectedPreset is BuiltinPreset;

    public bool IsDurationEditable => SelectedPreset is not (LoopBeginPreset or LoopEndPreset);

    public bool IsKeyboardSingleMode => SelectedPreset is KeySinglePreset;

    public bool IsKeyboardTypeStringMode => SelectedPreset is KeyTypeStringPreset;

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
        LoopBeginPreset => RepeatCount == 0 ? "× ∞ 回" : $"× {RepeatCount} 回",
        LoopEndPreset => "—",
        WaitPreset => "—",
        KeySinglePreset => $"{SelectedKey.Name} [{(SelectedKeyAction == KeyAction.Press ? "PRESS" : "RELEASE")}]",
        KeyTypeStringPreset => TypeText.Length == 0
            ? "(未入力)"
            : $"\"{Truncate(TypeText, 20)}\"{(AppendNewline ? " ↵" : "")}",
        CustomPreset => CustomAddress.Length > 0
                               ? $"{CustomAddress} [{CustomValueType}] = {ValueSummary}"
                               : $"(アドレス未設定) [{CustomValueType}] = {ValueSummary}",
        BuiltinPreset { ValueType: OscValueType.Int } => (int)Value == 1 ? "1 (ON)" : "0 (OFF)",
        _ => $"{Value:F2}",
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

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
        KeySinglePreset => new KeySingleSlot(SelectedKey.Code, SelectedKeyAction, DurationMs),
        KeyTypeStringPreset => new KeyTypeStringSlot(TypeText, AppendNewline, DurationMs),
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
