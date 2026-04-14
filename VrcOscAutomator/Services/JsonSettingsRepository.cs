using System.IO;
using System.Reflection;
using System.Text.Json;
using VrcOscAutomator.Interfaces;
using VrcOscAutomator.Models;

namespace VrcOscAutomator.Services;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Assembly.GetExecutingAssembly().GetName().Name!,
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(FilePath))
            return new AppSettings();

        await using FileStream fs = File.OpenRead(FilePath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(fs, JsonOptions)
               ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await using FileStream fs = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(fs, settings, JsonOptions);
    }
}
