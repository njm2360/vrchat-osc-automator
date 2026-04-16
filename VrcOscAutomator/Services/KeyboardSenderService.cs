using System.Runtime.InteropServices;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class KeyboardSenderService : IKeyboardSender
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    private const uint MAPVK_VK_TO_VSC = 0;

    private const uint INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_UNICODE = 0x0004;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint KEYEVENTF_KEYDOWN = 0x0000;
    private const ushort VK_RETURN = 0x0D;

    public KeyboardInputMode Mode { get; set; } = KeyboardInputMode.ScanCode;

    private static readonly HashSet<ushort> ExtendedKeys =
    [
        0x21, // VK_PRIOR  (Page Up)
        0x22, // VK_NEXT   (Page Down)
        0x23, // VK_END
        0x24, // VK_HOME
        0x25, // VK_LEFT
        0x26, // VK_UP
        0x27, // VK_RIGHT
        0x28, // VK_DOWN
        0x2C, // VK_SNAPSHOT (Print Screen)
        0x2D, // VK_INSERT
        0x2E, // VK_DELETE
        0x5B, // VK_LWIN
        0x5C, // VK_RWIN
        0x5D, // VK_APPS
        0x6F, // VK_DIVIDE  (Numpad /)
        0x90, // VK_NUMLOCK
        0xA3, // VK_RCONTROL
        0xA5, // VK_RMENU   (Right Alt)
        0xAD, // VK_VOLUME_MUTE
        0xAE, // VK_VOLUME_DOWN
        0xAF, // VK_VOLUME_UP
        0xB0, // VK_MEDIA_NEXT_TRACK
        0xB1, // VK_MEDIA_PREV_TRACK
        0xB3, // VK_MEDIA_PLAY_PAUSE
    ];

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
        var vk = (ushort)virtualKey;

        if (Mode == KeyboardInputMode.ScanCode)
        {
            ushort scan = (ushort)MapVirtualKey(vk, MAPVK_VK_TO_VSC);
            uint flags = KEYEVENTF_SCANCODE;
            if (action == KeyAction.Release) flags |= KEYEVENTF_KEYUP;
            if (ExtendedKeys.Contains(vk)) flags |= KEYEVENTF_EXTENDEDKEY;

            INPUT[] inputs = [MakeScanInput(scan, flags)];
            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }
        else
        {
            uint flags = action == KeyAction.Release ? KEYEVENTF_KEYUP : KEYEVENTF_KEYDOWN;
            INPUT[] inputs = [MakeVkInput(vk, flags)];
            SendInput(1, inputs, Marshal.SizeOf<INPUT>());
        }
    }

    public void TypeString(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        var inputs = new List<INPUT>(text.Length * 2);
        foreach (char c in text)
        {
            if (c is '\n' or '\r')
            {
                ushort scan = (ushort)MapVirtualKey(VK_RETURN, MAPVK_VK_TO_VSC);
                inputs.Add(MakeScanInput(scan, KEYEVENTF_SCANCODE));
                inputs.Add(MakeScanInput(scan, KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP));
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
        u = new InputUnion { ki = new KEYBDINPUT { wVk = vk, wScan = 0, dwFlags = flags } },
    };

    private static INPUT MakeScanInput(ushort scan, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scan, dwFlags = flags } },
    };

    private static INPUT MakeUnicodeInput(char c, uint flags) => new()
    {
        type = INPUT_KEYBOARD,
        u = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = c, dwFlags = flags } },
    };
}
