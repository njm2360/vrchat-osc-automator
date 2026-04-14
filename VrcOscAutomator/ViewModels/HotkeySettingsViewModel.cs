using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public enum ListeningTarget { None, Start, PauseResume, Stop }

public sealed partial class HotkeySettingsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartDisplayText))]
    [NotifyPropertyChangedFor(nameof(IsStartSet))]
    [NotifyCanExecuteChangedFor(nameof(ClearStartCommand))]
    private HotkeyInfo _startHotkey = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PauseDisplayText))]
    [NotifyPropertyChangedFor(nameof(IsPauseSet))]
    [NotifyCanExecuteChangedFor(nameof(ClearPauseCommand))]
    private HotkeyInfo _pauseHotkey = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StopDisplayText))]
    [NotifyPropertyChangedFor(nameof(IsStopSet))]
    [NotifyCanExecuteChangedFor(nameof(ClearStopCommand))]
    private HotkeyInfo _stopHotkey = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsListening))]
    [NotifyPropertyChangedFor(nameof(IsListeningStart))]
    [NotifyPropertyChangedFor(nameof(IsListeningPause))]
    [NotifyPropertyChangedFor(nameof(IsListeningStop))]
    [NotifyPropertyChangedFor(nameof(StartDisplayText))]
    [NotifyPropertyChangedFor(nameof(PauseDisplayText))]
    [NotifyPropertyChangedFor(nameof(StopDisplayText))]
    private ListeningTarget _currentListening = ListeningTarget.None;

    public bool IsListening => CurrentListening != ListeningTarget.None;
    public bool IsListeningStart => CurrentListening == ListeningTarget.Start;
    public bool IsListeningPause => CurrentListening == ListeningTarget.PauseResume;
    public bool IsListeningStop => CurrentListening == ListeningTarget.Stop;

    public bool IsStartSet => StartHotkey.Key != Key.None;
    public bool IsPauseSet => PauseHotkey.Key != Key.None;
    public bool IsStopSet => StopHotkey.Key != Key.None;

    public string StartDisplayText =>
        IsListeningStart ? "キーを押してください..." : StartHotkey.GetDisplayText();
    public string PauseDisplayText =>
        IsListeningPause ? "キーを押してください..." : PauseHotkey.GetDisplayText();
    public string StopDisplayText =>
        IsListeningStop ? "キーを押してください..." : StopHotkey.GetDisplayText();

    [RelayCommand]
    private void ListenStart() => CurrentListening = ListeningTarget.Start;

    [RelayCommand]
    private void ListenPause() => CurrentListening = ListeningTarget.PauseResume;

    [RelayCommand]
    private void ListenStop() => CurrentListening = ListeningTarget.Stop;

    [RelayCommand(CanExecute = nameof(IsStartSet))]
    private void ClearStart() => StartHotkey = new();

    [RelayCommand(CanExecute = nameof(IsPauseSet))]
    private void ClearPause() => PauseHotkey = new();

    [RelayCommand(CanExecute = nameof(IsStopSet))]
    private void ClearStop() => StopHotkey = new();

    public void CancelListening() => CurrentListening = ListeningTarget.None;

    public void HandleKeyPress(Key key, ModifierKeys modifiers)
    {
        var info = new HotkeyInfo { Key = key, Modifiers = modifiers };
        switch (CurrentListening)
        {
            case ListeningTarget.Start:
                StartHotkey = info;
                break;
            case ListeningTarget.PauseResume:
                PauseHotkey = info;
                break;
            case ListeningTarget.Stop:
                StopHotkey = info;
                break;
        }
        CurrentListening = ListeningTarget.None;
    }

    public void LoadFromSettings(HotkeySettings settings)
    {
        StartHotkey = new HotkeyInfo { Key = settings.Start.Key, Modifiers = settings.Start.Modifiers };
        PauseHotkey = new HotkeyInfo { Key = settings.PauseResume.Key, Modifiers = settings.PauseResume.Modifiers };
        StopHotkey = new HotkeyInfo { Key = settings.Stop.Key, Modifiers = settings.Stop.Modifiers };
    }

    public HotkeySettings ToSettings() => new()
    {
        Start = new() { Key = StartHotkey.Key, Modifiers = StartHotkey.Modifiers },
        PauseResume = new() { Key = PauseHotkey.Key, Modifiers = PauseHotkey.Modifiers },
        Stop = new() { Key = StopHotkey.Key, Modifiers = StopHotkey.Modifiers },
    };
}
