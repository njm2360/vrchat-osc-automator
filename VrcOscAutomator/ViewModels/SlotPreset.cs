using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed record SlotPreset(string Name, string? Address, OscValueType ValueType = OscValueType.Float)
{
    // ループマーカー識別用センチネル
    internal const string LoopBeginSentinel = "__LOOP_BEGIN__";
    internal const string LoopEndSentinel = "__LOOP_END__";

    /// <summary>既知のアドレスを持つ定義済みプリセット（待機・ループ・カスタム以外）。</summary>
    public bool IsBuiltinPreset => Address is { Length: > 0 } && !IsLoopBegin && !IsLoopEnd;

    /// <summary>Float 型の定義済みプリセット。</summary>
    public bool IsBuiltinFloat => IsBuiltinPreset && ValueType == OscValueType.Float;

    /// <summary>Int 型の定義済みプリセット。</summary>
    public bool IsBuiltinInt => IsBuiltinPreset && ValueType == OscValueType.Int;

    /// <summary>ユーザーがアドレスを手入力するプリセット。</summary>
    public bool IsCustom => Address is { Length: 0 };

    /// <summary>OSC を送信しない待機プリセット。</summary>
    public bool IsWait => Address is null;

    /// <summary>繰り返しブロックの開始マーカー。</summary>
    public bool IsLoopBegin => Address == LoopBeginSentinel;

    /// <summary>繰り返しブロックの終了マーカー。</summary>
    public bool IsLoopEnd => Address == LoopEndSentinel;

    // ─── 定義済みプリセット一覧 ───────────────────────────────────────
    public static readonly IReadOnlyList<SlotPreset> All =
    [
        // シーケンス制御
        new("待機",                 null),
        new("繰り返し開始",         LoopBeginSentinel),
        new("繰り返し終了",         LoopEndSentinel),

        // Float (-1.0 〜 +1.0)
        new("前後(軸)",             "/input/Vertical",       OscValueType.Float),
        new("左右(軸)",             "/input/Horizontal",     OscValueType.Float),
        new("水平視点回転",         "/input/LookHorizontal", OscValueType.Float),
        new("右手使用(軸)",         "/input/UseAxisRight",   OscValueType.Float),
        new("右手グラブ(軸)",       "/input/GrabAxisRight",  OscValueType.Float),
        new("保持:前後",            "/input/MoveHoldFB",     OscValueType.Float),
        new("保持:回転(時計回り)",  "/input/SpinHoldCwCcw",  OscValueType.Float),
        new("保持:回転(上下)",      "/input/SpinHoldUD",     OscValueType.Float),
        new("保持:回転(左右)",      "/input/SpinHoldLR",     OscValueType.Float),

        // Int 移動 (0 / 1)
        new("前進",                 "/input/MoveForward",           OscValueType.Int),
        new("後退",                 "/input/MoveBackward",          OscValueType.Int),
        new("左移動",               "/input/MoveLeft",              OscValueType.Int),
        new("右移動",               "/input/MoveRight",             OscValueType.Int),
        new("左旋回",               "/input/LookLeft",              OscValueType.Int),
        new("右旋回",               "/input/LookRight",             OscValueType.Int),
        new("ジャンプ",             "/input/Jump",                  OscValueType.Int),
        new("走る",                 "/input/Run",                   OscValueType.Int),
        new("スナップターン左",     "/input/ComfortLeft",           OscValueType.Int),
        new("スナップターン右",     "/input/ComfortRight",          OscValueType.Int),

        // Int 手操作 (0 / 1)
        new("右手グラブ",           "/input/GrabRight",             OscValueType.Int),
        new("右手使用",             "/input/UseRight",              OscValueType.Int),
        new("右手ドロップ",         "/input/DropRight",             OscValueType.Int),
        new("左手グラブ",           "/input/GrabLeft",              OscValueType.Int),
        new("左手使用",             "/input/UseLeft",               OscValueType.Int),
        new("左手ドロップ",         "/input/DropLeft",              OscValueType.Int),

        // Int その他 (0 / 1)
        new("パニックボタン",       "/input/PanicButton",           OscValueType.Int),
        new("クイックメニュー左",   "/input/QuickMenuToggleLeft",   OscValueType.Int),
        new("クイックメニュー右",   "/input/QuickMenuToggleRight",  OscValueType.Int),
        new("ボイス",               "/input/Voice",                 OscValueType.Int),

        // カスタム
        new("カスタム",             "",   OscValueType.Float),
    ];
}
