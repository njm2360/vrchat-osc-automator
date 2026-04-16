using System.Text.Json.Serialization;

namespace VrcOscAutomator.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FloatSlot), "float")]
[JsonDerivedType(typeof(IntSlot), "int")]
[JsonDerivedType(typeof(BoolSlot), "bool")]
[JsonDerivedType(typeof(StringSlot), "string")]
[JsonDerivedType(typeof(WaitSlot), "wait")]
[JsonDerivedType(typeof(LoopBeginSlot), "loop_begin")]
[JsonDerivedType(typeof(LoopEndSlot), "loop_end")]
[JsonDerivedType(typeof(KeySingleSlot), "key_single")]
[JsonDerivedType(typeof(KeyTypeStringSlot), "key_type_string")]
[JsonDerivedType(typeof(MouseButtonSlot), "mouse_button")]
[JsonDerivedType(typeof(MouseWheelSlot), "mouse_wheel")]
[JsonDerivedType(typeof(MouseMoveSlot), "mouse_move")]
public abstract record SequenceSlot;

/// <summary>OSC を送信する基底スロット。</summary>
public abstract record OscSlot(
    string Address,
    int DurationMs,
    bool ResetOnComplete) : SequenceSlot;

/// <summary>Float 値を送信するスロット（-1.0〜+1.0）。</summary>
public record FloatSlot(
    string Address,
    float Value,
    int DurationMs = 500,
    bool ResetOnComplete = true) : OscSlot(Address, DurationMs, ResetOnComplete);

/// <summary>Int 値を送信するスロット（0 / 1）。</summary>
public record IntSlot(
    string Address,
    int Value,
    int DurationMs = 500,
    bool ResetOnComplete = true) : OscSlot(Address, DurationMs, ResetOnComplete);

/// <summary>Bool 値を送信するスロット。</summary>
public record BoolSlot(
    string Address,
    bool Value,
    int DurationMs = 500,
    bool ResetOnComplete = true) : OscSlot(Address, DurationMs, ResetOnComplete);

/// <summary>String 値を送信するスロット。</summary>
public record StringSlot(
    string Address,
    string Value,
    int DurationMs = 500,
    bool ResetOnComplete = true) : OscSlot(Address, DurationMs, ResetOnComplete);

/// <summary>OSC を送信せず待機するスロット。</summary>
public record WaitSlot(int DurationMs = 500) : SequenceSlot;

/// <summary>繰り返しブロックの開始マーカー。</summary>
public record LoopBeginSlot(int RepeatCount = 2) : SequenceSlot;

/// <summary>繰り返しブロックの終了マーカー。</summary>
public record LoopEndSlot() : SequenceSlot;

/// <summary>単一キーを PRESS または RELEASE するスロット。</summary>
public record KeySingleSlot(int VirtualKey, KeyAction Action, int DurationMs = 0) : SequenceSlot;

/// <summary>文字列をキーボード入力として送信するスロット。</summary>
public record KeyTypeStringSlot(string Text, bool AppendNewline = false, int DurationMs = 0) : SequenceSlot;

public enum KeyAction { Press, Release }

/// <summary>マウスボタンを PRESS または RELEASE するスロット。</summary>
public record MouseButtonSlot(MouseButton Button, KeyAction Action, int DurationMs = 0) : SequenceSlot;

/// <summary>マウスホイールをスクロールするスロット。正値=上、負値=下。</summary>
public record MouseWheelSlot(int Clicks, int DurationMs = 0) : SequenceSlot;

/// <summary>マウスカーソルを移動するスロット。</summary>
public record MouseMoveSlot(int X, int Y, MouseMoveMode Mode, int DurationMs = 0) : SequenceSlot;

public enum MouseButton { Left, Right, Middle }

public enum MouseMoveMode { Relative, Absolute }
