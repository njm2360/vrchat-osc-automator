namespace VrcOscAutomator.Models;

public sealed class KeyRepeatSettings
{
    public bool IsEnabled { get; set; } = true;

    public int InitialDelayMs { get; set; } = 0;

    public int IntervalMs { get; set; } = 33;
}
