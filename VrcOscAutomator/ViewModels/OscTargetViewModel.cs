using CommunityToolkit.Mvvm.ComponentModel;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.ViewModels;

public sealed partial class OscTargetViewModel : ObservableObject
{
    [ObservableProperty]
    private string _ipAddress = "127.0.0.1";

    [ObservableProperty]
    private int _port = 9000;

    [ObservableProperty]
    private bool _isEnabled = true;

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
