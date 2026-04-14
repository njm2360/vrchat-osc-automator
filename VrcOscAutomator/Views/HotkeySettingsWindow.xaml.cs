using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using VrcOscAutomator.ViewModels;

namespace VrcOscAutomator.Views;

public partial class HotkeySettingsWindow : Window
{
    // ---- Win32 P/Invoke ----
    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] private static extern IntPtr GetModuleHandle(string? lpModuleName);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

    // ---- フィールド ----
    private IntPtr _hookHandle = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookProc; // GC 防止のため保持
    private HotkeySettingsViewModel? _vm;

    // ---- 初期化 ----
    public HotkeySettingsWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => SubscribeToViewModel();
    }

    private void SubscribeToViewModel()
    {
        if (_vm != null)
            _vm.PropertyChanged -= OnViewModelPropertyChanged;

        _vm = DataContext as HotkeySettingsViewModel;

        if (_vm != null)
            _vm.PropertyChanged += OnViewModelPropertyChanged;
    }

    // ---- 待機状態の変化を監視してフックを管理 ----
    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HotkeySettingsViewModel.IsListening)) return;

        if (_vm?.IsListening == true)
            InstallHook();
        else
            UninstallHook();
    }

    private void InstallHook()
    {
        if (_hookHandle != IntPtr.Zero) return;
        _hookProc = LowLevelKeyboardHookProc;
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _hookProc, GetModuleHandle(module.ModuleName), 0);
    }

    private void UninstallHook()
    {
        if (_hookHandle == IntPtr.Zero) return;
        UnhookWindowsHookEx(_hookHandle);
        _hookHandle = IntPtr.Zero;
        _hookProc = null;
    }

    // ---- ローレベルフック本体 ----
    private IntPtr LowLevelKeyboardHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0 || (_vm?.IsListening != true))
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        int msg = wParam.ToInt32();
        if (msg != WM_KEYDOWN && msg != WM_SYSKEYDOWN)
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
        Key key = KeyInterop.KeyFromVirtualKey((int)data.vkCode);

        // 修飾キー単体は無視
        if (key is Key.LeftCtrl or Key.RightCtrl
                or Key.LeftAlt or Key.RightAlt
                or Key.LeftShift or Key.RightShift
                or Key.LWin or Key.RWin)
            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        // 修飾キーの状態を GetAsyncKeyState で取得（Winキー対応）
        ModifierKeys modifiers = ModifierKeys.None;
        if ((GetAsyncKeyState(0x11) & 0x8000) != 0) modifiers |= ModifierKeys.Control; // VK_CONTROL
        if ((GetAsyncKeyState(0x12) & 0x8000) != 0) modifiers |= ModifierKeys.Alt;     // VK_MENU
        if ((GetAsyncKeyState(0x10) & 0x8000) != 0) modifiers |= ModifierKeys.Shift;   // VK_SHIFT
        if ((GetAsyncKeyState(0x5B) & 0x8000) != 0 ||
            (GetAsyncKeyState(0x5C) & 0x8000) != 0) modifiers |= ModifierKeys.Windows; // VK_LWIN / VK_RWIN

        if (key == Key.Escape)
            Dispatcher.BeginInvoke(() => _vm?.CancelListening());
        else
        {
            var k = key;
            var m = modifiers;
            Dispatcher.BeginInvoke(() => _vm?.HandleKeyPress(k, m));
        }

        return (IntPtr)1; // キーをシステムへ伝播させない
    }

    // ---- ウィンドウクローズ時にフック解除 ----
    protected override void OnClosed(EventArgs e)
    {
        UninstallHook();
        if (_vm != null) _vm.PropertyChanged -= OnViewModelPropertyChanged;
        base.OnClosed(e);
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        _vm?.CancelListening();
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
