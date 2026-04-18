namespace VrcOscAutomator.Models;

public record SequenceProgress(int SlotIndex, IReadOnlyDictionary<int, int> LoopIterations);
