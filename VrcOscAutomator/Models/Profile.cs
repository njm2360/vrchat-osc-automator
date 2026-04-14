namespace VrcOscAutomator.Models;

public sealed class Profile
{
    public string Name { get; set; } = string.Empty;
    public List<SequenceSlot> Slots { get; set; } = [];
}
