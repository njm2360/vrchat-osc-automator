using System.Windows.Input;

namespace VrcOscAutomator.Models;

public sealed class HotkeyInfo
{
    public Key Key { get; set; } = Key.None;
    public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;

    public string GetDisplayText()
    {
        if (Key == Key.None) return "未設定";
        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}
