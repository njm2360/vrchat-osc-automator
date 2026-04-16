using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel : ObservableObject
{
    public static IReadOnlyList<SlotPreset> AvailablePresets => SlotPreset.All;
    public static IReadOnlyList<OscValueType> AvailableValueTypes => [OscValueType.Float, OscValueType.Int, OscValueType.Bool, OscValueType.String];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowResetOption))]
    [NotifyPropertyChangedFor(nameof(IsDurationEditable))]
    [NotifyPropertyChangedFor(nameof(IsFloatMode))]
    [NotifyPropertyChangedFor(nameof(IsIntMode))]
    [NotifyPropertyChangedFor(nameof(IsBoolMode))]
    [NotifyPropertyChangedFor(nameof(IsStringMode))]
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
    private int _durationMs = 500;

    [ObservableProperty]
    private bool _resetOnComplete = true;

    [ObservableProperty]
    private bool _isCurrentSlot;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int RepeatCount { get; set; } = 2;

    /// <summary>Bool 型の値を bool として読み書きするビュー用プロパティ。</summary>
    public bool BoolValue
    {
        get => Value != 0f;
        set => Value = value ? 1f : 0f;
    }

    /// <summary>カスタムプリセットでアドレスが '/' 始まりでない場合は無効。</summary>
    public bool IsValid => !SelectedPreset.IsCustom || CustomAddress.StartsWith('/');

    /// <summary>ResetOnComplete を表示すべきスロット（定義済みOSCプリセットのみ）。</summary>
    public bool ShowResetOption => SelectedPreset.IsBuiltinPreset;

    /// <summary>待機時間を設定できるスロット（ループマーカー以外）。</summary>
    public bool IsDurationEditable => !SelectedPreset.IsLoopMarker;

    /// <summary>OSC 送信スロットの実効値型。待機・ループマーカーは null。</summary>
    private OscValueType? EffectiveValueType =>
        SelectedPreset.IsBuiltinPreset ? SelectedPreset.ValueType :
        SelectedPreset.IsCustom        ? CustomValueType :
        null;

    public bool IsFloatMode  => EffectiveValueType == OscValueType.Float;
    public bool IsIntMode    => EffectiveValueType == OscValueType.Int;
    public bool IsBoolMode   => EffectiveValueType == OscValueType.Bool;
    public bool IsStringMode => EffectiveValueType == OscValueType.String;

    public string ParameterSummary => SelectedPreset switch
    {
        { IsLoopBegin: true } => RepeatCount == 0 ? "× ∞ 回" : $"× {RepeatCount} 回",
        { IsLoopEnd: true } => "—",
        { IsWait: true } => "—",
        { IsCustom: true } => CustomAddress.Length > 0
                                    ? $"{CustomAddress} [{CustomValueType}] = {ValueSummary}"
                                    : $"(アドレス未設定) [{CustomValueType}] = {ValueSummary}",
        _ => SelectedPreset.IsBuiltinInt
                ? ((int)Value == 1 ? "1 (ON)" : "0 (OFF)")
                : $"{Value:F2}",
    };

    private string ValueSummary => CustomValueType switch
    {
        OscValueType.Int => $"{(int)Value}",
        OscValueType.Bool => Value != 0f ? "true" : "false",
        OscValueType.String => $"\"{StringValue}\"",
        _ => $"{Value:F3}",
    };

    public SequenceSlot ToModel()
    {
        if (SelectedPreset.IsLoopBegin) return new LoopBeginSlot(RepeatCount);
        if (SelectedPreset.IsLoopEnd) return new LoopEndSlot();
        if (SelectedPreset.IsWait) return new WaitSlot(DurationMs);

        string address = SelectedPreset.IsCustom ? CustomAddress : SelectedPreset.Address!;
        OscValueType vt = SelectedPreset.IsCustom ? CustomValueType : SelectedPreset.ValueType;

        return vt switch
        {
            OscValueType.Int => new IntSlot(address, (int)Value, DurationMs, ResetOnComplete),
            OscValueType.Bool => new BoolSlot(address, Value != 0f, DurationMs, ResetOnComplete),
            OscValueType.String => new StringSlot(address, StringValue, DurationMs, ResetOnComplete),
            _ => new FloatSlot(address, Value, DurationMs, ResetOnComplete),
        };
    }

    public static SequenceSlotViewModel FromModel(SequenceSlot slot) => slot switch
    {
        LoopBeginSlot lb => new() { SelectedPreset = SlotPreset.All.First(p => p.IsLoopBegin), RepeatCount = lb.RepeatCount },
        LoopEndSlot => new() { SelectedPreset = SlotPreset.All.First(p => p.IsLoopEnd) },
        WaitSlot w => new() { SelectedPreset = SlotPreset.All.First(p => p.IsWait), DurationMs = w.DurationMs },
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
        SlotPreset preset = SlotPreset.All.FirstOrDefault(
                                p => p.Address == address && !p.IsLoopBegin && !p.IsLoopEnd && !p.IsWait)
                         ?? SlotPreset.All.First(p => p.IsCustom);
        return new()
        {
            SelectedPreset = preset,
            Value = floatVal,
            StringValue = preset.IsCustom ? (strVal ?? string.Empty) : string.Empty,
            CustomValueType = preset.IsCustom ? vt : OscValueType.Float,
            CustomAddress = preset.IsCustom ? address : string.Empty,
            DurationMs = durationMs,
            ResetOnComplete = resetOnComplete,
        };
    }
}
