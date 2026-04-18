using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial float FloatValue { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTransitionMode))]
    [NotifyPropertyChangedFor(nameof(IsFixedValueMode))]
    [NotifyPropertyChangedFor(nameof(SelectedTransitionModeIndex))]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial TransitionMode TransitionMode { get; set; } = TransitionMode.None;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial float FloatTransitionFrom { get; set; } = 0f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial float FloatTransitionTo { get; set; } = 1f;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int IntTransitionFrom { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int IntTransitionTo { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsValueOn))]
    [NotifyPropertyChangedFor(nameof(IsValueOff))]
    public partial int IntValue { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial bool BoolValue { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial string StringValue { get; set; } = "";

    public bool IsValueOn
    {
        get => IntValue == 1;
        set { if (value) IntValue = 1; }
    }

    public bool IsValueOff
    {
        get => IntValue == 0;
        set { if (value) IntValue = 0; }
    }

    public bool IsTransitionAvailable =>
        SelectedPreset is CustomPreset && CustomValueType is OscValueType.Float or OscValueType.Int;

    public bool IsTransitionMode => TransitionMode != TransitionMode.None;
    public bool IsFixedValueMode => TransitionMode == TransitionMode.None;

    public int SelectedTransitionModeIndex
    {
        get => (int)TransitionMode;
        set => TransitionMode = (TransitionMode)value;
    }

    private string ValueSummary => CustomValueType switch
    {
        OscValueType.Float when IsTransitionMode =>
            $"{FloatTransitionFrom:0.###} → {FloatTransitionTo:0.###} [{TransitionModeLabel(TransitionMode)}]",
        OscValueType.Int when IsTransitionMode =>
            $"{IntTransitionFrom} → {IntTransitionTo} [{TransitionModeLabel(TransitionMode)}]",
        OscValueType.Int => $"{IntValue}",
        OscValueType.Bool => BoolValue ? "true" : "false",
        OscValueType.String => $"\"{StringValue}\"",
        _ => $"{FloatValue:0.###}",
    };

    private static string TransitionModeLabel(TransitionMode mode) => mode switch
    {
        TransitionMode.Linear => "Linear",
        TransitionMode.EaseIn => "EaseIn",
        TransitionMode.EaseOut => "EaseOut",
        TransitionMode.EaseInOut => "EaseInOut",
        _ => "",
    };

    partial void OnCustomValueTypeChanged(OscValueType value)
    {
        if (value is not OscValueType.Float and not OscValueType.Int)
            TransitionMode = TransitionMode.None;
    }

    partial void OnSelectedPresetChanged(SlotPreset value)
    {
        if (value is not CustomPreset)
            TransitionMode = TransitionMode.None;
    }
}
