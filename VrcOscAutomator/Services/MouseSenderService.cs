using System.Runtime.InteropServices;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class MouseSenderService : IMouseSender
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    private const uint INPUT_MOUSE = 0;
    private const uint MOUSEEVENTF_MOVE = 0x0001;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
    private const uint MOUSEEVENTF_WHEEL = 0x0800;
    private const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
    private const int WHEEL_DELTA = 120;

    // ── P/Invoke 構造体 ──────────────────────────────────────────────────

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk, wScan;
        public uint dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL, wParamH;
    }

    // ── 公開メソッド ─────────────────────────────────────────────────────

    public void SendMouseButton(MouseButton button, KeyAction action)
    {
        uint flags = (button, action) switch
        {
            (MouseButton.Left, KeyAction.Press) => MOUSEEVENTF_LEFTDOWN,
            (MouseButton.Left, KeyAction.Release) => MOUSEEVENTF_LEFTUP,
            (MouseButton.Right, KeyAction.Press) => MOUSEEVENTF_RIGHTDOWN,
            (MouseButton.Right, KeyAction.Release) => MOUSEEVENTF_RIGHTUP,
            (MouseButton.Middle, KeyAction.Press) => MOUSEEVENTF_MIDDLEDOWN,
            (MouseButton.Middle, KeyAction.Release) => MOUSEEVENTF_MIDDLEUP,
            _ => 0u,
        };
        if (flags == 0) return;

        INPUT[] inputs = [MakeMouseInput(0, 0, 0, flags)];
        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    public void SendMouseWheel(int clicks)
    {
        if (clicks == 0) return;
        uint delta = (uint)(clicks * WHEEL_DELTA);
        INPUT[] inputs = [MakeMouseInput(0, 0, delta, MOUSEEVENTF_WHEEL)];
        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    public void SendMouseMove(int x, int y, MouseMoveMode mode)
    {
        if (mode == MouseMoveMode.Absolute)
        {
            int screenW = GetSystemMetrics(SM_CXSCREEN);
            int screenH = GetSystemMetrics(SM_CYSCREEN);
            x = (int)((long)x * 65535 / screenW);
            y = (int)((long)y * 65535 / screenH);
        }

        uint flags = mode == MouseMoveMode.Absolute
            ? MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE
            : MOUSEEVENTF_MOVE;

        INPUT[] inputs = [MakeMouseInput(x, y, 0, flags)];
        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    // ── ヘルパー ─────────────────────────────────────────────────────────

    private static INPUT MakeMouseInput(int dx, int dy, uint mouseData, uint flags) => new()
    {
        type = INPUT_MOUSE,
        u = new InputUnion
        {
            mi = new MOUSEINPUT { dx = dx, dy = dy, mouseData = mouseData, dwFlags = flags },
        },
    };
}
