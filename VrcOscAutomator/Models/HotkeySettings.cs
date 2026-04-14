namespace VrcOscAutomator.Models;

public sealed class HotkeySettings
{
    public HotkeyInfo Start { get; set; } = new();
    public HotkeyInfo PauseResume { get; set; } = new();
    public HotkeyInfo Stop { get; set; } = new();
}
