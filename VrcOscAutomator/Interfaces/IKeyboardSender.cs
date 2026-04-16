using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface IKeyboardSender
{
    void SendKey(int virtualKey, KeyAction action);
    void TypeString(string text);
}
