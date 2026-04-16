namespace VrcOscAutomator.Models;

public sealed class AppSettings
{
    public int Version { get; init; } = 2;

    public List<OscTarget> Targets { get; set; } = [new()];
    public List<Profile> Profiles { get; set; } =
    [
        new() { Name = "Profile 1" },
        new() { Name = "Profile 2" },
        new() { Name = "Profile 3" },
        new() { Name = "Profile 4" },
        new() { Name = "Profile 5" },
    ];
    public HotkeySettings Hotkeys { get; set; } = new();
    public KeyRepeatSettings KeyRepeat { get; set; } = new();
    public InputSettings Input { get; set; } = new();
    public bool IsLoopMode { get; set; } = false;
}
