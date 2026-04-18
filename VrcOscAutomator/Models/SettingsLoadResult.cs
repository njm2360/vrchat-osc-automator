namespace VrcOscAutomator.Models;

public record SettingsLoadResult(
    AppSettings Settings,
    bool WasCorrupted = false,
    string? CorruptionDetail = null);
