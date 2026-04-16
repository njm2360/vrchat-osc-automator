using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class KeyRepeatSettingsViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderEnabled))]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InitialDelaySummary))]
    public partial int InitialDelayMs { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntervalSummary))]
    [NotifyPropertyChangedFor(nameof(IntervalRatePerSecond))]
    public partial int IntervalMs { get; set; } = 33;

    public int IntervalRatePerSecond
    {
        get => Math.Max(1, 1000 / Math.Max(1, IntervalMs));
        set
        {
            int ms = Math.Max(1, 1000 / Math.Max(1, value));
            if (IntervalMs != ms)
            {
                IntervalMs = ms;
                OnPropertyChanged();
            }
        }
    }

    public bool SliderEnabled => IsEnabled;

    public string InitialDelaySummary => InitialDelayMs == 0 ? "なし" : $"{InitialDelayMs} ms";
    public string IntervalSummary => $"{IntervalRatePerSecond} 回/秒";

    public void LoadFromSettings(KeyRepeatSettings settings)
    {
        IsEnabled = settings.IsEnabled;
        InitialDelayMs = settings.InitialDelayMs;
        IntervalMs = settings.IntervalMs;
    }

    public KeyRepeatSettings ToSettings() => new()
    {
        IsEnabled = IsEnabled,
        InitialDelayMs = InitialDelayMs,
        IntervalMs = IntervalMs,
    };
}
