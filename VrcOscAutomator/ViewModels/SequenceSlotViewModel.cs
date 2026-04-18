using System.Text.RegularExpressions;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;
using KeyAction = VrcOscAutomator.Models.KeyAction;
using MouseButton = VrcOscAutomator.Models.MouseButton;
using MouseMoveMode = VrcOscAutomator.Models.MouseMoveMode;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel : ObservableObject
{
    // ── 静的リソース ──────────────────────────────────────────────────────

    // OSCアドレスのバリデーション用正規表現（'/' 始まり、特殊文字禁止）
    [GeneratedRegex(@"^(/[^ #*,?/\[\]{}]+)+$")]
    private static partial Regex OscAddressRegex();

    // コマンド選択 ComboBox 用: カテゴリでグループ化済みのプリセット一覧
    public static ListCollectionView AvailablePresets { get; } = CreateGroupedPresets();

    private static ListCollectionView CreateGroupedPresets()
    {
        var view = new ListCollectionView(SlotPreset.All as System.Collections.IList ?? SlotPreset.All.ToList());
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SlotPreset.Category)));
        return view;
    }

    // 型選択ComboBox
    public static IReadOnlyList<OscValueType> AvailableValueTypes => [OscValueType.Int, OscValueType.Float, OscValueType.Bool, OscValueType.String];
    // キー選択ComboBox
    public static IReadOnlyList<VirtualKeyItem> AvailableKeys => VirtualKeyItem.All;

    // ── 共通プロパティ ────────────────────────────────────────────────────

    // 選択中のプリセット
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
    [NotifyPropertyChangedFor(nameof(IsRandomWaitMode))]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    [NotifyPropertyChangedFor(nameof(IsTransitionAvailable))]
    public partial SlotPreset SelectedPreset { get; set; } = SlotPreset.All[0];

    // カスタムスロット選択時のOSC値型
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsFloatMode))]
    [NotifyPropertyChangedFor(nameof(IsIntMode))]
    [NotifyPropertyChangedFor(nameof(IsBoolMode))]
    [NotifyPropertyChangedFor(nameof(IsStringMode))]
    [NotifyPropertyChangedFor(nameof(IsTransitionAvailable))]
    public partial OscValueType CustomValueType { get; set; } = OscValueType.Int;

    // カスタムスロット選択時のOSCアドレス
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsValid))]
    public partial string CustomAddress { get; set; } = string.Empty;

    // スロット実行時間
    [ObservableProperty]
    public partial int DurationMs { get; set; } = 500;

    // 実行完了後に値を0に戻すか(OSC用)
    [ObservableProperty]
    public partial bool ResetOnComplete { get; set; } = true;

    // このスロットが現在実行中かどうか
    [ObservableProperty]
    public partial bool IsCurrentSlot { get; set; }

    // ループの現在回数（サマリ表示用）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int CurrentIteration { get; set; }

    // ループ回数（0=エンドレス）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int RepeatCount { get; set; } = 2;

    // ランダム待機の最小・最大時間
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int RandomWaitMinMs { get; set; } = 300;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int RandomWaitMaxMs { get; set; } = 1000;

    // ── UI 表示切り替え用プロパティ ───────────────────────────────────────

    // OSCアドレスが有効な形式か
    public bool IsValid => SelectedPreset is not CustomPreset || OscAddressRegex().IsMatch(CustomAddress);
    // 完了後に0に戻すチェックボックスを表示するか
    public bool ShowResetOption => SelectedPreset is BuiltinPreset;
    // 時間入力欄が編集可能か
    public bool IsDurationEditable => SelectedPreset is not (LoopBeginPreset or LoopEndPreset or BreakpointPreset or RandomWaitPreset);

    // 各コマンド種別の入力パネルVisibility切り替え用
    public bool IsRandomWaitMode => SelectedPreset is RandomWaitPreset;
    public bool IsKeyboardSingleMode => SelectedPreset is KeySinglePreset;
    public bool IsKeyboardTypeStringMode => SelectedPreset is KeyTypeStringPreset;
    public bool IsMouseButtonMode => SelectedPreset is MouseButtonPreset;
    public bool IsMouseWheelMode => SelectedPreset is MouseWheelPreset;
    public bool IsMouseMoveMode => SelectedPreset is MouseMovePreset;

    // OSC値入力パネルの型別Visibility切り替え用
    // BuiltinPresetはValueType、CustomPresetはCustomValueTypeに従う
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

    // ── DataGrid サマリー列 ───────────────────────────────────────────────

    public string ParameterSummary => SelectedPreset switch
    {
        LoopBeginPreset => CurrentIteration > 0
            ? (RepeatCount == 0 ? $"{CurrentIteration} 回目" : $"{CurrentIteration} / {RepeatCount} 回")
            : (RepeatCount == 0 ? "エンドレス" : $"x {RepeatCount} 回"),
        LoopEndPreset => "—",
        WaitPreset => "—",
        RandomWaitPreset => $"{RandomWaitMinMs}ms 〜 {RandomWaitMaxMs}ms",
        BreakpointPreset => "—",
        KeySinglePreset => $"{SelectedKey.Name} [{SelectedKeyAction switch { KeyAction.Press => "押す", KeyAction.Release => "離す", _ => "押して離す" }}]",
        KeyTypeStringPreset => TypeText.Length == 0
            ? "(未入力)"
            : $"\"{Truncate(TypeText, 20)}\"{(AppendNewline ? " ↵" : "")}",
        MouseButtonPreset => $"{MouseButtonLabel(SelectedMouseButton)} [{SelectedMouseButtonAction switch { KeyAction.Press => "押す", KeyAction.Release => "離す", _ => "押して離す" }}]",
        MouseWheelPreset => WheelClicks > 0 ? $"↑ {WheelClicks} クリック" : WheelClicks < 0 ? $"↓ {-WheelClicks} クリック" : "0",
        MouseMovePreset => SelectedMouseMoveMode == MouseMoveMode.Relative
            ? $"相対 (Δ{MouseMoveX:+#;-#;0}, Δ{MouseMoveY:+#;-#;0})"
            : $"絶対 ({MouseMoveX}, {MouseMoveY})",
        CustomPreset => CustomAddress.Length > 0
                               ? $"{CustomAddress} [{CustomValueType}] = {ValueSummary}"
                               : $"(アドレス未設定) [{CustomValueType}] = {ValueSummary}",
        BuiltinPreset { ValueType: OscValueType.Int } => IntValue == 1 ? "1 (ON)" : "0 (OFF)",
        _ => $"{FloatValue:0.##}",
    };

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "…";

    private static string MouseButtonLabel(MouseButton b) => b switch
    {
        MouseButton.Left => "左ボタン",
        MouseButton.Right => "右ボタン",
        MouseButton.Middle => "中ボタン",
        _ => "?",
    };
}
