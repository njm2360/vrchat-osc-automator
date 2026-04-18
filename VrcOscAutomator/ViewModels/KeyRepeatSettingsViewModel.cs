using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class KeyRepeatSettingsViewModel : ObservableObject
{
    // ── 設定値 ────────────────────────────────────────────────────────────

    // キーリピート有効
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SliderEnabled))]
    public partial bool IsEnabled { get; set; } = true;

    // リピート開始遅延（ms） 0 = 遅延なし
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InitialDelaySummary))]
    public partial int InitialDelayMs { get; set; } = 0;

    // リピート間隔（ms）
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntervalSummary))]
    [NotifyPropertyChangedFor(nameof(IntervalRatePerSecond))]
    public partial int IntervalMs { get; set; } = 33;

    // ── 表示用プロパティ ──────────────────────────────────────────────────

    // IntervalMs <=> 回/秒
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

    // IsEnabledがfalseのときスライダーを無効化
    public bool SliderEnabled => IsEnabled;

    // スライダー横のラベル
    public string InitialDelaySummary => InitialDelayMs == 0 ? "なし" : $"{InitialDelayMs} ms";
    public string IntervalSummary => $"{IntervalRatePerSecond} 回/秒";

    // ── モデル変換 ────────────────────────────────────────────────────────

    // KeyRepatSettings => VM
    public void LoadFromSettings(KeyRepeatSettings settings)
    {
        IsEnabled = settings.IsEnabled;
        InitialDelayMs = settings.InitialDelayMs;
        IntervalMs = settings.IntervalMs;
    }

    // VM => KeyRepeatSettings
    public KeyRepeatSettings ToSettings() => new()
    {
        IsEnabled = IsEnabled,
        InitialDelayMs = InitialDelayMs,
        IntervalMs = IntervalMs,
    };
}
