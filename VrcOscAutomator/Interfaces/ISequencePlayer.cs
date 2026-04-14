using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface ISequencePlayer : IDisposable
{
    bool IsPlaying { get; }
    bool IsPaused { get; }
    int CurrentSlotIndex { get; }

    Task PlayAsync(IReadOnlyList<SequenceSlot> slots, bool loop, IProgress<int>? slotProgress, CancellationToken cancellationToken);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
}
