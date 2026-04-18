using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel
{
    // ── 固定値 ────────────────────────────────────────────────────────────

    // カスタムFloatスロットの送信値
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial float FloatValue { get; set; }

    // カスタムIntスロットの送信値
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsValueOn))]
    [NotifyPropertyChangedFor(nameof(IsValueOff))]
    public partial int IntValue { get; set; }

    // カスタムBoolスロットの送信値
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial bool BoolValue { get; set; }

    // カスタムStringスロットの送信値
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial string StringValue { get; set; } = "";

    // RadioButtonバインディング用: IntValueのON/OFF => bool
    // （BuiltinPresetのInt型専用）
    public bool IsValueOn
    {
        get => IntValue == 1;
        set { if (value) IntValue = 1; }
    }

    public bool IsValueOff
    {
        get => IntValue == 0;
        set { if (value) IntValue = 0; }
    }

    // ── トランジション ────────────────────────────────────────────────────

    // 補間方式（None=固定値、Linear/EaseIn/EaseOut/EaseInOut=時間補間）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTransitionMode))]
    [NotifyPropertyChangedFor(nameof(IsFixedValueMode))]
    [NotifyPropertyChangedFor(nameof(SelectedTransitionModeIndex))]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial TransitionMode TransitionMode { get; set; } = TransitionMode.None;

    // Floatトランジションの開始値・終了値
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial float FloatTransitionFrom { get; set; } = 0f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial float FloatTransitionTo { get; set; } = 1f;

    // Intトランジションの開始値・終了値
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int IntTransitionFrom { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int IntTransitionTo { get; set; } = 1;

    // トランジションUIの表示切り替え用
    // （Float/IntかつCustomPresetのときのみ有効）
    public bool IsTransitionAvailable =>
        SelectedPreset is CustomPreset && CustomValueType is OscValueType.Float or OscValueType.Int;

    // 固定値入力欄とFrom→To入力欄のVisibility切り替え用
    public bool IsTransitionMode => TransitionMode != TransitionMode.None;
    public bool IsFixedValueMode => TransitionMode == TransitionMode.None;

    // ComboBoxのSelectedIndex バインディング用
    // TransitionMode enumをintに変換
    public int SelectedTransitionModeIndex
    {
        get => (int)TransitionMode;
        set => TransitionMode = (TransitionMode)value;
    }

    // ── サマリー表示 ──────────────────────────────────────────────────────

    // ParameterSummaryのOSC値部分（カスタムスロット専用）
    private string ValueSummary => CustomValueType switch
    {
        OscValueType.Float when IsTransitionMode =>
            $"{FloatTransitionFrom:0.###} → {FloatTransitionTo:0.###} [{TransitionModeLabel(TransitionMode)}]",
        OscValueType.Int when IsTransitionMode =>
            $"{IntTransitionFrom} → {IntTransitionTo} [{TransitionModeLabel(TransitionMode)}]",
        OscValueType.Int => $"{IntValue}",
        OscValueType.Bool => BoolValue ? "true" : "false",
        OscValueType.String => $"\"{StringValue}\"",
        _ => $"{FloatValue:0.###}",
    };

    private static string TransitionModeLabel(TransitionMode mode) => mode switch
    {
        TransitionMode.Linear => "Linear",
        TransitionMode.EaseIn => "EaseIn",
        TransitionMode.EaseOut => "EaseOut",
        TransitionMode.EaseInOut => "EaseInOut",
        _ => "",
    };

    // ── ガード処理 ────────────────────────────────────────────────────────

    // Bool/Stringに切り替えたときトランジション設定をリセット
    partial void OnCustomValueTypeChanged(OscValueType value)
    {
        if (value is not OscValueType.Float and not OscValueType.Int)
            TransitionMode = TransitionMode.None;
    }

    // カスタム以外のプリセットに切り替えたときトランジション設定をリセット
    partial void OnSelectedPresetChanged(SlotPreset value)
    {
        if (value is not CustomPreset)
            TransitionMode = TransitionMode.None;
    }
}
