using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface IOscSender : IDisposable
{
    void SetTargets(IEnumerable<OscTarget> targets);
    void SendFloat(string address, float value);
    void SendInt(string address, int value);
    void SendBool(string address, bool value);
    void SendString(string address, string value);
}
