using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class GlobalHotkeyService : IGlobalHotkeyService
{
    private const int WM_HOTKEY = 0x0312;
    private const int ID_START = 9001;
    private const int ID_PAUSE = 9002;
    private const int ID_STOP = 9003;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private HotkeySettings _settings = new();

    public event Action? StartPressed;
    public event Action? PauseResumePressed;
    public event Action? StopPressed;

    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _hwndSource = HwndSource.FromHwnd(helper.Handle);
        _hwndSource?.AddHook(WndProc);
        RegisterAll();
    }

    public void UpdateSettings(HotkeySettings settings)
    {
        UnregisterAll();
        _settings = settings;
        if (_hwndSource != null)
            RegisterAll();
    }

    private void RegisterAll()
    {
        if (_hwndSource == null) return;
        var hwnd = _hwndSource.Handle;
        RegisterHotkeyIfSet(hwnd, ID_START, _settings.Start);
        RegisterHotkeyIfSet(hwnd, ID_PAUSE, _settings.PauseResume);
        RegisterHotkeyIfSet(hwnd, ID_STOP, _settings.Stop);
    }

    private static void RegisterHotkeyIfSet(IntPtr hwnd, int id, HotkeyInfo info)
    {
        if (info.Key == Key.None) return;
        uint vk = (uint)KeyInterop.VirtualKeyFromKey(info.Key);
        uint mods = ToWin32Modifiers(info.Modifiers);
        RegisterHotKey(hwnd, id, mods, vk);
    }

    private void UnregisterAll()
    {
        if (_hwndSource == null) return;
        var hwnd = _hwndSource.Handle;
        UnregisterHotKey(hwnd, ID_START);
        UnregisterHotKey(hwnd, ID_PAUSE);
        UnregisterHotKey(hwnd, ID_STOP);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            switch (wParam.ToInt32())
            {
                case ID_START:
                    StartPressed?.Invoke();
                    handled = true;
                    break;
                case ID_PAUSE:
                    PauseResumePressed?.Invoke();
                    handled = true;
                    break;
                case ID_STOP:
                    StopPressed?.Invoke();
                    handled = true;
                    break;
            }
        }
        return IntPtr.Zero;
    }

    private static uint ToWin32Modifiers(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= 0x0001;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= 0x0002;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= 0x0004;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= 0x0008;
        return result;
    }

    public void Dispose()
    {
        UnregisterAll();
        _hwndSource?.RemoveHook(WndProc);
    }
}
