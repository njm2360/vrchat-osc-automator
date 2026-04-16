using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class OscTargetViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string IpAddress { get; set; } = "127.0.0.1";

    [ObservableProperty]
    public partial int Port { get; set; } = 9000;

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public OscTarget ToModel() => new()
    {
        IpAddress = IpAddress,
        Port = Port,
        IsEnabled = IsEnabled,
    };

    public static OscTargetViewModel FromModel(OscTarget t) => new()
    {
        IpAddress = t.IpAddress,
        Port = t.Port,
        IsEnabled = t.IsEnabled,
    };
}
