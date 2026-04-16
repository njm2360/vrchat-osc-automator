namespace VrcOscAutomator.Models;

public enum KeyboardInputMode { VirtualKey, ScanCode }
public enum MouseInputMode { Standard, VirtualDesktop }

public sealed class InputSettings
{
    public KeyboardInputMode KeyboardMode { get; set; } = KeyboardInputMode.ScanCode;
    public MouseInputMode MouseMode { get; set; } = MouseInputMode.VirtualDesktop;
}
