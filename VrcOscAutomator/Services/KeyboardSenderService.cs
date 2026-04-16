using System.Runtime.InteropServices;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class KeyboardSenderService : IKeyboardSender
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const ushort VK_RETURN = 0x0D;

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
        public uint mouseData, dwFlags, time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL, wParamH;
    }

    // ── 公開メソッド ─────────────────────────────────────────────────────

    public void SendKey(int virtualKey, KeyAction action)
    {
        uint flags = action == KeyAction.Press ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP;
        INPUT[] inputs = [MakeVkInput((ushort)virtualKey, flags)];
        SendInput(1, inputs, Marshal.SizeOf<INPUT>());
    }

    public void TypeString(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var inputs = new List<INPUT>(text.Length * 2);
        foreach (char c in text)
        {
            if (c is '\n' or '\r')
            {
                inputs.Add(MakeVkInput(VK_RETURN, KEYEVENTF_KEYDOWN));
                inputs.Add(MakeVkInput(VK_RETURN, KEYEVENTF_KEYUP));
            }
            else
            {
                inputs.Add(MakeUnicodeInput(c, KEYEVENTF_UNICODE));
                inputs.Add(MakeUnicodeInput(c, KEYEVENTF_UNICODE | KEYEVENTF_KEYUP));
            }
        }

        if (inputs.Count > 0)
            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
    }

    // ── ヘルパー ─────────────────────────────────────────────────────────

    private static INPUT MakeVkInput(ushort vk, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } },
    };

    private static INPUT MakeUnicodeInput(char c, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion { ki = new KEYBDINPUT { wScan = c, dwFlags = flags } },
    };
}
