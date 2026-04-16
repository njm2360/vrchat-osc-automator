using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface IMouseSender
{
    void SendMouseButton(MouseButton button, KeyAction action);
    void SendMouseWheel(int clicks);
    void SendMouseMove(int x, int y, MouseMoveMode mode);
}
