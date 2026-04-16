namespace VrcOscAutomator.ViewModels;

/// <summary>キードロップダウン用 — VK コードと表示名のペア。</summary>
public sealed record VirtualKeyItem(int Code, string Name)
{
    public static readonly IReadOnlyList<VirtualKeyItem> All =
    [
        // ── 特殊キー ──────────────────────────────────────
        new(0x0D, "Enter"),
        new(0x1B, "Escape"),
        new(0x20, "Space"),
        new(0x09, "Tab"),
        new(0x08, "Backspace"),
        new(0x2E, "Delete"),
        new(0x2D, "Insert"),

        // ── カーソル ──────────────────────────────────────
        new(0x26, "↑"),
        new(0x28, "↓"),
        new(0x25, "←"),
        new(0x27, "→"),
        new(0x24, "Home"),
        new(0x23, "End"),
        new(0x21, "PageUp"),
        new(0x22, "PageDown"),

        // ── 修飾キー ──────────────────────────────────────
        new(0xA0, "Shift (左)"),
        new(0xA1, "Shift (右)"),
        new(0xA2, "Ctrl (左)"),
        new(0xA3, "Ctrl (右)"),
        new(0xA4, "Alt (左)"),
        new(0xA5, "Alt (右)"),
        new(0x5B, "Win (左)"),
        new(0x5C, "Win (右)"),

        // ── ファンクション ────────────────────────────────
        new(0x70, "F1"),
        new(0x71, "F2"),
        new(0x72, "F3"),
        new(0x73, "F4"),
        new(0x74, "F5"),
        new(0x75, "F6"),
        new(0x76, "F7"),
        new(0x77, "F8"),
        new(0x78, "F9"),
        new(0x79, "F10"),
        new(0x7A, "F11"),
        new(0x7B, "F12"),

        // ── アルファベット ────────────────────────────────
        new(0x41, "A"), new(0x42, "B"), new(0x43, "C"), new(0x44, "D"),
        new(0x45, "E"), new(0x46, "F"), new(0x47, "G"), new(0x48, "H"),
        new(0x49, "I"), new(0x4A, "J"), new(0x4B, "K"), new(0x4C, "L"),
        new(0x4D, "M"), new(0x4E, "N"), new(0x4F, "O"), new(0x50, "P"),
        new(0x51, "Q"), new(0x52, "R"), new(0x53, "S"), new(0x54, "T"),
        new(0x55, "U"), new(0x56, "V"), new(0x57, "W"), new(0x58, "X"),
        new(0x59, "Y"), new(0x5A, "Z"),

        // ── 数字 ──────────────────────────────────────────
        new(0x30, "0"), new(0x31, "1"), new(0x32, "2"), new(0x33, "3"),
        new(0x34, "4"), new(0x35, "5"), new(0x36, "6"), new(0x37, "7"),
        new(0x38, "8"), new(0x39, "9"),

        // ── 記号 (jp109前提  後で直す) ──────────────────────
        new(0xBD, "- _"),
        new(0xBB, "= +"),
        new(0xDB, "[ {"),
        new(0xDD, "] }"),
        new(0xDC, "\\ |"),
        new(0xBA, "; :"),
        new(0xDE, "' \""),
        new(0xBC, ", <"),
        new(0xBE, ". >"),
        new(0xBF, "/ ?"),
        new(0xC0, "` ~"),

        // ── ロック ────────────────────────────────────────
        new(0x14, "Caps Lock"),
        new(0x90, "Num Lock"),
        new(0x91, "Scroll Lock"),

        // ── システム ──────────────────────────────────────
        new(0x2C, "Print Screen"),
        new(0x13, "Pause/Break"),
    ];
}
