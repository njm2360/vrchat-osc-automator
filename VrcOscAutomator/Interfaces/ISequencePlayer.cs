using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface ISequencePlayer : IDisposable
{
    bool IsPlaying { get; }
    bool IsPaused { get; }
    int CurrentSlotIndex { get; }

    void SetKeyRepeatSettings(KeyRepeatSettings settings);
    Task PlayAsync(IReadOnlyList<SequenceSlot> slots, bool loop, IProgress<SequenceProgress>? slotProgress, CancellationToken cancellationToken);
    Task PauseAsync();
    Task ResumeAsync();
    Task StopAsync();
}
