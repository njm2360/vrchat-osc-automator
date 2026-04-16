using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public abstract record SlotPreset(string Name)
{
    // ── XAML バインディング用プロパティ──
    public bool IsBuiltinPreset => this is BuiltinPreset;
    public bool IsBuiltinFloat => this is BuiltinPreset { ValueType: OscValueType.Float };
    public bool IsBuiltinInt => this is BuiltinPreset { ValueType: OscValueType.Int };
    public bool IsCustom => this is CustomPreset;
    public bool IsWait => this is WaitPreset;
    public bool IsLoopBegin => this is LoopBeginPreset;
    public bool IsLoopEnd => this is LoopEndPreset;
    public bool IsLoopMarker => this is LoopBeginPreset or LoopEndPreset;
    public bool IsBreakpoint => this is BreakpointPreset;
    public bool IsKeyboardSingle => this is KeySinglePreset;
    public bool IsKeyboardTypeString => this is KeyTypeStringPreset;
    public bool IsMouseButton => this is MouseButtonPreset;
    public bool IsMouseWheel => this is MouseWheelPreset;
    public bool IsMouseMove => this is MouseMovePreset;

    // ─── 定義済みプリセット一覧 ───────────────────────────────────────
    public static readonly IReadOnlyList<SlotPreset> All =
    [
        // シーケンス制御
        new WaitPreset("待機"),
        new LoopBeginPreset("繰り返し開始"),
        new LoopEndPreset("繰り返し終了"),
        new BreakpointPreset("ブレークポイント"),

        // Float (-1.0 〜 +1.0)
        new BuiltinPreset("前後(軸)",             "/input/Vertical",              OscValueType.Float),
        new BuiltinPreset("左右(軸)",             "/input/Horizontal",            OscValueType.Float),
        new BuiltinPreset("水平視点回転",         "/input/LookHorizontal",        OscValueType.Float),
        new BuiltinPreset("右手使用(軸)",         "/input/UseAxisRight",          OscValueType.Float),
        new BuiltinPreset("右手グラブ(軸)",       "/input/GrabAxisRight",         OscValueType.Float),
        new BuiltinPreset("保持:前後",            "/input/MoveHoldFB",            OscValueType.Float),
        new BuiltinPreset("保持:回転(時計回り)",  "/input/SpinHoldCwCcw",         OscValueType.Float),
        new BuiltinPreset("保持:回転(上下)",      "/input/SpinHoldUD",            OscValueType.Float),
        new BuiltinPreset("保持:回転(左右)",      "/input/SpinHoldLR",            OscValueType.Float),

        // Int 移動 (0 / 1)
        new BuiltinPreset("前進",                 "/input/MoveForward",           OscValueType.Int),
        new BuiltinPreset("後退",                 "/input/MoveBackward",          OscValueType.Int),
        new BuiltinPreset("左移動",               "/input/MoveLeft",              OscValueType.Int),
        new BuiltinPreset("右移動",               "/input/MoveRight",             OscValueType.Int),
        new BuiltinPreset("左旋回",               "/input/LookLeft",              OscValueType.Int),
        new BuiltinPreset("右旋回",               "/input/LookRight",             OscValueType.Int),
        new BuiltinPreset("ジャンプ",             "/input/Jump",                  OscValueType.Int),
        new BuiltinPreset("走る",                 "/input/Run",                   OscValueType.Int),
        new BuiltinPreset("スナップターン左",     "/input/ComfortLeft",           OscValueType.Int),
        new BuiltinPreset("スナップターン右",     "/input/ComfortRight",          OscValueType.Int),

        // Int 手操作 (0 / 1)
        new BuiltinPreset("右手グラブ",           "/input/GrabRight",             OscValueType.Int),
        new BuiltinPreset("右手使用",             "/input/UseRight",              OscValueType.Int),
        new BuiltinPreset("右手ドロップ",         "/input/DropRight",             OscValueType.Int),
        new BuiltinPreset("左手グラブ",           "/input/GrabLeft",              OscValueType.Int),
        new BuiltinPreset("左手使用",             "/input/UseLeft",               OscValueType.Int),
        new BuiltinPreset("左手ドロップ",         "/input/DropLeft",              OscValueType.Int),

        // Int その他 (0 / 1)
        new BuiltinPreset("パニックボタン",       "/input/PanicButton",           OscValueType.Int),
        new BuiltinPreset("クイックメニュー左",   "/input/QuickMenuToggleLeft",   OscValueType.Int),
        new BuiltinPreset("クイックメニュー右",   "/input/QuickMenuToggleRight",  OscValueType.Int),
        new BuiltinPreset("ボイス",               "/input/Voice",                 OscValueType.Int),

        // キーボード
        new KeySinglePreset("キー送信"),
        new KeyTypeStringPreset("文字入力"),

        // マウス
        new MouseButtonPreset("マウスボタン"),
        new MouseWheelPreset("マウスホイール"),
        new MouseMovePreset("マウス移動"),

        // カスタム
        new CustomPreset("カスタム"),
    ];
}

public sealed record WaitPreset(string Name) : SlotPreset(Name);

public sealed record BreakpointPreset(string Name) : SlotPreset(Name);

public sealed record LoopBeginPreset(string Name) : SlotPreset(Name);

public sealed record LoopEndPreset(string Name) : SlotPreset(Name);

public sealed record CustomPreset(string Name) : SlotPreset(Name);

public sealed record BuiltinPreset(string Name, string Address, OscValueType ValueType) : SlotPreset(Name);

public sealed record KeySinglePreset(string Name) : SlotPreset(Name);

public sealed record KeyTypeStringPreset(string Name) : SlotPreset(Name);

public sealed record MouseButtonPreset(string Name) : SlotPreset(Name);

public sealed record MouseWheelPreset(string Name) : SlotPreset(Name);

public sealed record MouseMovePreset(string Name) : SlotPreset(Name);
