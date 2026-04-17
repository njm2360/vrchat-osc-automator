namespace VrcOscAutomator.Models;

public sealed class AppSettings
{
    public int Version { get; init; } = 2;

    public List<OscTarget> Targets { get; set; } = [new()];
    public List<Profile> Profiles { get; set; } = [new() { Name = "Profile 1" }];
    public HotkeySettings Hotkeys { get; set; } = new();
    public KeyRepeatSettings KeyRepeat { get; set; } = new();
    public InputSettings Input { get; set; } = new();
    public bool IsLoopMode { get; set; } = false;
}
