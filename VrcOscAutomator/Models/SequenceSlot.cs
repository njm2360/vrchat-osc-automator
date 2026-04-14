namespace VrcOscAutomator.Models;

public sealed record SequenceSlot
{
    /// <summary>OSC アドレス。null = 待機（送信なし）。</summary>
    public string? Address { get; init; }

    /// <summary>送信する値。Float: -1.0〜+1.0、Int: 0/1、Bool: 0(false)/1(true)。</summary>
    public float Value { get; init; }

    /// <summary>String型のときに送信する文字列。</summary>
    public string StringValue { get; init; } = string.Empty;

    /// <summary>送信する値の型。</summary>
    public OscValueType ValueType { get; init; } = OscValueType.Float;

    public int DurationMs { get; init; } = 500;
    public bool ResetOnComplete { get; init; } = true;

    /// <summary>スロットの種別。Normal 以外はループマーカーとして扱われる。</summary>
    public SlotType SlotType { get; init; } = SlotType.Normal;

    /// <summary>繰り返し回数。SlotType == LoopBegin のときのみ有効。</summary>
    public int RepeatCount { get; init; } = 2;
}
