using CommunityToolkit.Mvvm.ComponentModel;
using KeyAction = VrcOscAutomator.Models.KeyAction;
using MouseButton = VrcOscAutomator.Models.MouseButton;
using MouseMoveMode = VrcOscAutomator.Models.MouseMoveMode;

namespace VrcOscAutomator.ViewModels;

public sealed partial class SequenceSlotViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonLeft))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonRight))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonMiddle))]
    public partial MouseButton SelectedMouseButton { get; set; } = MouseButton.Left;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonActionPress))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonActionRelease))]
    [NotifyPropertyChangedFor(nameof(IsMouseButtonActionPressAndRelease))]
    public partial KeyAction SelectedMouseButtonAction { get; set; } = KeyAction.Press;

    public bool IsMouseButtonActionPress
    {
        get => SelectedMouseButtonAction == KeyAction.Press;
        set { if (value) SelectedMouseButtonAction = KeyAction.Press; }
    }

    public bool IsMouseButtonActionRelease
    {
        get => SelectedMouseButtonAction == KeyAction.Release;
        set { if (value) SelectedMouseButtonAction = KeyAction.Release; }
    }

    public bool IsMouseButtonActionPressAndRelease
    {
        get => SelectedMouseButtonAction == KeyAction.PressAndRelease;
        set { if (value) SelectedMouseButtonAction = KeyAction.PressAndRelease; }
    }

    public bool IsMouseButtonLeft { get => SelectedMouseButton == MouseButton.Left; set { if (value) SelectedMouseButton = MouseButton.Left; } }
    public bool IsMouseButtonRight { get => SelectedMouseButton == MouseButton.Right; set { if (value) SelectedMouseButton = MouseButton.Right; } }
    public bool IsMouseButtonMiddle { get => SelectedMouseButton == MouseButton.Middle; set { if (value) SelectedMouseButton = MouseButton.Middle; } }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int WheelClicks { get; set; } = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int MouseMoveX { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    public partial int MouseMoveY { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ParameterSummary))]
    [NotifyPropertyChangedFor(nameof(IsMouseMoveRelative))]
    [NotifyPropertyChangedFor(nameof(IsMouseMoveAbsolute))]
    public partial MouseMoveMode SelectedMouseMoveMode { get; set; } = MouseMoveMode.Relative;

    public bool IsMouseMoveRelative
    {
        get => SelectedMouseMoveMode == MouseMoveMode.Relative;
        set { if (value) SelectedMouseMoveMode = MouseMoveMode.Relative; }
    }

    public bool IsMouseMoveAbsolute
    {
        get => SelectedMouseMoveMode == MouseMoveMode.Absolute;
        set { if (value) SelectedMouseMoveMode = MouseMoveMode.Absolute; }
    }
}
