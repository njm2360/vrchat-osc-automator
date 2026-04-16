using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface IKeyboardSender
{
    KeyboardInputMode Mode { get; set; }
    void SendKey(int virtualKey, KeyAction action);
    void TypeString(string text);
}
