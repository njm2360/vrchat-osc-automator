using System.Windows;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface IGlobalHotkeyService : IDisposable
{
    event Action? StartPressed;
    event Action? PauseResumePressed;
    event Action? StopPressed;

    void Initialize(Window window);
    void UpdateSettings(HotkeySettings settings);
}
