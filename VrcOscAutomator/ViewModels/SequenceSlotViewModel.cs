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

    /// <summary>ResetOnComplete を表示すべきスロット（待機・ループマーカー・カスタム以外）。</summary>
    public bool ShowResetOption => !SelectedPreset.IsWait && !SelectedPreset.IsLoopBegin && !SelectedPreset.IsLoopEnd && !SelectedPreset.IsCustom;

    /// <summary>待機時間を設定できるスロット（ループマーカー以外）。</summary>
    public bool IsDurationEditable => !SelectedPreset.IsLoopBegin && !SelectedPreset.IsLoopEnd;

    /// <summary>現在のスロットが Float 値を送るモード（Float型定義済みプリセット or カスタムFloat）。</summary>
    public bool IsFloatMode => SelectedPreset.IsBuiltinFloat || (SelectedPreset.IsCustom && CustomValueType == OscValueType.Float);

    /// <summary>現在のスロットが Int 値を送るモード（Int型定義済みプリセット or カスタムInt）。</summary>
    public bool IsIntMode => SelectedPreset.IsBuiltinInt || (SelectedPreset.IsCustom && CustomValueType == OscValueType.Int);

    /// <summary>カスタムかつ Bool 型。</summary>
    public bool IsBoolMode => SelectedPreset.IsCustom && CustomValueType == OscValueType.Bool;

    /// <summary>カスタムかつ String 型。</summary>
    public bool IsStringMode => SelectedPreset.IsCustom && CustomValueType == OscValueType.String;

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
        if (SelectedPreset.IsLoopBegin)
            return new() { SlotType = SlotType.LoopBegin, RepeatCount = RepeatCount };
        if (SelectedPreset.IsLoopEnd)
            return new() { SlotType = SlotType.LoopEnd };

        return new()
        {
            Address = SelectedPreset.IsWait ? null
                            : SelectedPreset.IsCustom ? CustomAddress
                            : SelectedPreset.Address,
            Value = SelectedPreset.IsWait ? 0f : Value,
            StringValue = SelectedPreset.IsCustom && CustomValueType == OscValueType.String
                            ? StringValue : string.Empty,
            ValueType = SelectedPreset.IsCustom ? CustomValueType : SelectedPreset.ValueType,
            DurationMs = DurationMs,
            ResetOnComplete = ResetOnComplete,
            SlotType = SlotType.Normal,
        };
    }

    public static SequenceSlotViewModel FromModel(SequenceSlot slot)
    {
        SlotPreset preset = slot.SlotType switch
        {
            SlotType.LoopBegin => SlotPreset.All.First(p => p.IsLoopBegin),
            SlotType.LoopEnd => SlotPreset.All.First(p => p.IsLoopEnd),
            _ => slot.Address switch
            {
                null => SlotPreset.All.First(p => p.IsWait),
                _ => SlotPreset.All.FirstOrDefault(p => p.Address == slot.Address)
                        ?? SlotPreset.All.First(p => p.IsCustom),
            },
        };

        return new()
        {
            SelectedPreset = preset,
            Value = slot.Value,
            StringValue = preset.IsCustom ? slot.StringValue : string.Empty,
            CustomValueType = preset.IsCustom ? slot.ValueType : OscValueType.Float,
            CustomAddress = preset.IsCustom ? (slot.Address ?? string.Empty) : string.Empty,
            DurationMs = slot.DurationMs,
            ResetOnComplete = slot.ResetOnComplete,
            RepeatCount = slot.RepeatCount,
        };
    }
}
