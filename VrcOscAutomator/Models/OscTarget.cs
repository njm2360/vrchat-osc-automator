namespace VrcOscAutomator.Models;

public sealed class OscTarget
{
    public string IpAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 9000;
    public bool IsEnabled { get; set; } = true;
}
