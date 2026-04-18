using CommunityToolkit.Mvvm.ComponentModel;
using KeyAction = VrcOscAutomator.Models.KeyAction;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial VirtualKeyItem SelectedKey { get; set; } = VirtualKeyItem.All[0];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsKeyActionPress))]
    [NotifyPropertyChangedFor(nameof(IsKeyActionRelease))]
    [NotifyPropertyChangedFor(nameof(IsKeyActionPressAndRelease))]
    public partial KeyAction SelectedKeyAction { get; set; } = KeyAction.Press;

    public bool IsKeyActionPress
    {
        get => SelectedKeyAction == KeyAction.Press;
        set { if (value) SelectedKeyAction = KeyAction.Press; }
    }

    public bool IsKeyActionRelease
    {
        get => SelectedKeyAction == KeyAction.Release;
        set { if (value) SelectedKeyAction = KeyAction.Release; }
    }

    public bool IsKeyActionPressAndRelease
    {
        get => SelectedKeyAction == KeyAction.PressAndRelease;
        set { if (value) SelectedKeyAction = KeyAction.PressAndRelease; }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial string TypeText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial bool AppendNewline { get; set; }
}
