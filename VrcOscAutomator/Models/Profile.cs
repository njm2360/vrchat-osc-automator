namespace VrcOscAutomator.Models;

public sealed class Profile
{
    public string Name { get; set; } = string.Empty;
    public bool IsLoopMode { get; set; } = false;
    public List<SequenceSlot> Slots { get; set; } = [];
}
