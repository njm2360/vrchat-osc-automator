using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface ISettingsRepository
{
    Task<SettingsLoadResult> LoadAsync();
    Task SaveAsync(AppSettings settings);
}
