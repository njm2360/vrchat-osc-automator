using VrcOscAutomator.Models;

namespace VrcOscAutomator.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> LoadAsync();
    Task SaveAsync(AppSettings settings);
}
